using KudosApp.Api.Data;
using KudosApp.Api.Models;

namespace KudosApp.Api.Services;

// ── Business logic service (Scoped) ─────────────────────────────────────────

public interface IActionItemService
{
    Task SendAssigneeRemindersAsync(DateOnly today, CancellationToken ct);
    Task SendManagerEscalationsAsync(DateOnly today, CancellationToken ct);
}

public sealed class ActionItemService(
    AppDbContext db,
    IZohoBridge zohoBridge,
    ILogger<ActionItemService> logger) : IActionItemService
{
    /// <summary>
    /// Called every Monday. Groups all Open/InProgress items by assignee,
    /// sends one Cliq DM per person listing their open items, marks reminder date.
    /// </summary>
    public async Task SendAssigneeRemindersAsync(DateOnly today, CancellationToken ct)
    {
        var openItems = db.ActionItems
            .Where(x => x.Status == ActionItemStatus.Open || x.Status == ActionItemStatus.InProgress)
            .ToList();

        if (openItems.Count == 0) return;

        var assigneeIds = openItems.Select(x => x.AssignedToUserId).Distinct().ToList();
        var assignees = db.Users
            .Where(x => assigneeIds.Contains(x.UserId))
            .ToDictionary(x => x.UserId);

        var grouped = openItems.GroupBy(x => x.AssignedToUserId);

        foreach (var group in grouped)
        {
            if (!assignees.TryGetValue(group.Key, out var assignee)) continue;

            var lines = group
                .OrderBy(x => x.DueDate)
                .Select(x => $"• [{x.Priority}] {x.Title} — due {x.DueDate:dd MMM}");

            var message = $"Hi {assignee.Name}, you have {group.Count()} open action item(s) this week:\n"
                          + string.Join("\n", lines)
                          + "\nPlease update the status in KudosApp.";

            await zohoBridge.SendCliqNotificationAsync(message, [assignee.Email], ct);

            foreach (var item in group)
                item.FirstReminderSentDate = today;
        }

        db.SaveChanges();
        logger.LogInformation("Action item Monday reminders sent to {Count} assignees.", grouped.Count());
    }

    /// <summary>
    /// Called every Wednesday. Escalates items that were reminded on the preceding
    /// Monday (today - 2 days) and are still open. Sends one DM to the assignee's manager.
    /// </summary>
    public async Task SendManagerEscalationsAsync(DateOnly today, CancellationToken ct)
    {
        var lastMonday = today.AddDays(-2);

        var overdueItems = db.ActionItems
            .Where(x => (x.Status == ActionItemStatus.Open || x.Status == ActionItemStatus.InProgress)
                        && x.FirstReminderSentDate == lastMonday
                        && x.EscalationSentDate == null)
            .ToList();

        if (overdueItems.Count == 0) return;

        var allUserIds = overdueItems
            .SelectMany(x => new[] { x.AssignedToUserId, x.CreatedByUserId })
            .Distinct()
            .ToList();

        var users = db.Users
            .Where(x => allUserIds.Contains(x.UserId))
            .ToDictionary(x => x.UserId);

        // Group by assignee's manager
        var byManager = overdueItems
            .GroupBy(x => users.TryGetValue(x.AssignedToUserId, out var u) ? u.ManagerId : null)
            .Where(g => g.Key.HasValue);

        foreach (var group in byManager)
        {
            var managerId = group.Key!.Value;
            if (!users.TryGetValue(managerId, out var manager)) continue;

            var lines = group
                .OrderBy(x => x.DueDate)
                .Select(x =>
                {
                    var assigneeName = users.TryGetValue(x.AssignedToUserId, out var a) ? a.Name : "Unknown";
                    return $"• [{x.Priority}] {x.Title} → {assigneeName}, due {x.DueDate:dd MMM}";
                });

            var message = $"Hi {manager.Name}, the following action items are still open after Monday's reminder:\n"
                          + string.Join("\n", lines)
                          + "\nPlease follow up with your team.";

            await zohoBridge.SendCliqNotificationAsync(message, [manager.Email], ct);

            foreach (var item in group)
                item.EscalationSentDate = today;
        }

        db.SaveChanges();
        logger.LogInformation("Action item Wednesday escalations sent for {Count} overdue items.", overdueItems.Count);
    }
}

// ── Background hosted service (Singleton-lifetime via IHostedService) ────────

public sealed class ActionItemReminderHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<ActionItemReminderHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("ActionItemReminderHostedService started.");

        while (!ct.IsCancellationRequested)
        {
            var delay = TimeUntilNext8AmUtc();
            logger.LogInformation("Action item reminder job sleeping for {Minutes:F0} min until next 8 AM UTC.", delay.TotalMinutes);

            await Task.Delay(delay, ct);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IActionItemService>();

                if (today.DayOfWeek == DayOfWeek.Monday)
                {
                    logger.LogInformation("Running Monday assignee reminders ({Date}).", today);
                    await service.SendAssigneeRemindersAsync(today, ct);
                }
                else if (today.DayOfWeek == DayOfWeek.Wednesday)
                {
                    logger.LogInformation("Running Wednesday manager escalations ({Date}).", today);
                    await service.SendManagerEscalationsAsync(today, ct);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Action item reminder job failed on {Date}.", today);
            }
        }
    }

    private static TimeSpan TimeUntilNext8AmUtc()
    {
        var now = DateTime.UtcNow;
        var next = now.Date.AddHours(8);
        if (now >= next) next = next.AddDays(1);
        return next - now;
    }
}
