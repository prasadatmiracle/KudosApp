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
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController(
    AppDbContext db,
    IZohoBridge zohoBridge,
    IVisibilityService visibility,
    IReminderPolicy reminderPolicy,
    IAuditService auditService) : ControllerBase
{
    [HttpPost("send")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Send(NotificationInput input, CancellationToken ct)
    {
        if (input.UserIds.Count == 0 || string.IsNullOrWhiteSpace(input.Message))
            return BadRequest("UserIds and message are required.");

        var emails = db.Users
            .Where(x => input.UserIds.Contains(x.UserId))
            .Select(x => x.Email)
            .ToList();

        await zohoBridge.SendCliqNotificationAsync(input.Message, emails, ct);
        auditService.Write(User.CurrentUserId(), "SEND_NOTIFICATION", "Notification", emails.Count, JsonSerializer.Serialize(input));
        return Ok();
    }

    [HttpPost("remind-pending-daily")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> RemindPendingDaily([FromQuery] DateOnly date, CancellationToken ct)
    {
        var actorId = User.CurrentUserId();
        var role = User.CurrentRole();
        var visible = visibility.TeamViewableUserIds(actorId);

        var scopedUsers = db.Users
            .Where(x => role == AppRole.Admin || visible.Contains(x.UserId))
            .Select(x => new { x.UserId, x.Email })
            .ToList();

        var submitted = db.DailyUpdates
            .Where(x => x.WorkDate == date)
            .Select(x => x.UserId)
            .Distinct()
            .ToHashSet();

        var pendingUsers = scopedUsers.Where(x => !submitted.Contains(x.UserId)).ToList();
        var eligible = pendingUsers.Where(x => reminderPolicy.CanDispatchReminder(x.UserId, date)).ToList();

        if (eligible.Count == 0) return Ok(new { sent = 0 });

        var eligibleEmails = eligible.Select(x => x.Email).ToList();
        await zohoBridge.SendCliqNotificationAsync(
            $"Reminder: Please submit your daily update for {date:yyyy-MM-dd}.",
            eligibleEmails, ct);

        foreach (var user in eligible)
            reminderPolicy.MarkReminderSent(user.UserId, date);

        auditService.Write(actorId, "SEND_DAILY_REMINDER", "Notification", eligible.Count, $"{{\"date\":\"{date:yyyy-MM-dd}\"}}");
        return Ok(new { sent = eligible.Count });
    }
}
