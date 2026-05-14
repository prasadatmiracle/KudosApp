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
[Route("api/meetings")]
[Authorize]
public sealed class MeetingsController(
    AppDbContext db,
    IZohoBridge zohoBridge,
    IAuditService auditService) : ControllerBase
{
    [HttpPost]
    public ActionResult<MeetingRecord> Create(CreateMeetingInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Title)) return BadRequest("Title is required.");

        var userId = User.CurrentUserId();
        var created = new MeetingRecord
        {
            Title = input.Title.Trim(),
            MeetingAtUtc = input.MeetingAtUtc,
            ZohoMeetingUrl = input.ZohoMeetingUrl.Trim(),
            TranscriptUrl = input.TranscriptUrl?.Trim(),
            CreatedByUserId = userId
        };
        db.Meetings.Add(created);
        db.SaveChanges();

        auditService.Write(userId, "CREATE_MEETING", nameof(MeetingRecord), created.MeetingId, JsonSerializer.Serialize(created));
        return Ok(created);
    }

    [HttpPost("{meetingId:int}/mom")]
    public ActionResult<MomEntry> UploadMom(int meetingId, UploadMomInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Summary)) return BadRequest("Summary is required.");

        var userId = User.CurrentUserId();
        if (!db.Meetings.Any(x => x.MeetingId == meetingId)) return NotFound("Meeting not found.");

        var mom = new MomEntry
        {
            MeetingId = meetingId,
            Summary = input.Summary.Trim(),
            ActionItems = input.ActionItems.Trim(),
            CreatedByUserId = userId
        };
        db.MomEntries.Add(mom);
        db.SaveChanges();

        auditService.Write(userId, "UPLOAD_MOM", nameof(MeetingRecord), meetingId, JsonSerializer.Serialize(mom));
        return Ok(mom);
    }

    [HttpPost("{meetingId:int}/transcript-ingest")]
    public async Task<ActionResult<MomEntry>> IngestTranscript(int meetingId, TranscriptIngestInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.TranscriptText)) return BadRequest("TranscriptText is required.");

        var userId = User.CurrentUserId();
        if (!db.Meetings.Any(x => x.MeetingId == meetingId)) return NotFound("Meeting not found.");

        var extracted = await zohoBridge.IngestMeetingTranscriptAsync(input.TranscriptText, ct);
        var mom = new MomEntry
        {
            MeetingId = meetingId,
            Summary = extracted.summary,
            ActionItems = extracted.actionItems,
            CreatedByUserId = userId
        };
        db.MomEntries.Add(mom);
        db.SaveChanges();

        auditService.Write(userId, "INGEST_MOM_TRANSCRIPT", nameof(MeetingRecord), meetingId, JsonSerializer.Serialize(mom));
        return Ok(mom);
    }
}
