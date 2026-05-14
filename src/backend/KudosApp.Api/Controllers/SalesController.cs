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
[Route("api/sales")]
[Authorize]
public sealed class SalesController(AppDbContext db, IAuditService auditService) : ControllerBase
{
    [HttpPost("enquiries")]
    public ActionResult<SalesEnquiry> CreateEnquiry(CreateSalesEnquiryInput input)
    {
        var userId = User.CurrentUserId();
        var created = new SalesEnquiry
        {
            ClientName = input.ClientName.Trim(),
            Requirement = input.Requirement.Trim(),
            Technology = input.Technology.Trim(),
            EnquiryDate = input.EnquiryDate,
            SalesCoordinator = input.SalesCoordinator.Trim(),
            Status = input.Status.Trim(),
            CreatedByUserId = userId,
            ValidationStatus = ValidationStatus.Pending
        };
        db.SalesEnquiries.Add(created);
        db.SaveChanges();

        db.Validations.Add(new ValidationRecord
        {
            EntityType = ValidationEntityType.SalesEnquiry,
            EntityId = created.SalesEnquiryId,
            Status = ValidationStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        });
        db.SaveChanges();

        auditService.Write(userId, "CREATE_SALES_ENQUIRY", nameof(SalesEnquiry), created.SalesEnquiryId, JsonSerializer.Serialize(created));
        return Ok(created);
    }

    [HttpGet("enquiries")]
    [Authorize(Roles = "Manager,Admin")]
    public ActionResult<IReadOnlyCollection<SalesEnquiry>> ListEnquiries([FromQuery] string? status = null)
    {
        var rows = db.SalesEnquiries
            .Where(x => string.IsNullOrWhiteSpace(status) || x.Status == status)
            .OrderByDescending(x => x.EnquiryDate)
            .ToList();
        return Ok(rows);
    }

    [HttpPost("engagements")]
    [Authorize(Roles = "Manager,Admin")]
    public ActionResult<Engagement> CreateEngagement(CreateEngagementInput input)
    {
        var userId = User.CurrentUserId();
        var created = new Engagement
        {
            ClientName = input.ClientName.Trim(),
            ProjectName = input.ProjectName.Trim(),
            NumberOfPositions = input.NumberOfPositions,
            Details = input.Details.Trim(),
            CreatedByUserId = userId,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Engagements.Add(created);
        db.SaveChanges();

        auditService.Write(userId, "CREATE_ENGAGEMENT", nameof(Engagement), created.EngagementId, JsonSerializer.Serialize(created));
        return Ok(created);
    }

    [HttpPost("sessions")]
    [Authorize(Roles = "Manager,Admin")]
    public ActionResult<SalesSession> CreateSession(CreateSalesSessionInput input)
    {
        var userId = User.CurrentUserId();
        var created = new SalesSession
        {
            Title = input.Title.Trim(),
            SessionDate = input.SessionDate,
            TeamId = input.TeamId,
            Description = input.Description.Trim(),
            CreatedByUserId = userId
        };
        db.SalesSessions.Add(created);
        db.SaveChanges();

        auditService.Write(userId, "CREATE_SALES_SESSION", nameof(SalesSession), created.SalesSessionId, JsonSerializer.Serialize(created));
        return Ok(created);
    }
}
