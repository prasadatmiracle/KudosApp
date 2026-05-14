using KudosApp.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KudosApp.Api.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize(Roles = "Admin")]
public sealed class AuditController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyCollection<object>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var rows = db.AuditEntries
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.AuditEntryId,
                x.ActorUserId,
                x.Action,
                x.EntityType,
                x.EntityId,
                x.MetadataJson,
                x.CreatedAtUtc
            })
            .ToList();

        return Ok(rows);
    }
}
