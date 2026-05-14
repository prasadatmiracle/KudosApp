using System.Text;
using KudosApp.Api.Data;
using KudosApp.Api.DTOs;
using KudosApp.Api.Infrastructure;
using KudosApp.Api.Models;
using KudosApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KudosApp.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController(
    AppDbContext db,
    IVisibilityService visibility,
    IAuditService auditService) : ControllerBase
{
    [HttpGet("me")]
    public ActionResult<UserSummary> Me()
    {
        var userId = User.CurrentUserId();
        var user = db.Users.SingleOrDefault(x => x.UserId == userId);
        if (user is null) return NotFound();
        return Ok(ToSummary(user));
    }

    [HttpGet("team")]
    [Authorize(Roles = "Manager,Admin")]
    public ActionResult<IReadOnlyCollection<UserSummary>> Team()
    {
        var userId = User.CurrentUserId();
        var role = User.CurrentRole();
        var visible = visibility.TeamViewableUserIds(userId);

        var users = db.Users
            .Where(x => role == AppRole.Admin || visible.Contains(x.UserId))
            .OrderBy(x => x.Name)
            .ToList()
            .Select(ToSummary)
            .ToList();

        return Ok(users);
    }

    [HttpPost("import-csv")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ImportCsv()
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(body)) return BadRequest("CSV body is empty.");

        var lines = body.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2) return BadRequest("CSV should include header + at least one row.");

        var imported = 0;
        for (var i = 1; i < lines.Length; i++)
        {
            var cols = lines[i].Split(',', StringSplitOptions.TrimEntries);
            if (cols.Length < 6) continue;

            if (!Enum.TryParse<AppRole>(cols[3], true, out var role)) role = AppRole.Employee;

            _ = int.TryParse(cols[4], out var teamId);
            int? managerId = int.TryParse(cols[5], out var parsedManagerId) ? parsedManagerId : null;

            var existing = db.Users.SingleOrDefault(x => x.Email.ToLower() == cols[2].ToLower());
            if (existing is not null)
            {
                existing.EmployeeId = cols[0];
                existing.Name = cols[1];
                existing.Role = role;
                existing.TeamId = teamId;
                existing.ManagerId = managerId;
                existing.IsActive = true;
            }
            else
            {
                db.Users.Add(new UserProfile
                {
                    EmployeeId = cols[0],
                    Name = cols[1],
                    Email = cols[2],
                    Role = role,
                    TeamId = teamId,
                    ManagerId = managerId
                });
            }

            imported++;
        }

        db.SaveChanges();
        auditService.Write(User.CurrentUserId(), "IMPORT_USERS_CSV", nameof(UserProfile), imported, $"{{\"rows\":{imported}}}");
        return Ok(new { imported });
    }

    private static UserSummary ToSummary(UserProfile user) => new()
    {
        UserId = user.UserId,
        Name = user.Name,
        Email = user.Email,
        Role = user.Role,
        ManagerId = user.ManagerId,
        TeamId = user.TeamId
    };
}
