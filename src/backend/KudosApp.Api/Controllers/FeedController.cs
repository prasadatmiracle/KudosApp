using KudosApp.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KudosApp.Api.Controllers;

[ApiController]
[Route("api/feed")]
[Authorize]
public sealed class FeedController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyCollection<object>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var achievements = db.Achievements
            .Select(x => new { Id = x.AchievementId, Kind = "Achievement", x.Title, x.Description, x.CreatedAtUtc })
            .ToList();

        var events = db.Events
            .Select(x => new { Id = x.EventId, Kind = "Event", x.Title, x.Description, x.CreatedAtUtc })
            .ToList();

        var rows = achievements
            .Select(x => new { x.Id, x.Kind, x.Title, x.Description, x.CreatedAtUtc })
            .Concat(events.Select(x => new { x.Id, x.Kind, x.Title, x.Description, x.CreatedAtUtc }))
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList<object>();

        return Ok(rows);
    }
}
