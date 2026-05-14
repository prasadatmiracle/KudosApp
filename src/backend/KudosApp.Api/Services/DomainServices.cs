using System.Text;
using System.Text.Json;
using KudosApp.Api.Data;
using KudosApp.Api.DTOs;
using KudosApp.Api.Models;

namespace KudosApp.Api.Services;

public interface IVisibilityService
{
    bool CanViewUser(int viewerUserId, AppRole viewerRole, int targetUserId);
    IReadOnlyCollection<int> TeamViewableUserIds(int managerUserId);
}

public sealed class VisibilityService(AppDbContext db) : IVisibilityService
{
    public bool CanViewUser(int viewerUserId, AppRole viewerRole, int targetUserId)
    {
        if (viewerRole is AppRole.Admin) return true;
        if (viewerUserId == targetUserId) return true;
        return TeamViewableUserIds(viewerUserId).Contains(targetUserId);
    }

    public IReadOnlyCollection<int> TeamViewableUserIds(int managerUserId)
    {
        var direct = db.Users.Where(x => x.ManagerId == managerUserId).Select(x => x.UserId).ToHashSet();
        var skipLevel = db.Users
            .Where(x => x.ManagerId.HasValue && direct.Contains(x.ManagerId.Value))
            .Select(x => x.UserId)
            .ToList();
        foreach (var id in skipLevel) direct.Add(id);
        direct.Add(managerUserId);
        return direct.ToArray();
    }
}

public interface IPointsService
{
    void AddPoints(int userId, int points, string activityType, int? referenceId = null);
}

public sealed class PointsService(AppDbContext db) : IPointsService
{
    public void AddPoints(int userId, int points, string activityType, int? referenceId = null)
    {
        db.Points.Add(new PointsLog
        {
            UserId = userId,
            Points = points,
            ActivityType = activityType,
            ReferenceId = referenceId,
            CreatedAtUtc = DateTime.UtcNow
        });
        db.SaveChanges();
    }
}

public interface IReportService
{
    ReportRecord GenerateWeekly(int actorUserId, DateOnly startDate, DateOnly endDate);
    ReportRecord GenerateMonthly(int actorUserId, int year, int month);
    ReportRecord GenerateQuarterly(int actorUserId, int year, int quarter);
    ExportArtifact Export(ReportRecord report, string format);
}

public sealed class ReportService(AppDbContext db) : IReportService
{
    public ReportRecord GenerateWeekly(int actorUserId, DateOnly startDate, DateOnly endDate)
    {
        var updates = db.DailyUpdates
            .Where(x => x.WorkDate >= startDate && x.WorkDate <= endDate)
            .ToList();

        var userIds = updates.Select(x => x.UserId).Distinct().ToHashSet();
        var projectIds = updates.Select(x => x.ProjectId).Distinct().ToHashSet();
        var users = db.Users.Where(x => userIds.Contains(x.UserId)).ToDictionary(x => x.UserId);
        var projects = db.Projects.Where(x => projectIds.Contains(x.ProjectId)).ToDictionary(x => x.ProjectId);

        var rows = updates
            .GroupBy(x => new { x.ProjectId, x.TicketNumber })
            .Select(g => g.OrderByDescending(x => x.WorkDate).ThenByDescending(x => x.CreatedAtUtc).First())
            .Select(x => new WeeklyTicketRow
            {
                ProjectName = projects.TryGetValue(x.ProjectId, out var p) ? p.ProjectName : "Unknown",
                TicketNumber = x.TicketNumber,
                Description = x.Description,
                OwnerName = users.TryGetValue(x.UserId, out var u) ? u.Name : "Unknown",
                Status = x.Status,
                WorkDate = x.WorkDate
            })
            .OrderBy(x => x.ProjectName)
            .ThenBy(x => x.TicketNumber)
            .ToList();

        var payload = new WeeklyReportPayload { Tickets = rows, ManagerNotes = string.Empty };
        return SaveReport(actorUserId, ReportType.Weekly, startDate, endDate, JsonSerializer.Serialize(payload));
    }

