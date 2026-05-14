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
[Route("api/reports")]
[Authorize]
public sealed class ReportsController(
    AppDbContext db,
    IReportService reportService,
    IAuditService auditService,
    IZohoBridge zohoBridge) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Manager,Admin,Hr")]
    public ActionResult<IReadOnlyCollection<ReportRecord>> List([FromQuery] ReportType? reportType = null)
    {
        var rows = db.Reports
            .Where(x => !reportType.HasValue || x.ReportType == reportType.Value)
            .Where(x => User.CurrentRole() != AppRole.Hr || x.Status == ReportStatus.Locked)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToList();
        return Ok(rows);
    }

    [HttpPost("weekly/generate")]
    [Authorize(Roles = "Manager,Admin")]
    public ActionResult<ReportRecord> GenerateWeekly([FromQuery] DateOnly startDate, [FromQuery] DateOnly endDate)
    {
        var report = reportService.GenerateWeekly(User.CurrentUserId(), startDate, endDate);
        auditService.Write(User.CurrentUserId(), "GENERATE_WEEKLY_REPORT", nameof(ReportRecord), report.ReportRecordId, "{}");
        return Ok(report);
    }

    [HttpPost("monthly/generate")]
    [Authorize(Roles = "Manager,Admin")]
    public ActionResult<ReportRecord> GenerateMonthly([FromQuery] int year, [FromQuery] int month)
    {
        var report = reportService.GenerateMonthly(User.CurrentUserId(), year, month);
        auditService.Write(User.CurrentUserId(), "GENERATE_MONTHLY_REPORT", nameof(ReportRecord), report.ReportRecordId, "{}");
        return Ok(report);
    }

    [HttpPost("quarterly/generate")]
    [Authorize(Roles = "Manager,Admin")]
    public ActionResult<ReportRecord> GenerateQuarterly([FromQuery] int year, [FromQuery] int quarter)
    {
        var report = reportService.GenerateQuarterly(User.CurrentUserId(), year, quarter);
        auditService.Write(User.CurrentUserId(), "GENERATE_QUARTERLY_REPORT", nameof(ReportRecord), report.ReportRecordId, "{}");
        return Ok(report);
    }

    [HttpGet("{reportRecordId:int}")]
    [Authorize(Roles = "Manager,Admin,Hr")]
    public ActionResult<ReportRecord> Get(int reportRecordId)
    {
        var report = db.Reports.SingleOrDefault(x => x.ReportRecordId == reportRecordId);
        if (report is null) return NotFound();
        if (User.CurrentRole() == AppRole.Hr && report.Status != ReportStatus.Locked) return Forbid();
        return Ok(report);
    }

    [HttpPut("weekly/{reportRecordId:int}")]
    [Authorize(Roles = "Manager,Admin")]
    public ActionResult<ReportRecord> EditWeekly(int reportRecordId, ReportEditInput input)
    {
        var report = db.Reports.SingleOrDefault(x => x.ReportRecordId == reportRecordId && x.ReportType == ReportType.Weekly);
        if (report is null) return NotFound();
        if (report.Status == ReportStatus.Locked) return Conflict("Report is locked.");

        var payload = JsonSerializer.Deserialize<WeeklyReportPayload>(report.PayloadJson) ?? new WeeklyReportPayload();
        payload.ManagerNotes = input.ManagerNotes.Trim();
        report.PayloadJson = JsonSerializer.Serialize(payload);
        report.UpdatedAtUtc = DateTime.UtcNow;
        db.SaveChanges();

        auditService.Write(User.CurrentUserId(), "EDIT_WEEKLY_REPORT", nameof(ReportRecord), report.ReportRecordId, "{}");
        return Ok(report);
    }

    [HttpPost("weekly/{reportRecordId:int}/submit")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<ReportRecord>> SubmitWeekly(int reportRecordId, CancellationToken ct)
    {
        var report = db.Reports.SingleOrDefault(x => x.ReportRecordId == reportRecordId && x.ReportType == ReportType.Weekly);
        if (report is null) return NotFound();

        report.Status = ReportStatus.Locked;
        report.UpdatedAtUtc = DateTime.UtcNow;
        db.SaveChanges();

        auditService.Write(User.CurrentUserId(), "SUBMIT_LOCK_WEEKLY_REPORT", nameof(ReportRecord), report.ReportRecordId, "{}");

        // P7: email HR + all admins when report is locked
        await SendReportEmailAsync(report, ct);

        return Ok(report);
    }

    [HttpPost("{reportRecordId:int}/reopen")]
    [Authorize(Roles = "Admin")]
    public ActionResult<ReportRecord> Reopen(int reportRecordId)
    {
        var report = db.Reports.SingleOrDefault(x => x.ReportRecordId == reportRecordId);
        if (report is null) return NotFound();

        report.Status = ReportStatus.Draft;
        report.UpdatedAtUtc = DateTime.UtcNow;
        db.SaveChanges();

        auditService.Write(User.CurrentUserId(), "REOPEN_REPORT", nameof(ReportRecord), report.ReportRecordId, "{}");
        return Ok(report);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task SendReportEmailAsync(ReportRecord report, CancellationToken ct)
    {
        var recipients = db.Users
            .Where(x => x.IsActive && (x.Role == AppRole.Hr || x.Role == AppRole.Admin))
            .Select(x => x.Email)
            .ToList();

        if (recipients.Count == 0) return;

        var subject = $"KudosApp — {report.ReportType} Report {report.StartDate:dd MMM} – {report.EndDate:dd MMM yyyy} (Locked)";
        var html = $"""
            <h2>KudosApp {report.ReportType} Report</h2>
            <p><strong>Period:</strong> {report.StartDate:dd MMM yyyy} – {report.EndDate:dd MMM yyyy}</p>
            <p><strong>Status:</strong> Locked</p>
            <p><strong>Report #:</strong> {report.ReportRecordId}</p>
            <hr/>
            <p>Log in to KudosApp to view and export the full report.</p>
            """;

        await zohoBridge.SendMailAsync(subject, html, recipients, ct: ct);
    }

    [HttpGet("{reportRecordId:int}/export")]
    [Authorize(Roles = "Manager,Admin,Hr")]
    public ActionResult<ExportArtifact> Export(int reportRecordId, [FromQuery] string format = "pdf")
    {
        var report = db.Reports.SingleOrDefault(x => x.ReportRecordId == reportRecordId);
        if (report is null) return NotFound();
        if (User.CurrentRole() == AppRole.Hr && report.Status != ReportStatus.Locked) return Forbid();

        var artifact = reportService.Export(report, format);
        auditService.Write(User.CurrentUserId(), "EXPORT_REPORT", nameof(ReportRecord), report.ReportRecordId, $"{{\"format\":\"{format}\"}}");
        return Ok(artifact);
    }
}
