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
        // SCR-1 C6 (Assessment A1): FocusDay and Continuing don't require a
        // ticket number. NoTask is kept for backward compatibility.
        var isNoTicketStatus = input.Status is DailyStatus.NoTask
                                              or DailyStatus.FocusDay
                                              or DailyStatus.Continuing;
        if (!isNoTicketStatus && string.IsNullOrWhiteSpace(input.TicketNumber))
            return BadRequest("TicketNumber is required when there is active ticket work.");

        var duplicate = db.DailyUpdates.Any(x =>
            x.UserId == userId &&
            x.WorkDate == input.WorkDate &&
            x.TicketNumber == input.TicketNumber);

        if (duplicate && !isNoTicketStatus)
            return Conflict("Duplicate ticket/day update for the same user.");

        // Synthetic ticket numbers so the column stays non-null and unique-per-day.
        string syntheticTicket = input.Status switch
        {
            DailyStatus.NoTask     => "NO-TASK",
            DailyStatus.FocusDay   => $"FOCUS-{input.WorkDate:yyyyMMdd}",
            DailyStatus.Continuing => $"CONTINUING-{input.WorkDate:yyyyMMdd}",
            _ => input.TicketNumber.Trim()
        };

        var created = new DailyUpdate
        {
            UserId = userId,
            ProjectId = input.ProjectId,
            WorkDate = input.WorkDate,
            TicketNumber = syntheticTicket,
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

    // P11: Range heatmap — returns per-user per-day submission matrix for calendar UI
    [HttpGet("compliance-heatmap/range")]
    [Authorize(Roles = "Manager,Admin")]
    public ActionResult<object> ComplianceHeatmapRange(
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate)
    {
        if (endDate < startDate) return BadRequest("endDate must be >= startDate.");
        if (endDate.DayNumber - startDate.DayNumber > 90) return BadRequest("Range cannot exceed 90 days.");

        var userId = User.CurrentUserId();
        var role = User.CurrentRole();
        var visible = visibility.TeamViewableUserIds(userId);

        var users = db.Users
            .Where(x => x.IsActive && (role == AppRole.Admin || visible.Contains(x.UserId)))
            .Select(x => new { x.UserId, x.Name })
            .OrderBy(x => x.Name)
            .ToList();

        var submissions = db.DailyUpdates
            .Where(x => x.WorkDate >= startDate && x.WorkDate <= endDate
                        && users.Select(u => u.UserId).Contains(x.UserId))
            .Select(x => new { x.UserId, x.WorkDate, x.Status })
            .ToList();

        // Build date list (weekdays only for display, but include all for accuracy)
        var dates = new List<DateOnly>();
        for (var d = startDate; d <= endDate; d = d.AddDays(1))
            dates.Add(d);

        var rows = users.Select(u =>
        {
            var userSubs = submissions.Where(s => s.UserId == u.UserId).ToList();
            var days = dates.Select(d => new
            {
                date = d.ToString("yyyy-MM-dd"),
                isWeekend = d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday,
                submitted = userSubs.Any(s => s.WorkDate == d),
                status = userSubs.FirstOrDefault(s => s.WorkDate == d)?.Status.ToString()
            }).ToList();

            return new { u.UserId, u.Name, days };
        });

        return Ok(new { dates = dates.Select(d => d.ToString("yyyy-MM-dd")), users = rows });
    }
}
