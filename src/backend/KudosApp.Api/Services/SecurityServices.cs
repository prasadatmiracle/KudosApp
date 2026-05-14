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

public sealed class MailAttachment
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = "text/plain";
    public byte[] Data { get; init; } = [];
}

public interface IZohoBridge
{
    Task<bool> ValidateSsoAsync(string accessToken, string email, CancellationToken ct = default);

    /// <summary>
    /// Sends a Cliq notification. Uses channel webhook (broadcast) and/or bot DM (per-email)
    /// depending on what is configured in ZohoOptions. Failures are logged, never thrown.
    /// </summary>
    Task SendCliqNotificationAsync(string message, IReadOnlyCollection<string> emails, CancellationToken ct = default);

    /// <summary>
    /// Sends an email via Zoho Mail API. Falls back silently if Mail is not configured.
    /// </summary>
    Task SendMailAsync(string subject, string htmlBody, IReadOnlyCollection<string> toAddresses,
        IReadOnlyCollection<MailAttachment>? attachments = null, CancellationToken ct = default);

    /// <summary>
    /// Uploads a file to Zoho WorkDrive and returns the public file URL.
    /// Returns null if WorkDrive is not configured.
    /// </summary>
    Task<string?> UploadToWorkDriveAsync(string fileName, string contentType, Stream data, CancellationToken ct = default);

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

    // ── P7: Zoho Mail ────────────────────────────────────────────────────────

    public async Task SendMailAsync(string subject, string htmlBody,
        IReadOnlyCollection<string> toAddresses,
        IReadOnlyCollection<MailAttachment>? attachments = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(Zoho.MailAccountId)
            || string.IsNullOrWhiteSpace(Zoho.MailRefreshToken))
        {
            logger.LogWarning("Zoho Mail not configured — email not sent: {Subject}", subject);
            return;
        }

        try
        {
            var accessToken = await GetMailAccessTokenAsync(ct);
            if (accessToken is null) return;

            var client = httpClientFactory.CreateClient("ZohoCliq");
            var url = $"{Zoho.MailApiBaseUrl.TrimEnd('/')}/accounts/{Zoho.MailAccountId}/messages";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Zoho-oauthtoken", accessToken);

            var body = new
            {
                fromAddress = Zoho.MailFromAddress,
                toAddress = string.Join(",", toAddresses),
                subject,
                content = htmlBody,
                mailFormat = "html"
            };
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("Zoho Mail send failed [{Status}]: {Error}", (int)response.StatusCode, error);
            }
            else
            {
                logger.LogInformation("Zoho Mail sent to {Count} recipients: {Subject}", toAddresses.Count, subject);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Zoho Mail SendMailAsync threw an exception for subject: {Subject}", subject);
        }
    }

    private async Task<string?> GetMailAccessTokenAsync(CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient("ZohoCliq");
            var tokenUrl = "https://accounts.zoho.in/oauth/v2/token";
            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"]    = "refresh_token",
                ["client_id"]     = Zoho.MailClientId,
                ["client_secret"] = Zoho.MailClientSecret,
                ["refresh_token"] = Zoho.MailRefreshToken
            });

            var response = await client.PostAsync(tokenUrl, form, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Zoho token refresh failed [{Status}].", (int)response.StatusCode);
                return null;
            }

            var json = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(ct));
            return json.TryGetProperty("access_token", out var tok) ? tok.GetString() : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Zoho Mail token refresh threw an exception.");
            return null;
        }
    }

    // ── P8: Zoho WorkDrive ───────────────────────────────────────────────────

    public async Task<string?> UploadToWorkDriveAsync(string fileName, string contentType,
        Stream data, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(Zoho.WorkDriveBaseUrl)
            || string.IsNullOrWhiteSpace(Zoho.WorkDriveFolderId))
        {
            logger.LogWarning("Zoho WorkDrive not configured — file not uploaded: {File}", fileName);
            return null;
        }

        try
        {
            var accessToken = await GetMailAccessTokenAsync(ct); // reuses same OAuth app
            if (accessToken is null) return null;

            var client = httpClientFactory.CreateClient("ZohoCliq");
            var url = $"{Zoho.WorkDriveBaseUrl.TrimEnd('/')}/api/v1/upload";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Zoho-oauthtoken", accessToken);
            request.Headers.Add("overrideNameConflict", "true");

            using var form = new MultipartFormDataContent();
            using var fileContent = new StreamContent(data);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            form.Add(fileContent, "content", fileName);
            form.Add(new StringContent(Zoho.WorkDriveFolderId), "parent_id");
            form.Add(new StringContent("true"), "override-name-conflict");
            request.Content = form;

            var response = await client.SendAsync(request, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("WorkDrive upload failed [{Status}]: {Error}", (int)response.StatusCode, responseBody);
                return null;
            }

            // Response: { "data": { "attributes": { "permalink": "https://..." } } }
            var json = JsonSerializer.Deserialize<JsonElement>(responseBody);
            if (json.TryGetProperty("data", out var dataEl)
                && dataEl.TryGetProperty("attributes", out var attr)
                && attr.TryGetProperty("permalink", out var link))
            {
                var fileUrl = link.GetString();
                logger.LogInformation("WorkDrive upload succeeded: {File} → {Url}", fileName, fileUrl);
                return fileUrl;
            }

            logger.LogWarning("WorkDrive upload OK but could not parse permalink from response.");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "WorkDrive UploadToWorkDriveAsync threw an exception for file: {File}", fileName);
            return null;
        }
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
