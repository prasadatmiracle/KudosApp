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
[Route("api/daily-updates")]
[Authorize]
public sealed class DailyUpdatesController(
    AppDbContext db,
    IVisibilityService visibility,
    IAuditService auditService,
    IPointsService pointsService) : ControllerBase
{
    [HttpPost]
    public ActionResult<DailyUpdate> Create(DailyUpdateInput input)
    {
        var userId = User.CurrentUserId();
        if (input.WorkDate == default) return BadRequest("WorkDate is required.");
        if (input.ProjectId <= 0) return BadRequest("ProjectId is required.");
        if (input.Status != DailyStatus.NoTask && string.IsNullOrWhiteSpace(input.TicketNumber))
            return BadRequest("TicketNumber is required for non no-task updates.");

        var duplicate = db.DailyUpdates.Any(x =>
            x.UserId == userId &&
            x.WorkDate == input.WorkDate &&
            x.TicketNumber == input.TicketNumber);

        if (duplicate && input.Status != DailyStatus.NoTask)
            return Conflict("Duplicate ticket/day update for the same user.");

        var created = new DailyUpdate
        {
            UserId = userId,
            ProjectId = input.ProjectId,
            WorkDate = input.WorkDate,
            TicketNumber = input.Status == DailyStatus.NoTask ? "NO-TASK" : input.TicketNumber.Trim(),
            Description = input.Description.Trim(),
            Status = input.Status,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.DailyUpdates.Add(created);
        db.SaveChanges();

        pointsService.AddPoints(userId, 5, "DailyUpdate", created.DailyUpdateId);
        auditService.Write(userId, "CREATE_DAILY_UPDATE", nameof(DailyUpdate), created.DailyUpdateId, JsonSerializer.Serialize(created));
        return Ok(created);
    }

    [HttpGet("my")]
    public ActionResult<IReadOnlyCollection<DailyUpdate>> My([FromQuery] DateOnly? date = null)
    {
        var userId = User.CurrentUserId();
        var updates = db.DailyUpdates
            .Where(x => x.UserId == userId && (!date.HasValue || x.WorkDate == date.Value))
            .OrderByDescending(x => x.WorkDate)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToList();
        return Ok(updates);
    }

    [HttpGet("team")]
    [Authorize(Roles = "Manager,Admin")]
    public ActionResult<IReadOnlyCollection<DailyUpdate>> Team([FromQuery] DateOnly startDate, [FromQuery] DateOnly endDate)
    {
        var userId = User.CurrentUserId();
        var role = User.CurrentRole();
        var visible = visibility.TeamViewableUserIds(userId);

        var updates = db.DailyUpdates
            .Where(x => x.WorkDate >= startDate && x.WorkDate <= endDate &&
                        (role == AppRole.Admin || visible.Contains(x.UserId)))
            .OrderByDescending(x => x.WorkDate)
            .ToList();
        return Ok(updates);
    }

    [HttpGet("compliance-heatmap")]
    [Authorize(Roles = "Manager,Admin")]
    public ActionResult<object> ComplianceHeatmap([FromQuery] DateOnly date)
    {
        var userId = User.CurrentUserId();
        var role = User.CurrentRole();
        var visible = visibility.TeamViewableUserIds(userId);

        var scopedUsers = db.Users.Where(x => role == AppRole.Admin || visible.Contains(x.UserId)).ToList();
        var submittedUserIds = db.DailyUpdates.Where(x => x.WorkDate == date).Select(x => x.UserId).ToHashSet();
        var rows = scopedUsers.Select(x => new { x.UserId, x.Name, Submitted = submittedUserIds.Contains(x.UserId) });
        return Ok(rows);
    }
}
