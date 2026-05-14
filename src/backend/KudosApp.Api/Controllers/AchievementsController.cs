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
[Route("api/achievements")]
[Authorize]
public sealed class AchievementsController(
    AppDbContext db,
    IAuditService auditService,
    IPointsService pointsService,
    IZohoBridge zohoBridge) : ControllerBase
{
    [HttpPost]
    public ActionResult<Achievement> Create(CreateAchievementInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Title) || string.IsNullOrWhiteSpace(input.Category))
            return BadRequest("Category and title are required.");

        var userId = User.CurrentUserId();
        var created = new Achievement
        {
            UserId = userId,
            Category = input.Category.Trim(),
            Title = input.Title.Trim(),
            Description = input.Description.Trim(),
            ProofWorkDriveUrl = input.ProofWorkDriveUrl,
            ValidationStatus = ValidationStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Achievements.Add(created);
        db.SaveChanges();

        db.Validations.Add(new ValidationRecord
        {
            EntityType = ValidationEntityType.Achievement,
            EntityId = created.AchievementId,
            Status = ValidationStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        });
        db.SaveChanges();

        pointsService.AddPoints(userId, 10, "Achievement", created.AchievementId);
        auditService.Write(userId, "CREATE_ACHIEVEMENT", nameof(Achievement), created.AchievementId, JsonSerializer.Serialize(created));
        return Ok(created);
    }

    // P8: Upload proof document to WorkDrive and attach to achievement
    [HttpPost("{achievementId:int}/proof/upload")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<IActionResult> UploadProof(int achievementId, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("No file provided.");

        var userId = User.CurrentUserId();
        var achievement = db.Achievements.SingleOrDefault(x => x.AchievementId == achievementId);
        if (achievement is null) return NotFound();
        if (achievement.UserId != userId && User.CurrentRole() != AppRole.Admin) return Forbid();

        await using var stream = file.OpenReadStream();
        var fileUrl = await zohoBridge.UploadToWorkDriveAsync(file.FileName, file.ContentType, stream, ct);

        if (fileUrl is null)
            return StatusCode(502, "WorkDrive upload failed or is not configured.");

        achievement.ProofWorkDriveUrl = fileUrl;
        db.SaveChanges();

        auditService.Write(userId, "UPLOAD_ACHIEVEMENT_PROOF", nameof(Achievement), achievementId,
            JsonSerializer.Serialize(new { file.FileName, fileUrl }));
        return Ok(new { fileUrl });
    }

    [HttpGet("feed")]
    public ActionResult<IReadOnlyCollection<object>> Feed([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var rows = db.Achievements
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Join(db.Users, a => a.UserId, u => u.UserId, (a, u) => new
            {
                a.AchievementId, a.Category, a.Title, a.Description,
                a.ProofWorkDriveUrl, a.ValidationStatus, a.CreatedAtUtc,
                UserName = u.Name
            })
            .ToList();
        return Ok(rows);
    }
}
