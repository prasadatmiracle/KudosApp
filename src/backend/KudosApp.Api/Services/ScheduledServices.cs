using KudosApp.Api.Data;
using KudosApp.Api.Models;

namespace KudosApp.Api.Services;

// ── P4: Daily update reminder (5 PM UTC — non-submitters get a Cliq DM) ──────

public interface IDailyReminderService
{
    Task SendDailyUpdateRemindersAsync(DateOnly today, CancellationToken ct);
}

public sealed class DailyReminderService(
    AppDbContext db,
    IZohoBridge zohoBridge,
    IReminderPolicy reminderPolicy,
    ILogger<DailyReminderService> logger) : IDailyReminderService
{
    public async Task SendDailyUpdateRemindersAsync(DateOnly today, CancellationToken ct)
    {
        // Skip weekends
        if (today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) return;

        var activeUsers = db.Users
            .Where(x => x.IsActive && x.Role == AppRole.Employee)
            .Select(x => new { x.UserId, x.Name, x.Email })
            .ToList();

        var submittedToday = db.DailyUpdates
            .Where(x => x.WorkDate == today)
            .Select(x => x.UserId)
            .Distinct()
            .ToHashSet();

        var nonSubmitters = activeUsers
            .Where(x => !submittedToday.Contains(x.UserId)
                        && reminderPolicy.CanDispatchReminder(x.UserId, today))
            .ToList();

        if (nonSubmitters.Count == 0)
        {
            logger.LogInformation("Daily reminder: all employees submitted for {Date}.", today);
            return;
        }

        foreach (var user in nonSubmitters)
        {
            var message = $"Hi {user.Name}, you haven't logged your daily update for {today:dd MMM} yet. "
                        + "It takes less than a minute — please submit before end of shift. Thank you!";

            await zohoBridge.SendCliqNotificationAsync(message, [user.Email], ct);
            reminderPolicy.MarkReminderSent(user.UserId, today);
        }

        logger.LogInformation("Daily update reminders sent to {Count} non-submitters on {Date}.",
            nonSubmitters.Count, today);
    }
}

// ── P4: BackgroundService — fires at 5 PM IST (11:30 AM UTC) on weekdays ─────

public sealed class DailyReminderHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<DailyReminderHostedService> logger) : BackgroundService
{
    // 2 PM IST = 08:30 AM UTC (team shift starts 2 PM, send reminder ~3 hours in)
    // We use 11:30 AM UTC = 5 PM IST as end-of-shift nudge
    private static readonly TimeSpan FireAtUtc = TimeSpan.FromHours(11).Add(TimeSpan.FromMinutes(30));

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("DailyReminderHostedService started.");

        while (!ct.IsCancellationRequested)
        {
            var delay = TimeUntilNext(FireAtUtc);
            logger.LogInformation("Daily reminder sleeping {Minutes:F0} min until next 5 PM IST.", delay.TotalMinutes);

            await Task.Delay(delay, ct);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IDailyReminderService>();
                await service.SendDailyUpdateRemindersAsync(today, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Daily reminder job failed on {Date}.", today);
            }
        }
    }

    private static TimeSpan TimeUntilNext(TimeSpan targetUtc)
    {
        var now = DateTime.UtcNow;
        var next = now.Date.Add(targetUtc);
        if (now >= next) next = next.AddDays(1);
        return next - now;
    }
}

// ── P5: Auto weekly report (Friday 6 PM IST = 12:30 PM UTC) ─────────────────

public interface IWeeklyReportSchedulerService
{
    Task GenerateWeeklyDraftsAsync(DateOnly today, CancellationToken ct);
}

public sealed class WeeklyReportSchedulerService(
    AppDbContext db,
    IReportService reportService,
    IZohoBridge zohoBridge,
    ILogger<WeeklyReportSchedulerService> logger) : IWeeklyReportSchedulerService
{
    public async Task GenerateWeeklyDraftsAsync(DateOnly today, CancellationToken ct)
    {
        // Run on Fridays only
        if (today.DayOfWeek != DayOfWeek.Friday) return;

        // Current week: Monday–Friday
        var daysFromMonday = (int)today.DayOfWeek - (int)DayOfWeek.Monday;
        var weekStart = today.AddDays(-daysFromMonday);
        var weekEnd = today;

        // Find all managers and admins to generate drafts for
        var managers = db.Users
            .Where(x => x.IsActive && (x.Role == AppRole.Manager || x.Role == AppRole.Admin))
            .Select(x => new { x.UserId, x.Name, x.Email })
            .ToList();

        if (managers.Count == 0)
        {
            logger.LogInformation("Weekly report: no managers found to generate drafts.");
            return;
        }

        foreach (var manager in managers)
        {
            // Skip if a report already exists for this window
            var alreadyExists = db.Reports.Any(x =>
                x.ReportType == ReportType.Weekly &&
                x.StartDate == weekStart &&
                x.EndDate == weekEnd &&
                x.GeneratedByUserId == manager.UserId);

            if (alreadyExists)
            {
                logger.LogInformation("Weekly draft already exists for manager {UserId} — skipping.", manager.UserId);
                continue;
            }

            var report = reportService.GenerateWeekly(manager.UserId, weekStart, weekEnd);

            var message = $"Hi {manager.Name}, your weekly report draft for {weekStart:dd MMM}–{weekEnd:dd MMM} "
                        + $"has been auto-generated (Report #{report.ReportRecordId}). "
                        + "Please review, add your notes, and submit it in KudosApp before end of day.";

            await zohoBridge.SendCliqNotificationAsync(message, [manager.Email], ct);

            logger.LogInformation("Weekly draft #{ReportId} generated for manager {UserId}.", report.ReportRecordId, manager.UserId);
        }
    }
}

