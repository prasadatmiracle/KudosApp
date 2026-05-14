using KudosApp.Api.Models;

namespace KudosApp.Api.DTOs;

public sealed class ZohoSsoLoginRequest
{
    public string ZohoAccessToken { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public sealed class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public UserSummary User { get; set; } = new();
}

public sealed class UserSummary
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public AppRole Role { get; set; }
    public int? ManagerId { get; set; }
    public int TeamId { get; set; }
}

public sealed class DailyUpdateInput
{
    public int ProjectId { get; set; }
    public DateOnly WorkDate { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DailyStatus Status { get; set; }
}

public sealed class CreateTaskInput
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskType TaskType { get; set; } = TaskType.Vote;
    public int? ProjectId { get; set; }
    public DateTime DueAtUtc { get; set; }
}

public sealed class TaskResponseInput
{
    public string Option { get; set; } = string.Empty;
    public string? Remark { get; set; }
}

public sealed class TaskResponseExportRow
{
    public string Name { get; set; } = string.Empty;
    public string Option { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class CreateAchievementInput
{
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ProofWorkDriveUrl { get; set; }
}

public sealed class CreateSalesEnquiryInput
{
    public string ClientName { get; set; } = string.Empty;
    public string Requirement { get; set; } = string.Empty;
    public string Technology { get; set; } = string.Empty;
    public DateOnly EnquiryDate { get; set; }
    public string SalesCoordinator { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
}

public sealed class ValidationDecision
{
    public ValidationStatus Status { get; set; }
    public string? Remarks { get; set; }
}

public sealed class BulkValidationDecision
{
    public List<int> ValidationRecordIds { get; set; } = [];
    public ValidationStatus Status { get; set; }
    public string? Remarks { get; set; }
}

public sealed class CreateEventInput
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
}

public sealed class AddEventMediaInput
{
    public string WorkDriveFileUrl { get; set; } = string.Empty;
}

public sealed class CreateMeetingInput
{
    public string Title { get; set; } = string.Empty;
    public DateTime MeetingAtUtc { get; set; }
    public string ZohoMeetingUrl { get; set; } = string.Empty;
    public string? TranscriptUrl { get; set; }
}

public sealed class UploadMomInput
{
    public string Summary { get; set; } = string.Empty;
    public string ActionItems { get; set; } = string.Empty;
}

public sealed class TranscriptIngestInput
{
    public string TranscriptText { get; set; } = string.Empty;
}

public sealed class CreateEngagementInput
{
    public string ClientName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public int NumberOfPositions { get; set; }
    public string Details { get; set; } = string.Empty;
}

public sealed class CreateSalesSessionInput
{
    public string Title { get; set; } = string.Empty;
    public DateOnly SessionDate { get; set; }
    public int TeamId { get; set; }
    public string Description { get; set; } = string.Empty;
}

public sealed class WeeklyTicketRow
{
    public string ProjectName { get; set; } = string.Empty;
    public string TicketNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public DailyStatus Status { get; set; }
    public DateOnly WorkDate { get; set; }
}

public sealed class WeeklyReportPayload
{
    public List<WeeklyTicketRow> Tickets { get; set; } = [];
    public string ManagerNotes { get; set; } = string.Empty;
}

public sealed class MonthlyReportSection
{
    public Dictionary<string, int> ResourceUtilization { get; set; } = [];
    public List<Engagement> Engagements { get; set; } = [];
    public List<Achievement> ApprovedAchievements { get; set; } = [];
    public List<SalesEnquiry> ApprovedSalesEnquiries { get; set; } = [];
    public List<SalesSession> SalesSessions { get; set; } = [];
    public List<EventItem> Events { get; set; } = [];
}

public sealed class QuarterlyReportSection
{
    public int Year { get; set; }
    public int Quarter { get; set; }
    public Dictionary<string, int> EnquiryCountByMonth { get; set; } = [];
    public Dictionary<string, int> AchievementCountByMonth { get; set; } = [];
    public Dictionary<string, int> ParticipationByMonth { get; set; } = [];
}

public sealed class ExportArtifact
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text/plain";
    public string Base64Content { get; set; } = string.Empty;
}

public sealed class ReportEditInput
{
    public string ManagerNotes { get; set; } = string.Empty;
}

public sealed class NotificationInput
{
    public string Message { get; set; } = string.Empty;
    public List<int> UserIds { get; set; } = [];
}

public sealed class CsvUserImportRow
{
    public string EmployeeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public AppRole Role { get; set; } = AppRole.Employee;
    public int TeamId { get; set; }
    public int? ManagerId { get; set; }
}

public sealed class CreateActionItemInput
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int AssignedToUserId { get; set; }
    public DateOnly DueDate { get; set; }
    public ActionItemPriority Priority { get; set; } = ActionItemPriority.Medium;
    public string SourceType { get; set; } = "Manual";
    public int? SourceId { get; set; }
}

public sealed class UpdateActionItemStatusInput
{
    public ActionItemStatus Status { get; set; }
}

public sealed class ActionItemRow
{
    public int ActionItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int AssignedToUserId { get; set; }
    public string AssigneeName { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public ActionItemPriority Priority { get; set; }
    public ActionItemStatus Status { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public int? SourceId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public bool IsOverdue { get; set; }
}
