using System.Text;
using System.Text.Json;
using KudosApp.Api.Data;
using KudosApp.Api.DTOs;
using KudosApp.Api.Models;

namespace KudosApp.Api.Services;

/// <summary>
/// P15 — Generates a human-readable narrative summary from a report's structured payload.
/// Stub: derives text from data without AI. Replace Generate() with Azure OpenAI when available.
/// </summary>
public static class NarrativeService
{
    public static string Generate(ReportRecord report, AppDbContext db)
    {
        return report.ReportType switch
        {
            ReportType.Weekly    => WeeklyNarrative(report),
            ReportType.Monthly   => MonthlyNarrative(report, db),
            ReportType.Quarterly => QuarterlyNarrative(report),
            _                    => $"{report.ReportType} report for {report.StartDate:dd MMM} – {report.EndDate:dd MMM yyyy}."
        };
    }

    private static string WeeklyNarrative(ReportRecord report)
    {
        var payload = TryDeserialize<WeeklyReportPayload>(report.PayloadJson);
        if (payload is null)
            return $"Weekly report for {report.StartDate:dd MMM} – {report.EndDate:dd MMM yyyy}.";

        var tickets   = payload.Tickets;
        var total     = tickets.Count;
        var completed = tickets.Count(t => t.Status == DailyStatus.Completed);
        var blocked   = tickets.Count(t => t.Status == DailyStatus.Blocked);
        var inProgress = tickets.Count(t => t.Status == DailyStatus.InProgress);

        var projects  = tickets.Select(t => t.ProjectName).Distinct().ToList();
        var owners    = tickets.Select(t => t.OwnerName).Distinct().ToList();

        var sb = new StringBuilder();
        sb.Append($"Week of {report.StartDate:dd MMM} – {report.EndDate:dd MMM yyyy}. ");
        sb.Append($"The team tracked {total} ticket(s) across {projects.Count} project(s) ");
        sb.Append($"({string.Join(", ", projects.Take(3))}{(projects.Count > 3 ? " and others" : "")}). ");

        if (completed > 0) sb.Append($"{completed} ticket(s) were completed. ");
        if (inProgress > 0) sb.Append($"{inProgress} are still in progress. ");

        if (blocked > 0)
        {
            var blockedTickets = tickets.Where(t => t.Status == DailyStatus.Blocked)
                                        .Select(t => t.TicketNumber).Take(3);
            sb.Append($"{blocked} ticket(s) are currently blocked ({string.Join(", ", blockedTickets)}");
            if (blocked > 3) sb.Append($" and {blocked - 3} more");
            sb.Append(") — these may need manager attention. ");
        }

        sb.Append($"{owners.Count} team member(s) contributed updates this week.");

        if (!string.IsNullOrWhiteSpace(payload.ManagerNotes))
            sb.Append($" Manager notes: {payload.ManagerNotes.Trim()}");

        return sb.ToString();
    }

    private static string MonthlyNarrative(ReportRecord report, AppDbContext db)
    {
        var payload = TryDeserialize<MonthlyReportSection>(report.PayloadJson);
        if (payload is null)
            return $"Monthly report for {report.StartDate:MMMM yyyy}.";

        var sb = new StringBuilder();
        sb.Append($"{report.StartDate:MMMM yyyy} Monthly Summary. ");

        var headcount = payload.ResourceUtilization.Values.Sum();
        if (headcount > 0)
        {
            sb.Append($"Team headcount: {headcount}");
            var billing = payload.ResourceUtilization
                .OrderByDescending(kv => kv.Value)
                .Select(kv => $"{kv.Value} {kv.Key}");
            sb.Append($" ({string.Join(", ", billing)}). ");
        }

        if (payload.Engagements.Count > 0)
        {
            var positions = payload.Engagements.Sum(e => e.NumberOfPositions);
            sb.Append($"{payload.Engagements.Count} client engagement(s) covering {positions} open position(s). ");
        }

        if (payload.ApprovedAchievements.Count > 0)
        {
            var categories = payload.ApprovedAchievements.GroupBy(a => a.Category)
                .Select(g => $"{g.Count()} {g.Key}");
            sb.Append($"{payload.ApprovedAchievements.Count} achievement(s) approved this month ({string.Join(", ", categories)}). ");
        }

        if (payload.ApprovedSalesEnquiries.Count > 0)
            sb.Append($"{payload.ApprovedSalesEnquiries.Count} sales enquiry(ies) won. ");

        if (payload.SalesSessions.Count > 0)
            sb.Append($"{payload.SalesSessions.Count} sales enablement session(s) conducted. ");

        if (payload.Events.Count > 0)
        {
            var eventNames = payload.Events.Select(e => e.Title).Take(3);
            sb.Append($"{payload.Events.Count} team event(s) held ({string.Join(", ", eventNames)}). ");
        }

        // Participation from daily updates
        var startDt = report.StartDate.ToDateTime(TimeOnly.MinValue);
        var endDt   = report.EndDate.ToDateTime(TimeOnly.MaxValue);
        var updateCount = db.DailyUpdates
            .Count(x => x.WorkDate >= report.StartDate && x.WorkDate <= report.EndDate);
        if (updateCount > 0)
            sb.Append($"{updateCount} daily update entries logged this month.");

        return sb.ToString();
    }

    private static string QuarterlyNarrative(ReportRecord report)
    {
        var payload = TryDeserialize<QuarterlyReportSection>(report.PayloadJson);
        if (payload is null)
            return $"Q{((report.StartDate.Month - 1) / 3) + 1} {report.StartDate.Year} quarterly report.";

        var sb = new StringBuilder();
        sb.Append($"Q{payload.Quarter} {payload.Year} Quarterly Review. ");

        var totalEnquiries    = payload.EnquiryCountByMonth.Values.Sum();
        var totalAchievements = payload.AchievementCountByMonth.Values.Sum();
        var totalParticipation = payload.ParticipationByMonth.Values.Sum();

        if (totalEnquiries > 0)
            sb.Append($"{totalEnquiries} sales enquiry(ies) across the quarter. ");

        if (totalAchievements > 0)
            sb.Append($"{totalAchievements} team achievement(s) approved. ");

        if (totalParticipation > 0)
            sb.Append($"{totalParticipation} daily update entries logged. ");

        // Month-by-month trend
        var months = payload.EnquiryCountByMonth.Keys.OrderBy(k => k).ToList();
        if (months.Count > 0)
        {
            var trendParts = months.Select(m =>
                $"{m}: {payload.EnquiryCountByMonth.GetValueOrDefault(m, 0)} enquiries, " +
                $"{payload.AchievementCountByMonth.GetValueOrDefault(m, 0)} achievements");
            sb.Append($"Month breakdown — {string.Join("; ", trendParts)}.");
        }

        return sb.ToString();
    }

    private static T? TryDeserialize<T>(string json) where T : class
    {
        try { return JsonSerializer.Deserialize<T>(json); }
        catch { return null; }
    }
}
