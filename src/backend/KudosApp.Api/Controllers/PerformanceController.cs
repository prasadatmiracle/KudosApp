using KudosApp.Api.Data;
using KudosApp.Api.Infrastructure;
using KudosApp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KudosApp.Api.Controllers;

[ApiController]
[Route("api/performance")]
[Authorize]
public sealed class PerformanceController(AppDbContext db) : ControllerBase
{
    [HttpGet("leaderboard")]
    public ActionResult<IReadOnlyCollection<object>> Leaderboard([FromQuery] int year, [FromQuery] int month)
    {
        var points = db.Points
            .Where(x => x.CreatedAtUtc.Year == year && x.CreatedAtUtc.Month == month)
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, Points = g.Sum(x => x.Points) })
            .ToList();

        var userIds = points.Select(x => x.UserId).ToHashSet();
        var names = db.Users.Where(u => userIds.Contains(u.UserId)).ToDictionary(u => u.UserId, u => u.Name);

        var rows = points
            .Select(x => new { x.UserId, Name = names.GetValueOrDefault(x.UserId, "Unknown"), x.Points })
            .OrderByDescending(x => x.Points)
            .ThenBy(x => x.Name)
            .ToList<object>();

        return Ok(rows);
    }

    [HttpGet("my")]
    public ActionResult<object> My()
    {
        var userId = User.CurrentUserId();
        var now = DateTime.UtcNow;

        var points = db.Points
            .Where(x => x.UserId == userId && x.CreatedAtUtc.Year == now.Year && x.CreatedAtUtc.Month == now.Month)
            .Sum(x => x.Points);

        var badgeIds = db.UserBadges.Where(x => x.UserId == userId).Select(x => x.BadgeId).ToList();
        var badges = db.Badges.Where(x => badgeIds.Contains(x.BadgeId)).Select(x => x.BadgeName).ToList();

        return Ok(new { points, badges });
    }

    [HttpPost("refresh-badges")]
    [Authorize(Roles = "Manager,Admin")]
    public IActionResult RefreshBadges()
    {
        var now = DateTime.UtcNow;
        var activeUsers = db.Users.Where(x => x.IsActive).Select(x => x.UserId).ToList();

        foreach (var userId in activeUsers)
        {
            var updatesCount = db.DailyUpdates.Count(x => x.UserId == userId && x.WorkDate.Year == now.Year && x.WorkDate.Month == now.Month);
            var voteCount = db.TaskResponses.Count(x => x.UserId == userId && x.CreatedAtUtc.Year == now.Year && x.CreatedAtUtc.Month == now.Month);
            var knowledgeCount = db.Achievements.Count(x => x.UserId == userId && x.ValidationStatus == ValidationStatus.Approved && x.CreatedAtUtc.Year == now.Year && x.CreatedAtUtc.Month == now.Month);

            TryAwardBadge(userId, "Consistent Contributor", updatesCount >= 20);
            TryAwardBadge(userId, "Team Player", voteCount >= 15);
            TryAwardBadge(userId, "Knowledge Sharer", knowledgeCount >= 4);
        }

        db.SaveChanges();
        return Ok();
    }

    private void TryAwardBadge(int userId, string badgeName, bool eligible)
    {
        if (!eligible) return;

        var badge = db.Badges.SingleOrDefault(x => x.BadgeName == badgeName);
        if (badge is null) return;

        var alreadyAwarded = db.UserBadges.Any(x => x.UserId == userId && x.BadgeId == badge.BadgeId);
        if (alreadyAwarded) return;

        db.UserBadges.Add(new UserBadge
        {
            UserId = userId,
            BadgeId = badge.BadgeId,
            AwardedAtUtc = DateTime.UtcNow
        });
    }
}
