using KudosApp.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace KudosApp.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<UserProfile> Users => Set<UserProfile>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ResourceAllocation> ResourceAllocations => Set<ResourceAllocation>();
    public DbSet<DailyUpdate> DailyUpdates => Set<DailyUpdate>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<TaskResponse> TaskResponses => Set<TaskResponse>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<SalesEnquiry> SalesEnquiries => Set<SalesEnquiry>();
    public DbSet<Engagement> Engagements => Set<Engagement>();
    public DbSet<SalesSession> SalesSessions => Set<SalesSession>();
    public DbSet<EventItem> Events => Set<EventItem>();
    public DbSet<EventMedia> EventMedia => Set<EventMedia>();
    public DbSet<MeetingRecord> Meetings => Set<MeetingRecord>();
    public DbSet<MomEntry> MomEntries => Set<MomEntry>();
    public DbSet<ValidationRecord> Validations => Set<ValidationRecord>();
    public DbSet<PointsLog> Points => Set<PointsLog>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<UserBadge> UserBadges => Set<UserBadge>();
    public DbSet<ReportRecord> Reports => Set<ReportRecord>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<ReminderDispatch> ReminderDispatches => Set<ReminderDispatch>();
    public DbSet<ActionItem> ActionItems => Set<ActionItem>();
    public DbSet<InboxTask> InboxTasks => Set<InboxTask>();
    public DbSet<InboxTaskDependency> InboxTaskDependencies => Set<InboxTaskDependency>();
    public DbSet<InboxTaskReminder> InboxTaskReminders => Set<InboxTaskReminder>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        // ── Teams ──────────────────────────────────────────────────────────────
        m.Entity<Team>(e =>
        {
            e.ToTable("Teams");
            e.HasKey(x => x.TeamId);
            e.Property(x => x.TeamName).HasMaxLength(200).IsRequired();
        });

        // ── Users ──────────────────────────────────────────────────────────────
        m.Entity<UserProfile>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.UserId);
            e.Property(x => x.EmployeeId).HasMaxLength(50).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(50).IsRequired();
            e.HasOne<Team>().WithMany().HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.ManagerId)
             .IsRequired(false).OnDelete(DeleteBehavior.NoAction);
        });

        // ── Projects ───────────────────────────────────────────────────────────
        m.Entity<Project>(e =>
        {
            e.ToTable("Projects");
            e.HasKey(x => x.ProjectId);
            e.Property(x => x.ProjectCode).HasMaxLength(50).IsRequired();
            e.Property(x => x.ProjectName).HasMaxLength(200).IsRequired();
            e.Property(x => x.ClientName).HasMaxLength(200).IsRequired();
        });

        // ── ResourceAllocations ────────────────────────────────────────────────
        m.Entity<ResourceAllocation>(e =>
        {
            e.ToTable("ResourceAllocations");
            e.HasKey(x => x.ResourceAllocationId);
            e.Property(x => x.BillingType).HasConversion<string>().HasMaxLength(50).IsRequired();
            e.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── DailyUpdates ───────────────────────────────────────────────────────
        m.Entity<DailyUpdate>(e =>
        {
            e.ToTable("DailyUpdates");
            e.HasKey(x => x.DailyUpdateId);
            e.Property(x => x.TicketNumber).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.WorkDate);
            e.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Tasks ──────────────────────────────────────────────────────────────
        m.Entity<TaskItem>(e =>
        {
            e.ToTable("Tasks");
            e.HasKey(x => x.TaskId);
            e.Property(x => x.Title).HasMaxLength(250).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            e.Property(x => x.TaskType).HasConversion<string>().HasMaxLength(50).IsRequired();
            e.Property(x => x.State).HasConversion<string>().HasMaxLength(50).IsRequired();
            e.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId)
             .IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        });

        // ── TaskResponses ──────────────────────────────────────────────────────
        m.Entity<TaskResponse>(e =>
        {
            e.ToTable("TaskResponses");
            e.HasKey(x => x.TaskResponseId);
            e.Property(x => x.Option).HasMaxLength(100).IsRequired();
            e.Property(x => x.Remark).HasMaxLength(1000);
            e.HasIndex(x => x.TaskId);
            e.HasOne<TaskItem>().WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Achievements ───────────────────────────────────────────────────────
        m.Entity<Achievement>(e =>
        {
            e.ToTable("Achievements");
            e.HasKey(x => x.AchievementId);
            e.Property(x => x.Category).HasMaxLength(100).IsRequired();
            e.Property(x => x.Title).HasMaxLength(250).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            e.Property(x => x.ProofWorkDriveUrl).HasMaxLength(1000);
            e.Property(x => x.ValidationStatus).HasConversion<string>().HasMaxLength(50).IsRequired();
            e.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── SalesEnquiries ─────────────────────────────────────────────────────
        m.Entity<SalesEnquiry>(e =>
        {
            e.ToTable("SalesEnquiries");
            e.HasKey(x => x.SalesEnquiryId);
            e.Property(x => x.ClientName).HasMaxLength(250).IsRequired();
            e.Property(x => x.Requirement).HasMaxLength(2000).IsRequired();
            e.Property(x => x.Technology).HasMaxLength(250).IsRequired();
            e.Property(x => x.SalesCoordinator).HasMaxLength(200).IsRequired();
            e.Property(x => x.Status).HasMaxLength(100).IsRequired();
            e.Property(x => x.ValidationStatus).HasConversion<string>().HasMaxLength(50).IsRequired();
            e.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Engagements ────────────────────────────────────────────────────────
        m.Entity<Engagement>(e =>
        {
            e.ToTable("Engagements");
            e.HasKey(x => x.EngagementId);
            e.Property(x => x.ClientName).HasMaxLength(250).IsRequired();
            e.Property(x => x.ProjectName).HasMaxLength(250).IsRequired();
            e.Property(x => x.Details).HasMaxLength(2000).IsRequired();
            e.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── SalesSessions ──────────────────────────────────────────────────────
        m.Entity<SalesSession>(e =>
        {
            e.ToTable("SalesSessions");
            e.HasKey(x => x.SalesSessionId);
            e.Property(x => x.Title).HasMaxLength(250).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            e.HasOne<Team>().WithMany().HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Events ─────────────────────────────────────────────────────────────
        m.Entity<EventItem>(e =>
        {
            e.ToTable("Events");
            e.HasKey(x => x.EventId);
            e.Property(x => x.Title).HasMaxLength(250).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            e.Property(x => x.Location).HasMaxLength(250).IsRequired();
            e.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── EventMedia ─────────────────────────────────────────────────────────
        m.Entity<EventMedia>(e =>
        {
            e.ToTable("EventMedia");
            e.HasKey(x => x.EventMediaId);
            e.Property(x => x.WorkDriveFileUrl).HasMaxLength(1000).IsRequired();
            e.HasIndex(x => x.EventId);
            e.HasOne<EventItem>().WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Meetings ───────────────────────────────────────────────────────────
        m.Entity<MeetingRecord>(e =>
        {
            e.ToTable("Meetings");
            e.HasKey(x => x.MeetingId);
            e.Property(x => x.Title).HasMaxLength(250).IsRequired();
            e.Property(x => x.ZohoMeetingUrl).HasMaxLength(1000).IsRequired();
            e.Property(x => x.TranscriptUrl).HasMaxLength(1000);
            e.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── MomEntries ─────────────────────────────────────────────────────────
        m.Entity<MomEntry>(e =>
        {
            e.ToTable("MomEntries");
            e.HasKey(x => x.MomEntryId);
            e.HasIndex(x => x.MeetingId);
            e.HasOne<MeetingRecord>().WithMany().HasForeignKey(x => x.MeetingId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Validations ────────────────────────────────────────────────────────
        m.Entity<ValidationRecord>(e =>
        {
            e.ToTable("Validations");
            e.HasKey(x => x.ValidationRecordId);
            e.Property(x => x.EntityType).HasConversion<string>().HasMaxLength(50).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            e.Property(x => x.Remarks).HasMaxLength(1000);
            e.HasIndex(x => x.Status);
        });

        // ── PointsLogs ─────────────────────────────────────────────────────────
        m.Entity<PointsLog>(e =>
        {
            e.ToTable("PointsLogs");
            e.HasKey(x => x.PointsLogId);
            e.Property(x => x.ActivityType).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.UserId);
            e.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Badges ─────────────────────────────────────────────────────────────
        m.Entity<Badge>(e =>
        {
            e.ToTable("Badges");
            e.HasKey(x => x.BadgeId);
            e.Property(x => x.BadgeName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Criteria).HasMaxLength(500).IsRequired();
        });

        // ── UserBadges ─────────────────────────────────────────────────────────
        m.Entity<UserBadge>(e =>
        {
            e.ToTable("UserBadges");
            e.HasKey(x => x.UserBadgeId);
            e.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Badge>().WithMany().HasForeignKey(x => x.BadgeId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Reports ────────────────────────────────────────────────────────────
        m.Entity<ReportRecord>(e =>
        {
            e.ToTable("Reports");
            e.HasKey(x => x.ReportRecordId);
            e.Property(x => x.ReportType).HasConversion<string>().HasMaxLength(50).IsRequired();
            e.Property(x => x.PayloadJson).HasColumnType("nvarchar(max)").IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.ReportType, x.StartDate, x.EndDate });
            e.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.GeneratedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── AuditEntries ───────────────────────────────────────────────────────
        m.Entity<AuditEntry>(e =>
        {
            e.ToTable("AuditEntries");
            e.HasKey(x => x.AuditEntryId);
            e.Property(x => x.Action).HasMaxLength(150).IsRequired();
            e.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
            e.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)").IsRequired();
            e.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── ReminderDispatches ─────────────────────────────────────────────────
        m.Entity<ReminderDispatch>(e =>
        {
            e.ToTable("ReminderDispatches");
            e.HasKey(x => x.ReminderDispatchId);
            e.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── ActionItems ────────────────────────────────────────────────────────
        m.Entity<ActionItem>(e =>
        {
            e.ToTable("ActionItems");
            e.HasKey(x => x.ActionItemId);
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            e.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(x => x.SourceType).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.AssignedToUserId);
            e.HasIndex(x => x.Status);
            e.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.AssignedToUserId)
             .OnDelete(DeleteBehavior.NoAction);
            e.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.CreatedByUserId)
             .OnDelete(DeleteBehavior.NoAction);
        });

        // ── InboxTasks ─────────────────────────────────────────────────────────
        m.Entity<InboxTask>(e =>
        {
            e.ToTable("InboxTasks");
            e.HasKey(x => x.InboxTaskId);
            e.Property(x => x.SourceChannel).HasMaxLength(20).IsRequired();
            e.Property(x => x.SourceSender).HasMaxLength(200).IsRequired();
            e.Property(x => x.SourceMessageId).HasMaxLength(500).IsRequired();
            e.Property(x => x.SourcePreview).HasMaxLength(1000).IsRequired();
            e.Property(x => x.ExtractedTaskText).HasMaxLength(2000).IsRequired();
            e.Property(x => x.DeduplicationHash).HasMaxLength(64).IsRequired();
            e.Property(x => x.Category).HasConversion<string>().HasMaxLength(50).IsRequired();
            e.Property(x => x.CustomCategoryName).HasMaxLength(100);
            e.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.State).HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(x => x.WeeklyReportCategory).HasConversion<string>().HasMaxLength(30);
            e.HasIndex(x => x.DeduplicationHash);
            e.HasIndex(x => new { x.UserId, x.State });
            e.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<InboxTaskDependency>(e =>
        {
            e.ToTable("InboxTaskDependencies");
            e.HasKey(x => x.InboxTaskDependencyId);
            e.HasIndex(x => x.InboxTaskId);
            e.HasOne<InboxTask>().WithMany().HasForeignKey(x => x.InboxTaskId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.DependentUserId).OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<InboxTaskReminder>(e =>
        {
            e.ToTable("InboxTaskReminders");
            e.HasKey(x => x.InboxTaskReminderId);
            e.Property(x => x.Channel).HasMaxLength(20).IsRequired();
            e.HasIndex(x => new { x.InboxTaskId, x.IsSent });
            e.HasOne<InboxTask>().WithMany().HasForeignKey(x => x.InboxTaskId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
