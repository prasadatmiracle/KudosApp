using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using KudosApp.Api.Data;
using KudosApp.Api.Models;

namespace KudosApp.Api.Services;

public sealed record IngestResult(bool HasTask, string TaskText, string? Deadline);

public interface IInboxTaskService
{
    /// <summary>Parse a raw message and create an InboxTask if a task is detected.</summary>
    InboxTask? Ingest(int userId, string channel, string sender,
        string messageId, string messageText);

    void Confirm(InboxTask task, InboxTaskCategory category, InboxTaskPriority priority,
        DateTime? dueAtUtc, string? customCategoryName);

    void Dismiss(InboxTask task);
    void UpdateState(InboxTask task, InboxTaskState newState);
    void Complete(InboxTask task, bool includeInReport, InboxTaskWeeklyCategory? reportCategory);
    void MakePublic(InboxTask task, IReadOnlyList<int> dependentUserIds);
}

public sealed class InboxTaskService(AppDbContext db) : IInboxTaskService
{
    // ── Ingest ────────────────────────────────────────────────────────────────

    public InboxTask? Ingest(int userId, string channel, string sender,
        string messageId, string messageText)
    {
        var extracted = ExtractTask(messageText);
        if (!extracted.HasTask) return null;

        var hash = ComputeDeduplicationHash(sender, extracted.TaskText);

        // Reject duplicate within 24-hour window
        var cutoff = DateTime.UtcNow.AddHours(-24);
        if (db.InboxTasks.Any(x => x.DeduplicationHash == hash && x.CreatedAtUtc > cutoff))
            return null;

        var task = new InboxTask
        {
            UserId           = userId,
            SourceChannel    = channel,
            SourceSender     = sender,
            SourceMessageId  = messageId,
            SourcePreview    = messageText.Length > 1000 ? messageText[..1000] : messageText,
            ExtractedTaskText = extracted.TaskText,
            DeduplicationHash = hash,
            State            = InboxTaskState.PendingConfirmation,
            DueAtUtc         = ParseDeadline(extracted.Deadline),
            CreatedAtUtc     = DateTime.UtcNow
        };

        db.InboxTasks.Add(task);
        db.SaveChanges();
        return task;
    }

    // ── State mutations ───────────────────────────────────────────────────────

    public void Confirm(InboxTask task, InboxTaskCategory category, InboxTaskPriority priority,
        DateTime? dueAtUtc, string? customCategoryName)
    {
        task.State              = InboxTaskState.Active;
        task.Category           = category;
        task.Priority           = priority;
        task.CustomCategoryName = customCategoryName;
        if (dueAtUtc.HasValue) task.DueAtUtc = dueAtUtc;
        task.UpdatedAtUtc       = DateTime.UtcNow;
        db.SaveChanges();
    }

    public void Dismiss(InboxTask task)
    {
        task.State        = InboxTaskState.Dismissed;
        task.UpdatedAtUtc = DateTime.UtcNow;
        db.SaveChanges();
    }

    public void UpdateState(InboxTask task, InboxTaskState newState)
    {
        task.State        = newState;
        task.UpdatedAtUtc = DateTime.UtcNow;
        db.SaveChanges();
    }

    public void Complete(InboxTask task, bool includeInReport, InboxTaskWeeklyCategory? reportCategory)
    {
        task.State                  = InboxTaskState.Completed;
        task.CompletedAtUtc         = DateTime.UtcNow;
        task.UpdatedAtUtc           = DateTime.UtcNow;
        task.IncludeInWeeklyReport  = includeInReport;
        task.WeeklyReportCategory   = reportCategory;
        db.SaveChanges();
    }

    public void MakePublic(InboxTask task, IReadOnlyList<int> dependentUserIds)
    {
        task.IsPrivate    = false;
        task.UpdatedAtUtc = DateTime.UtcNow;

        foreach (var depId in dependentUserIds)
        {
            if (!db.InboxTaskDependencies.Any(x => x.InboxTaskId == task.InboxTaskId
                                                   && x.DependentUserId == depId))
            {
                db.InboxTaskDependencies.Add(new InboxTaskDependency
                {
                    InboxTaskId     = task.InboxTaskId,
                    DependentUserId = depId
                });
            }
        }

        db.SaveChanges();
    }

    // ── AI extraction stub ────────────────────────────────────────────────────
    // Replace with Azure OpenAI call once keys are configured (P14/P15 dependency).
    // Stub uses heuristics: looks for action verbs and question patterns.

    private static IngestResult ExtractTask(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length < 10)
            return new IngestResult(false, string.Empty, null);

        // Action verbs that indicate a task/request
        var taskIndicators = new[]
        {
            "please", "can you", "could you", "would you", "need you to", "need to",
            "action:", "todo:", "follow up", "follow-up", "by when", "deadline",
            "asap", "urgent", "review", "send me", "share", "update", "check",
            "prepare", "complete", "submit", "fix", "resolve", "schedule", "book"
        };

        var lower = text.ToLowerInvariant();
        bool hasTask = taskIndicators.Any(t => lower.Contains(t))
                    || text.TrimEnd().EndsWith('?');

        if (!hasTask) return new IngestResult(false, string.Empty, null);

        // Extract deadline hint
        string? deadline = null;
        var deadlinePatterns = new[]
        {
            @"\bby\s+(monday|tuesday|wednesday|thursday|friday|tomorrow|eod|end of day|today)\b",
            @"\bby\s+\d{1,2}[/-]\d{1,2}",
            @"\bdeadline[:\s]+(.{5,40})",
            @"\bdue[:\s]+(.{5,40})"
        };

        foreach (var pattern in deadlinePatterns)
        {
            var match = Regex.Match(lower, pattern);
            if (match.Success) { deadline = match.Value; break; }
        }

        // Trim task text to 500 chars
        var taskText = text.Length <= 500 ? text.Trim() : text[..500].Trim() + "…";
        return new IngestResult(true, taskText, deadline);
    }

    private static string ComputeDeduplicationHash(string sender, string taskText)
    {
        var normalised = Regex.Replace(taskText.ToLowerInvariant(), @"\s+", " ").Trim();
        var keywords = string.Join(" ", normalised.Split(' ')
            .Where(w => w.Length > 4)
            .OrderBy(w => w)
            .Take(8));
        var input = $"{sender.ToLowerInvariant()}|{keywords}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
    }

    private static DateTime? ParseDeadline(string? deadlineHint)
    {
        if (string.IsNullOrEmpty(deadlineHint)) return null;
        var lower = deadlineHint.ToLowerInvariant();
        var today = DateTime.UtcNow.Date;
        if (lower.Contains("today") || lower.Contains("eod")) return today.AddHours(17);
        if (lower.Contains("tomorrow")) return today.AddDays(1).AddHours(17);
        if (lower.Contains("monday"))   return NextWeekday(today, DayOfWeek.Monday).AddHours(17);
        if (lower.Contains("friday"))   return NextWeekday(today, DayOfWeek.Friday).AddHours(17);
        return null;
    }

    private static DateTime NextWeekday(DateTime from, DayOfWeek day)
    {
        int diff = ((int)day - (int)from.DayOfWeek + 7) % 7;
        return from.AddDays(diff == 0 ? 7 : diff);
    }
}
