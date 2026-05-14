using System.Text.Json;
using KudosApp.Api.Data;
using KudosApp.Api.DTOs;
using KudosApp.Api.Models;
using KudosApp.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace KudosApp.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    AppDbContext db,
    ITokenService tokenService,
    IZohoBridge zohoBridge,
    IAuditService auditService) : ControllerBase
{
    [HttpPost("zoho-sso")]
    public async Task<ActionResult<AuthResponse>> Login(ZohoSsoLoginRequest request, CancellationToken ct)
    {
        var valid = await zohoBridge.ValidateSsoAsync(request.ZohoAccessToken, request.Email, ct);
        if (!valid) return Unauthorized("Invalid Zoho token.");

        var user = db.Users.SingleOrDefault(x => x.Email.Equals(request.Email))
                   ?? AutoProvisionUser(request.Email);

        var authToken = tokenService.Generate(user);
        auditService.Write(user.UserId, "LOGIN", nameof(UserProfile), user.UserId, "{\"provider\":\"zoho-sso\"}");

        return Ok(new AuthResponse
        {
            Token = authToken.Token,
            ExpiresAtUtc = authToken.ExpiresAtUtc,
            User = new UserSummary
            {
                UserId = user.UserId,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                ManagerId = user.ManagerId,
                TeamId = user.TeamId
            }
        });
    }

    private UserProfile AutoProvisionUser(string email)
    {
        var defaultTeam = db.Teams.Select(x => x.TeamId).FirstOrDefault();
        var defaultManager = db.Users.Where(x => x.Role == AppRole.Manager).Select(x => (int?)x.UserId).FirstOrDefault();

        var user = new UserProfile
        {
            EmployeeId = $"EMP-{DateTime.UtcNow.Ticks % 100000:D5}",
            Name = email.Split('@')[0],
            Email = email,
            Role = AppRole.Employee,
            TeamId = defaultTeam,
            ManagerId = defaultManager
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }
}