    public ReportRecord GenerateMonthly(int actorUserId, int year, int month)
    {
        var payload = new MonthlyReportSection
        {
            ResourceUtilization = db.ResourceAllocations
                .Where(x => x.IsActive)
                .GroupBy(x => x.BillingType.ToString())
                .ToDictionary(g => g.Key, g => g.Count()),
            Engagements = db.Engagements
                .Where(x => x.CreatedAtUtc.Year == year && x.CreatedAtUtc.Month == month).ToList(),
            ApprovedAchievements = db.Achievements
                .Where(x => x.ValidationStatus == ValidationStatus.Approved && x.CreatedAtUtc.Year == year && x.CreatedAtUtc.Month == month).ToList(),
            ApprovedSalesEnquiries = db.SalesEnquiries
                .Where(x => x.ValidationStatus == ValidationStatus.Approved && x.EnquiryDate.Year == year && x.EnquiryDate.Month == month).ToList(),
            SalesSessions = db.SalesSessions
                .Where(x => x.SessionDate.Year == year && x.SessionDate.Month == month).ToList(),
            Events = db.Events
                .Where(x => x.EventDate.Year == year && x.EventDate.Month == month).ToList()
        };

        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        return SaveReport(actorUserId, ReportType.Monthly, start, end, JsonSerializer.Serialize(payload));
    }

    public ReportRecord GenerateQuarterly(int actorUserId, int year, int quarter)
    {
        if (quarter < 1 || quarter > 4) throw new ArgumentOutOfRangeException(nameof(quarter));

        var firstMonth = ((quarter - 1) * 3) + 1;
        var months = new[] { firstMonth, firstMonth + 1, firstMonth + 2 };

        var payload = new QuarterlyReportSection
        {
            Year = year,
            Quarter = quarter,
            EnquiryCountByMonth = months.ToDictionary(
                m => new DateOnly(year, m, 1).ToString("yyyy-MM"),
                m => db.SalesEnquiries.Count(x => x.EnquiryDate.Year == year && x.EnquiryDate.Month == m)),
            AchievementCountByMonth = months.ToDictionary(
                m => new DateOnly(year, m, 1).ToString("yyyy-MM"),
                m => db.Achievements.Count(x => x.CreatedAtUtc.Year == year && x.CreatedAtUtc.Month == m && x.ValidationStatus == ValidationStatus.Approved)),
            ParticipationByMonth = months.ToDictionary(
                m => new DateOnly(year, m, 1).ToString("yyyy-MM"),
                m => db.DailyUpdates.Count(x => x.WorkDate.Year == year && x.WorkDate.Month == m))
        };

        var start = new DateOnly(year, firstMonth, 1);
        var end = start.AddMonths(3).AddDays(-1);
        return SaveReport(actorUserId, ReportType.Quarterly, start, end, JsonSerializer.Serialize(payload));
    }

    public ExportArtifact Export(ReportRecord report, string format)
    {
        var extension = format.Equals("excel", StringComparison.OrdinalIgnoreCase) ? "csv" : "txt";
        var contentType = extension == "csv" ? "text/csv" : "text/plain";
        var fileName = $"{report.ReportType}-{report.StartDate:yyyyMMdd}-{report.EndDate:yyyyMMdd}.{extension}";

        string content;
        if (extension == "csv")
        {
            content = $"report_id,report_type,status,start_date,end_date{Environment.NewLine}"
                    + $"{report.ReportRecordId},{report.ReportType},{report.Status},{report.StartDate:yyyy-MM-dd},{report.EndDate:yyyy-MM-dd}";
        }
        else
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Kudos Report: {report.ReportType}");
            sb.AppendLine($"Window: {report.StartDate:yyyy-MM-dd} to {report.EndDate:yyyy-MM-dd}");
            sb.AppendLine($"Status: {report.Status}");
            sb.AppendLine();
            sb.AppendLine(report.PayloadJson);
            content = sb.ToString();
        }

        return new ExportArtifact
        {
            FileName = fileName,
            ContentType = contentType,
            Base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(content))
        };
    }

    private ReportRecord SaveReport(int actorUserId, ReportType type, DateOnly startDate, DateOnly endDate, string payloadJson)
    {
        var record = new ReportRecord
        {
            ReportType = type,
            StartDate = startDate,
            EndDate = endDate,
            PayloadJson = payloadJson,
            Status = ReportStatus.Draft,
            GeneratedByUserId = actorUserId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.Reports.Add(record);
        db.SaveChanges();
        return record;
    }
}
