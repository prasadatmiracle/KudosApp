namespace KudosApp.Api.Infrastructure;

public sealed class ZohoOptions
{
    public const string SectionName = "Zoho";

    // ── Cliq ────────────────────────────────────────────────────────────────
    /// <summary>Incoming webhook URL for team channel broadcasts (no auth needed).</summary>
    public string CliqWebhookUrl { get; set; } = "";

    /// <summary>Bot name registered in Zoho Cliq — used for per-user DMs.</summary>
    public string CliqBotName { get; set; } = "";

    /// <summary>Short-lived OAuth token for the bot. Refresh manually until P18 wires OAuth.</summary>
    public string CliqBotOAuthToken { get; set; } = "";

    /// <summary>Base URL — use cliq.zoho.eu for EU data residency.</summary>
    public string CliqApiBaseUrl { get; set; } = "https://cliq.zoho.in/api/v2";

    // ── Mail (P7) ────────────────────────────────────────────────────────────
    public string MailAccountId { get; set; } = "";
    public string MailClientId { get; set; } = "";
    public string MailClientSecret { get; set; } = "";
    public string MailRefreshToken { get; set; } = "";
    public string MailFromAddress { get; set; } = "";
    public string MailApiBaseUrl { get; set; } = "https://mail.zoho.in/api";

    // ── WorkDrive (P8) ───────────────────────────────────────────────────────
    public string WorkDriveBaseUrl { get; set; } = "";
    public string WorkDriveFolderId { get; set; } = "";

    // ── Meetings (P14) ───────────────────────────────────────────────────────
    public string MeetingsApiBaseUrl { get; set; } = "";
}