public sealed class WeeklyReportHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<WeeklyReportHostedService> logger) : BackgroundService
{
    // Friday 6 PM IST = Friday 12:30 PM UTC
    private static readonly TimeSpan FireAtUtc = TimeSpan.FromHours(12).Add(TimeSpan.FromMinutes(30));

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("WeeklyReportHostedService started.");

        while (!ct.IsCancellationRequested)
        {
            var delay = TimeUntilNextFriday(FireAtUtc);
            logger.LogInformation("Weekly report sleeping {Hours:F1} hrs until Friday 6 PM IST.", delay.TotalHours);

            await Task.Delay(delay, ct);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IWeeklyReportSchedulerService>();
                await service.GenerateWeeklyDraftsAsync(today, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Weekly report auto-generation failed on {Date}.", today);
            }
        }
    }

    private static TimeSpan TimeUntilNextFriday(TimeSpan targetUtc)
    {
        var now = DateTime.UtcNow;
        var daysUntilFriday = ((int)DayOfWeek.Friday - (int)now.DayOfWeek + 7) % 7;
        var next = now.Date.AddDays(daysUntilFriday).Add(targetUtc);
        if (now >= next) next = next.AddDays(7);
        return next - now;
    }
}

// ── P6: Daily compliance digest to manager (2 PM IST = 08:30 AM UTC) ────────

public interface IComplianceDigestService
{
    Task SendComplianceDigestAsync(DateOnly today, CancellationToken ct);
}

public sealed class ComplianceDigestService(
    AppDbContext db,
    IZohoBridge zohoBridge,
    ILogger<ComplianceDigestService> logger) : IComplianceDigestService
{
    public async Task SendComplianceDigestAsync(DateOnly today, CancellationToken ct)
    {
        if (today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) return;

        var managers = db.Users
            .Where(x => x.IsActive && (x.Role == AppRole.Manager || x.Role == AppRole.Admin))
            .ToList();

        foreach (var manager in managers)
        {
            // Users this manager can see
            var teamIds = db.Users
                .Where(x => x.IsActive && x.ManagerId == manager.UserId)
                .Select(x => x.UserId)
                .ToList();

            if (teamIds.Count == 0) continue;

            var totalTeam = teamIds.Count;

            // Who submitted today
            var submittedIds = db.DailyUpdates
                .Where(x => x.WorkDate == today && teamIds.Contains(x.UserId))
                .Select(x => x.UserId)
                .Distinct()
                .ToHashSet();

            var submittedCount = submittedIds.Count;
            var participationPct = totalTeam == 0 ? 0 : (submittedCount * 100 / totalTeam);

            // Non-submitters names
            var nonSubmitterNames = db.Users
                .Where(x => teamIds.Contains(x.UserId) && !submittedIds.Contains(x.UserId))
                .Select(x => x.Name)
                .ToList();

            // Blocked tickets today
            var blockedCount = db.DailyUpdates
                .Count(x => x.WorkDate == today
                            && teamIds.Contains(x.UserId)
                            && x.Status == DailyStatus.Blocked);

            // Pending validations (achievements + sales)
            var pendingAchievements = db.Achievements
                .Count(x => x.ValidationStatus == ValidationStatus.Pending
                            && teamIds.Contains(x.UserId));

            var pendingSales = db.SalesEnquiries
                .Count(x => x.ValidationStatus == ValidationStatus.Pending
                            && teamIds.Contains(x.CreatedByUserId));

            var totalPending = pendingAchievements + pendingSales;

            // Build message
            var lines = new List<string>
            {
                $"📊 *Daily Team Digest — {today:dd MMM yyyy}*",
                $"",
                $"✅ Participation: {submittedCount}/{totalTeam} ({participationPct}%)"
            };

            if (nonSubmitterNames.Count > 0)
                lines.Add($"⚠️  Missing: {string.Join(", ", nonSubmitterNames)}");

            if (blockedCount > 0)
                lines.Add($"🚫 Blocked tickets: {blockedCount}");

            if (totalPending > 0)
                lines.Add($"📋 Pending validations: {totalPending} ({pendingAchievements} achievements, {pendingSales} sales)");

            if (blockedCount == 0 && totalPending == 0 && submittedCount == totalTeam)
                lines.Add("🎉 All submissions in, no blockers, no pending items!");

            lines.Add("");
            lines.Add("Open KudosApp for details.");

            var message = string.Join("\n", lines);
            await zohoBridge.SendCliqNotificationAsync(message, [manager.Email], ct);

            logger.LogInformation(
                "Compliance digest sent to manager {UserId}: {Submitted}/{Total} submitted, {Blocked} blocked, {Pending} pending.",
                manager.UserId, submittedCount, totalTeam, blockedCount, totalPending);
        }
    }
}

public sealed class ComplianceDigestHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<ComplianceDigestHostedService> logger) : BackgroundService
{
    // 2 PM IST = 08:30 AM UTC
    private static readonly TimeSpan FireAtUtc = TimeSpan.FromHours(8).Add(TimeSpan.FromMinutes(30));

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("ComplianceDigestHostedService started.");

        while (!ct.IsCancellationRequested)
        {
            var delay = TimeUntilNext(FireAtUtc);
            logger.LogInformation("Compliance digest sleeping {Minutes:F0} min until next 2 PM IST.", delay.TotalMinutes);

            await Task.Delay(delay, ct);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IComplianceDigestService>();
                await service.SendComplianceDigestAsync(today, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Compliance digest job failed on {Date}.", today);
            }
        }
    }

    private static TimeSpan TimeUntilNext(TimeSpan targetUtc)
    {
        var now = DateTime.UtcNow;
        var next = now.Date.Add(targetUtc);
        if (now >= next) next = next.AddDays(1);
        return next - now;
    }
}
