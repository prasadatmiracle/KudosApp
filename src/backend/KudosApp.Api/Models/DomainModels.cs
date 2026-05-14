using System.Text.Json.Serialization;

namespace KudosApp.Api.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AppRole
{
    Employee,
    Manager,
    Admin,
    Hr
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BillingType
{
    Billable,
    NonBillable,
    Shadow,
    Trainee,
    Overhead
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TaskType
{
    Vote,
    Action,
    Info
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TaskState
{
    Active,
    Closed
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DailyStatus
{
    Open,
    InProgress,
    Completed,
    Blocked,
    NoTask
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ValidationEntityType
{
    Achievement,
    SalesEnquiry
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ValidationStatus
{
    Pending,
    Approved,
    Rejected
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReportType
{
    Weekly,
    Monthly,
    Quarterly
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReportStatus
{
    Draft,
    Finalized,
    Locked
}

public sealed class Team
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class UserProfile
{
    public int UserId { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public AppRole Role { get; set; } = AppRole.Employee;
    public int TeamId { get; set; }
    public int? ManagerId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class Project
{
    public int ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ResourceAllocation
{
    public int ResourceAllocationId { get; set; }
    public int UserId { get; set; }
    public int ProjectId { get; set; }
    public BillingType BillingType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class DailyUpdate
{
    public int DailyUpdateId { get; set; }
    public int UserId { get; set; }
    public int ProjectId { get; set; }
    public DateOnly WorkDate { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DailyStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class TaskItem
{
    public int TaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskType TaskType { get; set; } = TaskType.Vote;
    public TaskState State { get; set; } = TaskState.Active;
    public int CreatedByUserId { get; set; }
    public int? ProjectId { get; set; }
    public DateTime DueAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class TaskResponse
{
    public int TaskResponseId { get; set; }
    public int TaskId { get; set; }
    public int UserId { get; set; }
    public string Option { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class Achievement
{
    public int AchievementId { get; set; }
    public int UserId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ProofWorkDriveUrl { get; set; }
    public ValidationStatus ValidationStatus { get; set; } = ValidationStatus.Pending;
    public int? ValidatedByUserId { get; set; }
    public DateTime? ValidatedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class SalesEnquiry
{
    public int SalesEnquiryId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string Requirement { get; set; } = string.Empty;
    public string Technology { get; set; } = string.Empty;
    public DateOnly EnquiryDate { get; set; }
    public string SalesCoordinator { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public int CreatedByUserId { get; set; }
    public ValidationStatus ValidationStatus { get; set; } = ValidationStatus.Pending;
    public int? ValidatedByUserId { get; set; }
    public DateTime? ValidatedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class Engagement
{
    public int EngagementId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public int NumberOfPositions { get; set; }
    public string Details { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class SalesSession
{
    public int SalesSessionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly SessionDate { get; set; }
    public int TeamId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class EventItem
{
    public int EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class EventMedia
{
    public int EventMediaId { get; set; }
    public int EventId { get; set; }
    public string WorkDriveFileUrl { get; set; } = string.Empty;
    public int UploadedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class MeetingRecord
{
    public int MeetingId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime MeetingAtUtc { get; set; }
    public string ZohoMeetingUrl { get; set; } = string.Empty;
    public string? TranscriptUrl { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class MomEntry
{
    public int MomEntryId { get; set; }
    public int MeetingId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string ActionItems { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ValidationRecord
{
    public int ValidationRecordId { get; set; }
    public ValidationEntityType EntityType { get; set; }
    public int EntityId { get; set; }
    public ValidationStatus Status { get; set; } = ValidationStatus.Pending;
    public int? ValidatedByUserId { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class PointsLog
{
    public int PointsLogId { get; set; }
    public int UserId { get; set; }
    public int Points { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public int? ReferenceId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class Badge
{
    public int BadgeId { get; set; }
    public string BadgeName { get; set; } = string.Empty;
    public string Criteria { get; set; } = string.Empty;
}

public sealed class UserBadge
{
    public int UserBadgeId { get; set; }
    public int UserId { get; set; }
    public int BadgeId { get; set; }
    public DateTime AwardedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ReportRecord
{
    public int ReportRecordId { get; set; }
    public ReportType ReportType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public ReportStatus Status { get; set; } = ReportStatus.Draft;
    public int GeneratedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class AuditEntry
{
    public int AuditEntryId { get; set; }
    public int ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ReminderDispatch
{
    public int ReminderDispatchId { get; set; }
    public int UserId { get; set; }
    public DateOnly DispatchDate { get; set; }
    public int Count { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ActionItemPriority { Low, Medium, High, Critical }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ActionItemStatus { Open, InProgress, Completed, Cancelled }

public sealed class ActionItem
{
    public int ActionItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int AssignedToUserId { get; set; }
    public int CreatedByUserId { get; set; }
    public DateOnly DueDate { get; set; }
    public ActionItemPriority Priority { get; set; } = ActionItemPriority.Medium;
    public ActionItemStatus Status { get; set; } = ActionItemStatus.Open;

    /// <summary>Where this item originated: Manual | MOM | Meeting</summary>
    public string SourceType { get; set; } = "Manual";

    /// <summary>Id of the source record (e.g. MeetingId) when SourceType != Manual.</summary>
    public int? SourceId { get; set; }

    /// <summary>Date of the Monday reminder sent to the assignee this cycle.</summary>
    public DateOnly? FirstReminderSentDate { get; set; }

    /// <summary>Date of the Wednesday escalation sent to the assignee's manager.</summary>
    public DateOnly? EscalationSentDate { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

// ── P9B: Smart Inbox Task Capture ─────────────────────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InboxTaskState
{
    PendingConfirmation,
    Active,
    InProgress,
    Completed,
    Dismissed
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InboxTaskCategory
{
    Development,
    FollowUp,
    StatusUpdate,
    ReportGeneration,
    Support,
    Communicate,
    Custom
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InboxTaskPriority { Low, Medium, High, Critical }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InboxTaskWeeklyCategory { RoutineTask, Accomplishment, Achievement, Other }

public sealed class InboxTask
{
    public int InboxTaskId { get; set; }
    public int UserId { get; set; }
    public string SourceChannel { get; set; } = string.Empty;   // ZohoMail | ZohoCliq
    public string SourceSender { get; set; } = string.Empty;
    public string SourceMessageId { get; set; } = string.Empty;
    public string SourcePreview { get; set; } = string.Empty;
    public string ExtractedTaskText { get; set; } = string.Empty;
    public string DeduplicationHash { get; set; } = string.Empty;
    public bool IsPrivate { get; set; } = true;
    public InboxTaskCategory Category { get; set; } = InboxTaskCategory.FollowUp;
    public string? CustomCategoryName { get; set; }
    public InboxTaskPriority Priority { get; set; } = InboxTaskPriority.Medium;
    public InboxTaskState State { get; set; } = InboxTaskState.PendingConfirmation;
    public DateTime? DueAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public bool IncludeInWeeklyReport { get; set; } = false;
    public InboxTaskWeeklyCategory? WeeklyReportCategory { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class InboxTaskDependency
{
    public int InboxTaskDependencyId { get; set; }
    public int InboxTaskId { get; set; }
    public int DependentUserId { get; set; }
    public DateTime? NotifiedCompletedAtUtc { get; set; }
}

public sealed class InboxTaskReminder
{
    public int InboxTaskReminderId { get; set; }
    public int InboxTaskId { get; set; }
    public DateTime RemindAtUtc { get; set; }
    public string Channel { get; set; } = "InApp";   // Cliq | InApp | Both
    public bool IsSent { get; set; } = false;
}
