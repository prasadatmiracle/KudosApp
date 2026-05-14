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
[Route("api/validations")]
[Authorize(Roles = "Manager,Admin")]
public sealed class ValidationsController(
    AppDbContext db,
    IVisibilityService visibility,
    IAuditService auditService) : ControllerBase
{
    [HttpGet("pending")]
    public ActionResult<IReadOnlyCollection<ValidationRecord>> Pending()
    {
        var userId = User.CurrentUserId();
        var role = User.CurrentRole();
        var visible = visibility.TeamViewableUserIds(userId);

        var pending = db.Validations
            .Where(x => x.Status == ValidationStatus.Pending)
            .OrderBy(x => x.CreatedAtUtc)
            .ToList();

        if (role != AppRole.Admin)
        {
            var achievementIds = db.Achievements
                .Where(x => visible.Contains(x.UserId))
                .Select(x => x.AchievementId)
                .ToHashSet();

            var enquiryIds = db.SalesEnquiries
                .Where(x => visible.Contains(x.CreatedByUserId))
                .Select(x => x.SalesEnquiryId)
                .ToHashSet();

            pending = pending.Where(x =>
                x.EntityType == ValidationEntityType.Achievement
                    ? achievementIds.Contains(x.EntityId)
                    : enquiryIds.Contains(x.EntityId)).ToList();
        }

        return Ok(pending);
    }

    [HttpGet("pending-detail")]
    public ActionResult<IReadOnlyCollection<object>> PendingDetail()
    {
        var userId = User.CurrentUserId();
        var role   = User.CurrentRole();
        var visible = visibility.TeamViewableUserIds(userId);

        var pending = db.Validations
            .Where(x => x.Status == ValidationStatus.Pending)
            .OrderBy(x => x.CreatedAtUtc)
            .ToList();

        if (role != AppRole.Admin)
        {
            var achievementIds = db.Achievements
                .Where(x => visible.Contains(x.UserId))
                .Select(x => x.AchievementId)
                .ToHashSet();
            var enquiryIds = db.SalesEnquiries
                .Where(x => visible.Contains(x.CreatedByUserId))
                .Select(x => x.SalesEnquiryId)
                .ToHashSet();
            pending = pending.Where(x =>
                x.EntityType == ValidationEntityType.Achievement
                    ? achievementIds.Contains(x.EntityId)
                    : enquiryIds.Contains(x.EntityId)).ToList();
        }

        var achIds = pending.Where(x => x.EntityType == ValidationEntityType.Achievement)
                            .Select(x => x.EntityId).ToHashSet();
        var enqIds = pending.Where(x => x.EntityType == ValidationEntityType.SalesEnquiry)
                            .Select(x => x.EntityId).ToHashSet();

        var achievements = db.Achievements
            .Where(x => achIds.Contains(x.AchievementId))
            .Join(db.Users, a => a.UserId, u => u.UserId,
                (a, u) => new { a.AchievementId, a.Category, a.Title, a.Description, a.ProofWorkDriveUrl, UserName = u.Name })
            .ToDictionary(x => x.AchievementId);

        var enquiries = db.SalesEnquiries
            .Where(x => enqIds.Contains(x.SalesEnquiryId))
            .Join(db.Users, e => e.CreatedByUserId, u => u.UserId,
                (e, u) => new { e.SalesEnquiryId, e.ClientName, e.Technology, e.Status, UserName = u.Name })
            .ToDictionary(x => x.SalesEnquiryId);

        var result = pending.Select(v =>
        {
            if (v.EntityType == ValidationEntityType.Achievement && achievements.TryGetValue(v.EntityId, out var a))
                return (object)new
                {
                    v.ValidationRecordId, v.EntityType, v.EntityId, v.CreatedAtUtc,
                    a.UserName, a.Category, a.Title, a.Description,
                    ProofUrl = a.ProofWorkDriveUrl
                };
            if (v.EntityType == ValidationEntityType.SalesEnquiry && enquiries.TryGetValue(v.EntityId, out var e))
                return (object)new
                {
                    v.ValidationRecordId, v.EntityType, v.EntityId, v.CreatedAtUtc,
                    e.UserName, Category = "Sales", Title = e.ClientName,
                    Description = $"{e.Technology} · {e.Status}",
                    ProofUrl = (string?)null
                };
            return (object)new
            {
                v.ValidationRecordId, v.EntityType, v.EntityId, v.CreatedAtUtc,
                UserName = "Unknown", Category = "", Title = $"Entity #{v.EntityId}", Description = "", ProofUrl = (string?)null
            };
        }).ToList();

        return Ok(result);
    }

    [HttpPost("{validationRecordId:int}/decision")]
    public IActionResult Decide(int validationRecordId, ValidationDecision input)
    {
        var userId = User.CurrentUserId();
        var validation = db.Validations.SingleOrDefault(x => x.ValidationRecordId == validationRecordId);
        if (validation is null) return NotFound();

        ApplyDecision(validation, input.Status, userId, input.Remarks);
        ApplyDecisionToEntity(validation, input.Status, userId);
        db.SaveChanges();

        auditService.Write(userId, "VALIDATE_SINGLE", nameof(ValidationRecord), validationRecordId, JsonSerializer.Serialize(input));
        return Ok();
    }

    [HttpPost("bulk")]
    public IActionResult Bulk(BulkValidationDecision input)
    {
        if (input.ValidationRecordIds.Count == 0) return BadRequest("ValidationRecordIds cannot be empty.");

        var userId = User.CurrentUserId();
        var ids = input.ValidationRecordIds.Distinct().ToList();
        var validations = db.Validations.Where(x => ids.Contains(x.ValidationRecordId)).ToList();

        foreach (var validation in validations)
        {
            ApplyDecision(validation, input.Status, userId, input.Remarks);
            ApplyDecisionToEntity(validation, input.Status, userId);
        }

        db.SaveChanges();

        auditService.Write(userId, "VALIDATE_BULK", nameof(ValidationRecord), validations.Count, JsonSerializer.Serialize(input));
        return Ok(new { updated = validations.Count });
    }

    private static void ApplyDecision(ValidationRecord validation, ValidationStatus status, int userId, string? remarks)
    {
        validation.Status = status;
        validation.ValidatedByUserId = userId;
        validation.Remarks = remarks?.Trim();
        validation.UpdatedAtUtc = DateTime.UtcNow;
    }

    private void ApplyDecisionToEntity(ValidationRecord validation, ValidationStatus status, int userId)
    {
        switch (validation.EntityType)
        {
            case ValidationEntityType.Achievement:
            {
                var achievement = db.Achievements.SingleOrDefault(x => x.AchievementId == validation.EntityId);
                if (achievement is null) return;
                achievement.ValidationStatus = status;
                achievement.ValidatedByUserId = userId;
                achievement.ValidatedAtUtc = DateTime.UtcNow;
                break;
            }
            case ValidationEntityType.SalesEnquiry:
            {
                var enquiry = db.SalesEnquiries.SingleOrDefault(x => x.SalesEnquiryId == validation.EntityId);
                if (enquiry is null) return;
                enquiry.ValidationStatus = status;
                enquiry.ValidatedByUserId = userId;
                enquiry.ValidatedAtUtc = DateTime.UtcNow;
                break;
            }
        }
    }
}
