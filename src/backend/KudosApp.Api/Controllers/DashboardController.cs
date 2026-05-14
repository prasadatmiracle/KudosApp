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
}
