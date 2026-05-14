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
[Route("api/events")]
[Authorize]
public sealed class EventsController(AppDbContext db, IAuditService auditService) : ControllerBase
{
    private const int MaxMediaPerEvent = 10;

    [HttpPost]
    public ActionResult<EventItem> Create(CreateEventInput input)
    {
        var userId = User.CurrentUserId();
        var created = new EventItem
        {
            Title = input.Title.Trim(),
            Description = input.Description.Trim(),
            EventDate = input.EventDate,
            Location = input.Location.Trim(),
            CreatedByUserId = userId
        };
        db.Events.Add(created);
        db.SaveChanges();

        auditService.Write(userId, "CREATE_EVENT", nameof(EventItem), created.EventId, JsonSerializer.Serialize(created));
        return Ok(created);
    }

    [HttpPost("{eventId:int}/media")]
    public ActionResult<EventMedia> AddMedia(int eventId, AddEventMediaInput input)
    {
        if (string.IsNullOrWhiteSpace(input.WorkDriveFileUrl)) return BadRequest("WorkDriveFileUrl is required.");

        var userId = User.CurrentUserId();
        if (!db.Events.Any(x => x.EventId == eventId)) return NotFound("Event not found.");

        var count = db.EventMedia.Count(x => x.EventId == eventId);
        if (count >= MaxMediaPerEvent) return Conflict("Media limit reached (10 files per event).");

        var created = new EventMedia
        {
            EventId = eventId,
            WorkDriveFileUrl = input.WorkDriveFileUrl.Trim(),
            UploadedByUserId = userId,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.EventMedia.Add(created);
        db.SaveChanges();

        auditService.Write(userId, "ADD_EVENT_MEDIA", nameof(EventItem), eventId, JsonSerializer.Serialize(created));
        return Ok(created);
    }

    [HttpGet("feed")]
    public ActionResult<IReadOnlyCollection<object>> Feed([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var events = db.Events
            .OrderByDescending(x => x.EventDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var eventIds = events.Select(x => x.EventId).ToHashSet();
        var mediaByEvent = db.EventMedia
            .Where(x => eventIds.Contains(x.EventId))
            .GroupBy(x => x.EventId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.WorkDriveFileUrl).ToList());

        var rows = events.Select(evt => new
        {
            evt.EventId, evt.Title, evt.Description, evt.EventDate, evt.Location,
            Media = mediaByEvent.TryGetValue(evt.EventId, out var urls) ? urls : new List<string>()
        });
        return Ok(rows);
    }
}
