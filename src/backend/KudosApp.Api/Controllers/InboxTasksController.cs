using KudosApp.Api.Data;
using KudosApp.Api.Infrastructure;
using KudosApp.Api.Models;
using KudosApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KudosApp.Api.Controllers;

// ── Input DTOs ────────────────────────────────────────────────────────────────

public sealed record IngestMailInput(string Sender, string MessageId, string Subject, string Body);
public sealed record IngestCliqInput(string SenderId, string MessageId, string Text);
public sealed record ConfirmTaskInput(
    InboxTaskCategory Category,
    InboxTaskPriority Priority,
    DateTime? DueAtUtc,
    string? CustomCategoryName);
public sealed record UpdateStateInput(InboxTaskState State);
public sealed record CompleteTaskInput(
    bool IncludeInWeeklyReport,
    InboxTaskWeeklyCategory? WeeklyReportCategory);
public sealed record MakePublicInput(List<int> DependentUserIds);

// ── Controller ────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/inbox-tasks")]
[Authorize]
public sealed class InboxTasksController(
    AppDbContext db,
    IInboxTaskService inboxTaskService,
    IVisibilityService visibility) : ControllerBase
{
    // POST /api/inbox-tasks/ingest/mail — called by Zoho Mail webhook
    [HttpPost("ingest/mail")]
    [AllowAnonymous]   // webhook — secured by shared secret header in production
    public ActionResult IngestMail(IngestMailInput input)
    {
        var user = db.Users.SingleOrDefault(x => x.Email == input.Sender && x.IsActive);
        if (user is null)
        {
            // Try to find user from recipient side — route to manager if unknown sender
            var manager = db.Users.FirstOrDefault(x => x.Role == AppRole.Manager && x.IsActive);
            if (manager is null) return Ok(new { skipped = true, reason = "no_user_found" });
            user = manager;
        }

        var messageText = $"{input.Subject}\n\n{input.Body}";
        var task = inboxTaskService.Ingest(
            user.UserId, "ZohoMail", input.Sender, input.MessageId, messageText);

        return Ok(new { created = task is not null, inboxTaskId = task?.InboxTaskId });
    }

    // POST /api/inbox-tasks/ingest/cliq — called by Zoho Cliq event webhook
    [HttpPost("ingest/cliq")]
    [AllowAnonymous]
    public ActionResult IngestCliq(IngestCliqInput input)
    {
        var user = db.Users.FirstOrDefault(x => x.IsActive && x.Role != AppRole.Hr);
        if (user is null) return Ok(new { skipped = true });

        var task = inboxTaskService.Ingest(
            user.UserId, "ZohoCliq", input.SenderId, input.MessageId, input.Text);

        return Ok(new { created = task is not null, inboxTaskId = task?.InboxTaskId });
    }

    // GET /api/inbox-tasks/pending — tasks awaiting user confirmation
    [HttpGet("pending")]
    public ActionResult GetPending()
    {
        var userId = User.CurrentUserId();
        var tasks = db.InboxTasks
            .Where(x => x.UserId == userId && x.State == InboxTaskState.PendingConfirmation)
            .OrderBy(x => x.CreatedAtUtc)
            .ToList();
        return Ok(tasks);
    }

    // GET /api/inbox-tasks — active + in-progress tasks for current user
    [HttpGet]
    public ActionResult GetActive()
    {
        var userId = User.CurrentUserId();
        var tasks = db.InboxTasks
            .Where(x => x.UserId == userId
                        && (x.State == InboxTaskState.Active
                            || x.State == InboxTaskState.InProgress))
            .OrderBy(x => x.DueAtUtc ?? DateTime.MaxValue)
            .ThenBy(x => x.CreatedAtUtc)
            .ToList();
        return Ok(tasks);
    }

    // GET /api/inbox-tasks/completed — completed tasks (last 30 days)
    [HttpGet("completed")]
    public ActionResult GetCompleted()
    {
        var userId = User.CurrentUserId();
        var since = DateTime.UtcNow.AddDays(-30);
        var tasks = db.InboxTasks
            .Where(x => x.UserId == userId
                        && x.State == InboxTaskState.Completed
                        && x.CompletedAtUtc >= since)
            .OrderByDescending(x => x.CompletedAtUtc)
            .ToList();
        return Ok(tasks);
    }

    // POST /api/inbox-tasks/{id}/confirm
    [HttpPost("{id:int}/confirm")]
    public ActionResult Confirm(int id, ConfirmTaskInput input)
    {
        var task = GetOwnedTask(id);
        if (task is null) return NotFound();
        if (task.State != InboxTaskState.PendingConfirmation)
            return BadRequest("Task is not pending confirmation.");

        inboxTaskService.Confirm(task, input.Category, input.Priority,
            input.DueAtUtc, input.CustomCategoryName);
        return Ok(task);
    }

    // POST /api/inbox-tasks/{id}/dismiss
    [HttpPost("{id:int}/dismiss")]
    public ActionResult Dismiss(int id)
    {
        var task = GetOwnedTask(id);
        if (task is null) return NotFound();
        if (task.State is InboxTaskState.Completed or InboxTaskState.Dismissed)
            return BadRequest("Task is already finalized.");

        inboxTaskService.Dismiss(task);
        return Ok(task);
    }

    // PUT /api/inbox-tasks/{id}/state
    [HttpPut("{id:int}/state")]
    public ActionResult UpdateState(int id, UpdateStateInput input)
    {
        var task = GetOwnedTask(id);
        if (task is null) return NotFound();

        var allowed = new[] { InboxTaskState.Active, InboxTaskState.InProgress };
        if (!allowed.Contains(input.State))
            return BadRequest("Use /complete or /dismiss for terminal states.");

        inboxTaskService.UpdateState(task, input.State);
        return Ok(task);
    }

    // POST /api/inbox-tasks/{id}/complete
    [HttpPost("{id:int}/complete")]
    public ActionResult Complete(int id, CompleteTaskInput input)
    {
        var task = GetOwnedTask(id);
        if (task is null) return NotFound();
        if (task.State is InboxTaskState.Completed or InboxTaskState.Dismissed)
            return BadRequest("Task is already finalized.");

        inboxTaskService.Complete(task, input.IncludeInWeeklyReport, input.WeeklyReportCategory);
        return Ok(task);
    }

    // POST /api/inbox-tasks/{id}/make-public
    [HttpPost("{id:int}/make-public")]
    public ActionResult MakePublic(int id, MakePublicInput input)
    {
        var task = GetOwnedTask(id);
        if (task is null) return NotFound();

        inboxTaskService.MakePublic(task, input.DependentUserIds);
        return Ok(task);
    }

    // GET /api/inbox-tasks/team — manager view of all visible team tasks
    [HttpGet("team")]
    [Authorize(Roles = "Manager,Admin")]
    public ActionResult GetTeam(
        [FromQuery] InboxTaskState? state = null,
        [FromQuery] bool includePrivate = false)
    {
        var managerId = User.CurrentUserId();
        var visibleIds = visibility.TeamViewableUserIds(managerId).ToHashSet();

        var q = db.InboxTasks
            .Where(x => visibleIds.Contains(x.UserId));

        if (state.HasValue)
            q = q.Where(x => x.State == state.Value);
        else
            q = q.Where(x => x.State == InboxTaskState.Active
                           || x.State == InboxTaskState.InProgress
                           || x.State == InboxTaskState.PendingConfirmation);

        if (!includePrivate)
            q = q.Where(x => !x.IsPrivate);

        var tasks = q.OrderBy(x => x.Priority)
                     .ThenBy(x => x.DueAtUtc ?? DateTime.MaxValue)
                     .ToList();

        // Attach user names
        var userIds = tasks.Select(x => x.UserId).Distinct().ToHashSet();
        var users = db.Users.Where(x => userIds.Contains(x.UserId))
                            .ToDictionary(x => x.UserId, x => x.Name);

        return Ok(tasks.Select(t => new
        {
            task     = t,
            userName = users.GetValueOrDefault(t.UserId, "Unknown")
        }));
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private InboxTask? GetOwnedTask(int id)
    {
        var userId = User.CurrentUserId();
        return db.InboxTasks.SingleOrDefault(x => x.InboxTaskId == id && x.UserId == userId);
    }
}
