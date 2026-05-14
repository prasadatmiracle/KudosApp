using System.Text.Json;
using KudosApp.Api.Data;
using KudosApp.Api.DTOs;
using KudosApp.Api.Infrastructure;
using KudosApp.Api.Models;
using KudosApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KudosApp.Api.Controllers;

[ApiController]
[Route("api/action-items")]
[Authorize]
public sealed class ActionItemsController(
    AppDbContext db,
    IAuditService auditService) : ControllerBase
{
    // Manager/Admin create items and assign them to team members
    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    public ActionResult<ActionItem> Create(CreateActionItemInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Title)) return BadRequest("Title is required.");
        if (input.AssignedToUserId <= 0) return BadRequest("AssignedToUserId is required.");
        if (!db.Users.Any(x => x.UserId == input.AssignedToUserId)) return BadRequest("Assigned user not found.");

        var userId = User.CurrentUserId();
        var item = new ActionItem
        {
            Title = input.Title.Trim(),
            Description = input.Description.Trim(),
            AssignedToUserId = input.AssignedToUserId,
            CreatedByUserId = userId,
            DueDate = input.DueDate,
            Priority = input.Priority,
            Status = ActionItemStatus.Open,
            SourceType = input.SourceType,
            SourceId = input.SourceId
        };
        db.ActionItems.Add(item);
        db.SaveChanges();

        auditService.Write(userId, "CREATE_ACTION_ITEM", nameof(ActionItem), item.ActionItemId, JsonSerializer.Serialize(item));
        return Ok(item);
    }

    // Assignee or manager can view their own items
    [HttpGet("my")]
    public ActionResult<IReadOnlyCollection<ActionItemRow>> My([FromQuery] ActionItemStatus? status = null)
    {
        var userId = User.CurrentUserId();
        return Ok(QueryItems(assignedTo: userId, status: status));
    }

    // Manager/Admin see their whole team's items
    [HttpGet("team")]
    [Authorize(Roles = "Manager,Admin")]
    public ActionResult<IReadOnlyCollection<ActionItemRow>> Team(
        [FromQuery] int? assignedToUserId = null,
        [FromQuery] ActionItemStatus? status = null)
    {
        return Ok(QueryItems(assignedTo: assignedToUserId, status: status));
    }

    [HttpGet("{id:int}")]
    public ActionResult<ActionItemRow> Get(int id)
    {
        var userId = User.CurrentUserId();
        var role = User.CurrentRole();

        var rows = QueryItems(singleId: id);
        if (rows.Count == 0) return NotFound();

        var row = rows[0];
        // Employees can only see their own items
        if (role == AppRole.Employee && row.AssignedToUserId != userId && row.CreatedByUserId != userId)
            return Forbid();

        return Ok(row);
    }

    [HttpPut("{id:int}/status")]
    public ActionResult<ActionItem> UpdateStatus(int id, UpdateActionItemStatusInput input)
    {
        var userId = User.CurrentUserId();
        var role = User.CurrentRole();

        var item = db.ActionItems.SingleOrDefault(x => x.ActionItemId == id);
        if (item is null) return NotFound();

        // Employees can only update items assigned to them
        if (role == AppRole.Employee && item.AssignedToUserId != userId) return Forbid();

        if (item.Status == ActionItemStatus.Cancelled) return Conflict("Cannot update a cancelled item.");

        item.Status = input.Status;
        item.UpdatedAtUtc = DateTime.UtcNow;
        if (input.Status == ActionItemStatus.Completed)
            item.CompletedAtUtc = DateTime.UtcNow;

        db.SaveChanges();
        auditService.Write(userId, "UPDATE_ACTION_ITEM_STATUS", nameof(ActionItem), id, JsonSerializer.Serialize(new { input.Status }));
        return Ok(item);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Manager,Admin")]
    public IActionResult Cancel(int id)
    {
        var userId = User.CurrentUserId();
        var item = db.ActionItems.SingleOrDefault(x => x.ActionItemId == id);
        if (item is null) return NotFound();
        if (item.Status == ActionItemStatus.Completed) return Conflict("Cannot cancel a completed item.");

        item.Status = ActionItemStatus.Cancelled;
        item.UpdatedAtUtc = DateTime.UtcNow;
        db.SaveChanges();

        auditService.Write(userId, "CANCEL_ACTION_ITEM", nameof(ActionItem), id, "{}");
        return Ok();
    }

    // Manual trigger so manager can fire reminders on demand (outside Mon/Wed schedule)
    [HttpPost("send-reminders")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> SendReminders(
        [FromServices] IActionItemService actionItemService,
        [FromQuery] string type = "assignee",
        CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (type == "escalation")
            await actionItemService.SendManagerEscalationsAsync(today, ct);
        else
            await actionItemService.SendAssigneeRemindersAsync(today, ct);

        return Ok(new { triggered = type });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private List<ActionItemRow> QueryItems(
        int? assignedTo = null,
        ActionItemStatus? status = null,
        int? singleId = null)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var query = db.ActionItems.AsQueryable();
        if (singleId.HasValue) query = query.Where(x => x.ActionItemId == singleId.Value);
        if (assignedTo.HasValue) query = query.Where(x => x.AssignedToUserId == assignedTo.Value);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);

        var items = query.OrderBy(x => x.DueDate).ThenBy(x => x.Priority).ToList();

        var userIds = items.SelectMany(x => new[] { x.AssignedToUserId, x.CreatedByUserId }).Distinct().ToList();
        var names = db.Users.Where(u => userIds.Contains(u.UserId)).ToDictionary(u => u.UserId, u => u.Name);

        return items.Select(x => new ActionItemRow
        {
            ActionItemId = x.ActionItemId,
            Title = x.Title,
            Description = x.Description,
            AssignedToUserId = x.AssignedToUserId,
            AssigneeName = names.GetValueOrDefault(x.AssignedToUserId, "Unknown"),
            CreatedByUserId = x.CreatedByUserId,
            CreatedByName = names.GetValueOrDefault(x.CreatedByUserId, "Unknown"),
            DueDate = x.DueDate,
            Priority = x.Priority,
            Status = x.Status,
            SourceType = x.SourceType,
            SourceId = x.SourceId,
            CreatedAtUtc = x.CreatedAtUtc,
            CompletedAtUtc = x.CompletedAtUtc,
            IsOverdue = x.Status != ActionItemStatus.Completed
                        && x.Status != ActionItemStatus.Cancelled
                        && x.DueDate < today
        }).ToList();
    }
}
