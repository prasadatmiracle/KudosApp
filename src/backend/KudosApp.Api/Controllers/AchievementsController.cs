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
    IPointsService pointsService) : ControllerBase
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
