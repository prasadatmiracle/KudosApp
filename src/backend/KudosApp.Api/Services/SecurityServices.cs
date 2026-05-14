using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using KudosApp.Api.Infrastructure;
using KudosApp.Api.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace KudosApp.Api.Services;

public interface ITokenService
{
    AuthToken Generate(UserProfile user);
}

public sealed class AuthToken
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}

public sealed class TokenService(IOptions<JwtOptions> options) : ITokenService
{
    public AuthToken Generate(UserProfile user)
    {
        var jwt = options.Value;
        var expires = DateTime.UtcNow.AddMinutes(jwt.ExpiryMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new("user_id", user.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("team_id", user.TeamId.ToString())
        };

        if (user.ManagerId.HasValue)
        {
            claims.Add(new Claim("manager_id", user.ManagerId.Value.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new AuthToken
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAtUtc = expires
        };
    }
}

public interface IUserContext
{
    int UserId { get; }
    AppRole Role { get; }
}

public sealed class UserContext(IHttpContextAccessor accessor) : IUserContext
{
    public int UserId
    {
        get
        {
            var raw = accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? accessor.HttpContext?.User?.FindFirstValue("user_id");
            return int.TryParse(raw, out var parsed) ? parsed : 0;
        }
    }

    public AppRole Role
    {
        get
        {
            var raw = accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<AppRole>(raw, ignoreCase: true, out var role) ? role : AppRole.Employee;
        }
    }
}

public interface IZohoBridge
{
    Task<bool> ValidateSsoAsync(string accessToken, string email, CancellationToken ct = default);

    /// <summary>
    /// Sends a Cliq notification. Uses channel webhook (broadcast) and/or bot DM (per-email)
    /// depending on what is configured in ZohoOptions. Failures are logged, never thrown.
    /// </summary>
    Task SendCliqNotificationAsync(string message, IReadOnlyCollection<string> emails, CancellationToken ct = default);

    Task<(string summary, string actionItems)> IngestMeetingTranscriptAsync(string transcriptText, CancellationToken ct = default);
}

public sealed class ZohoBridge(
    IHttpClientFactory httpClientFactory,
    IOptions<ZohoOptions> options,
    ILogger<ZohoBridge> logger) : IZohoBridge
{
    private ZohoOptions Zoho => options.Value;

    // P18 will replace this with real Zoho OAuth token introspection.
    public Task<bool> ValidateSsoAsync(string accessToken, string email, CancellationToken ct = default)
    {
        var ok = !string.IsNullOrWhiteSpace(accessToken) && !string.IsNullOrWhiteSpace(email);
        return Task.FromResult(ok);
    }

    public async Task SendCliqNotificationAsync(string message, IReadOnlyCollection<string> emails, CancellationToken ct = default)
    {
        var tasks = new List<Task>();

        if (!string.IsNullOrWhiteSpace(Zoho.CliqWebhookUrl))
            tasks.Add(PostChannelWebhookAsync(message, ct));

        if (!string.IsNullOrWhiteSpace(Zoho.CliqBotOAuthToken)
            && !string.IsNullOrWhiteSpace(Zoho.CliqBotName)
            && emails.Count > 0)
        {
            foreach (var email in emails)
                tasks.Add(PostBotDmAsync(message, email, ct));
        }

        if (tasks.Count == 0)
        {
            logger.LogWarning(
                "Zoho Cliq not configured (CliqWebhookUrl and CliqBotOAuthToken both empty). " +
                "Message not sent: {Message}", message);
            return;
        }

        await Task.WhenAll(tasks);
    }

    // P14 will replace this with Azure OpenAI / Zoho Zia transcript parsing.
    public Task<(string summary, string actionItems)> IngestMeetingTranscriptAsync(string transcriptText, CancellationToken ct = default)
    {
        var summary = transcriptText.Length <= 200 ? transcriptText : transcriptText[..200] + "...";
        return Task.FromResult((summary, "Review meeting transcript and assign owners."));
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task PostChannelWebhookAsync(string message, CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient("ZohoCliq");
            var body = JsonSerializer.Serialize(new { text = message });
            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(Zoho.CliqWebhookUrl, content, ct);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("Cliq channel webhook failed [{Status}]: {Error}", (int)response.StatusCode, error);
            }
            else
            {
                logger.LogInformation("Cliq channel webhook posted OK.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cliq channel webhook POST threw an exception.");
        }
    }

    private async Task PostBotDmAsync(string message, string email, CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient("ZohoCliq");
            var baseUrl = Zoho.CliqApiBaseUrl.TrimEnd('/');
            var botName = Uri.EscapeDataString(Zoho.CliqBotName);
            var encodedEmail = Uri.EscapeDataString(email);
            var url = $"{baseUrl}/bots/{botName}/direct_message?email={encodedEmail}";

            var body = JsonSerializer.Serialize(new { text = message });
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Zoho-oauthtoken", Zoho.CliqBotOAuthToken);

            var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("Cliq bot DM to {Email} failed [{Status}]: {Error}",
                    email, (int)response.StatusCode, error);
            }
            else
            {
                logger.LogInformation("Cliq bot DM sent to {Email}.", email);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cliq bot DM to {Email} threw an exception.", email);
        }
    }
}
