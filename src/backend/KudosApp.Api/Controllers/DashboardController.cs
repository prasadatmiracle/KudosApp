using KudosApp.Api.Data;
using KudosApp.Api.Infrastructure;
using KudosApp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KudosApp.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public ActionResult<object> Get([FromQuery] DateOnly? date = null)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var userId = User.CurrentUserId();
        var now = DateTime.UtcNow;

        var pendingTasks = db.Tasks
            .Where(t => t.State == TaskState.Active)
            .Count(t => !db.TaskResponses.Any(r => r.TaskId == t.TaskId && r.UserId == userId));

        var hasTodayUpdate = db.DailyUpdates.Any(x => x.UserId == userId && x.WorkDate == targetDate);

        var currentMonthPoints = db.Points
            .Where(x => x.UserId == userId && x.CreatedAtUtc.Year == now.Year && x.CreatedAtUtc.Month == now.Month)
            .Sum(x => x.Points);

        var leaderboard = db.Points
            .Where(x => x.CreatedAtUtc.Year == now.Year && x.CreatedAtUtc.Month == now.Month)
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, Points = g.Sum(p => p.Points) })
            .OrderByDescending(x => x.Points)
            .ToList();

        var rank = leaderboard.FindIndex(x => x.UserId == userId) + 1;

        return Ok(new
        {
            pendingTasks,
            hasTodayUpdate,
            currentMonthPoints,
            rank = rank <= 0 ? (int?)null : rank
        });
    }

    // P10: Rich team health data for manager dashboard
    [HttpGet("team-health")]
    [Authorize(Roles = "Manager,Admin")]
    public ActionResult<object> TeamHealth()
    {
        var userId = User.CurrentUserId();
        var role = User.CurrentRole();
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        var visibleIds = db.Users
            .Where(x => x.IsActive && (role == AppRole.Admin || x.ManagerId == userId))
            .Select(x => x.UserId)
            .ToList();

        var totalTeam = visibleIds.Count;

        // Participation today
        var submittedToday = db.DailyUpdates
            .Where(x => x.WorkDate == today && visibleIds.Contains(x.UserId))
            .Select(x => x.UserId).Distinct().ToHashSet();

        var missingNames = db.Users
            .Where(x => visibleIds.Contains(x.UserId) && !submittedToday.Contains(x.UserId))
            .Select(x => x.Name).ToList();

        // Blocked tickets today
        var blockedTickets = db.DailyUpdates
            .Where(x => x.WorkDate == today && x.Status == DailyStatus.Blocked && visibleIds.Contains(x.UserId))
            .Select(x => new { x.TicketNumber, x.Description })
            .ToList();

        // Pending validations
        var pendingAchievements = db.Achievements
            .Count(x => x.ValidationStatus == ValidationStatus.Pending && visibleIds.Contains(x.UserId));
        var pendingSales = db.SalesEnquiries
            .Count(x => x.ValidationStatus == ValidationStatus.Pending && visibleIds.Contains(x.CreatedByUserId));

        // Open action items
        var openActionItems = db.ActionItems
            .Count(x => (x.Status == ActionItemStatus.Open || x.Status == ActionItemStatus.InProgress)
                        && visibleIds.Contains(x.AssignedToUserId));

        var overdueActionItems = db.ActionItems
            .Count(x => x.Status != ActionItemStatus.Completed
                        && x.Status != ActionItemStatus.Cancelled
                        && x.DueDate < today
                        && visibleIds.Contains(x.AssignedToUserId));

        // Billing type breakdown
        var billingBreakdown = db.ResourceAllocations
            .Where(x => x.IsActive && visibleIds.Contains(x.UserId))
            .GroupBy(x => x.BillingType)
            .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
            .ToList();

        // Weekly participation trend (last 5 workdays)
        var weekStart = today.AddDays(-6);
        var weeklyTrend = db.DailyUpdates
            .Where(x => x.WorkDate >= weekStart && x.WorkDate <= today && visibleIds.Contains(x.UserId))
            .GroupBy(x => x.WorkDate)
            .Select(g => new { Date = g.Key, Count = g.Select(x => x.UserId).Distinct().Count() })
            .OrderBy(x => x.Date)
            .ToList();

        // Engagement score: participation(40%) + task response rate(30%) + validation throughput(30%)
        var participationScore = totalTeam == 0 ? 0 : (submittedToday.Count * 100 / totalTeam);
        var activeTasks = db.Tasks.Count(t => t.State == TaskState.Active);
        var taskResponseRate = activeTasks == 0 ? 100
            : db.TaskResponses
                .Where(r => visibleIds.Contains(r.UserId))
                .Select(r => r.UserId).Distinct().Count() * 100 / Math.Max(totalTeam, 1);
        var validationScore = pendingAchievements + pendingSales == 0 ? 100
            : Math.Max(0, 100 - ((pendingAchievements + pendingSales) * 10));

        var engagementScore = (int)(participationScore * 0.4 + taskResponseRate * 0.3 + validationScore * 0.3);

        return Ok(new
        {
            totalTeam,
            submittedToday = submittedToday.Count,
            participationPct = participationScore,
            missingNames,
            blockedTickets,
            pendingAchievements,
            pendingSales,
            openActionItems,
            overdueActionItems,
            billingBreakdown,
            weeklyTrend = weeklyTrend.Select(x => new { date = x.Date.ToString("yyyy-MM-dd"), x.Count }),
            engagementScore
        });
    }
}
