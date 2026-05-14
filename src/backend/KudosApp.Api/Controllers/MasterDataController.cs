using System.Text.Json;
using KudosApp.Api.Data;
using KudosApp.Api.Infrastructure;
using KudosApp.Api.Models;
using KudosApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KudosApp.Api.Controllers;

public sealed class UpsertAllocationInput
{
    public int UserId { get; set; }
    public int ProjectId { get; set; }
    public BillingType BillingType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}

[ApiController]
[Route("api/master-data")]
[Authorize]
public sealed class MasterDataController(AppDbContext db, IAuditService auditService) : ControllerBase
{
    [HttpGet("projects")]
    public ActionResult<IReadOnlyCollection<Project>> Projects()
    {
        return Ok(db.Projects.OrderBy(x => x.ProjectName).ToList());
    }

    [HttpGet("teams")]
    public ActionResult<IReadOnlyCollection<Team>> Teams()
    {
        return Ok(db.Teams.OrderBy(x => x.TeamName).ToList());
    }

    [HttpPost("resource-allocation")]
    [Authorize(Roles = "Employee,Manager,Admin")]
    public ActionResult<ResourceAllocation> UpsertResourceAllocation(UpsertAllocationInput input)
    {
        if (input.UserId <= 0 || input.ProjectId <= 0)
            return BadRequest("UserId and ProjectId are required.");

        var actor = User.CurrentUserId();
        var allocation = db.ResourceAllocations.SingleOrDefault(x => x.UserId == input.UserId && x.ProjectId == input.ProjectId && x.IsActive);

        if (allocation is null)
        {
            allocation = new ResourceAllocation
            {
                UserId = input.UserId,
                ProjectId = input.ProjectId,
                BillingType = input.BillingType,
                StartDate = input.StartDate,
                EndDate = input.EndDate,
                IsActive = input.IsActive
            };
            db.ResourceAllocations.Add(allocation);
        }
        else
        {
            allocation.BillingType = input.BillingType;
            allocation.StartDate = input.StartDate;
            allocation.EndDate = input.EndDate;
            allocation.IsActive = input.IsActive;
        }

        db.SaveChanges();
        auditService.Write(actor, "UPSERT_RESOURCE_ALLOCATION", nameof(ResourceAllocation), allocation.ResourceAllocationId, JsonSerializer.Serialize(input));
        return Ok(allocation);
    }
}
