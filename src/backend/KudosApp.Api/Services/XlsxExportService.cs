using System.Text.Json;
using ClosedXML.Excel;
using KudosApp.Api.DTOs;
using KudosApp.Api.Models;

namespace KudosApp.Api.Services;

public static class XlsxExportService
{
    private static readonly XLColor HeaderBg   = XLColor.FromHtml("#17324A");
    private static readonly XLColor HeaderFg   = XLColor.White;
    private static readonly XLColor AltRow     = XLColor.FromHtml("#EEF4FB");
    private static readonly XLColor BlockedClr = XLColor.FromHtml("#FEE2E2");

    // ── Entry point ──────────────────────────────────────────────────────────

    public static ExportArtifact Export(ReportRecord report)
    {
        using var wb = new XLWorkbook();

        switch (report.ReportType)
        {
            case ReportType.Weekly:
                BuildWeekly(wb, report);
                break;
            case ReportType.Monthly:
                BuildMonthly(wb, report);
                break;
            case ReportType.Quarterly:
                BuildQuarterly(wb, report);
                break;
            default:
                BuildGeneric(wb, report);
                break;
        }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);

        return new ExportArtifact
        {
            FileName    = $"KudosApp-{report.ReportType}-{report.StartDate:yyyyMMdd}-{report.EndDate:yyyyMMdd}.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Base64Content = Convert.ToBase64String(ms.ToArray())
        };
    }

    // ── Weekly ───────────────────────────────────────────────────────────────

    private static void BuildWeekly(XLWorkbook wb, ReportRecord report)
    {
        var payload = JsonSerializer.Deserialize<WeeklyReportPayload>(report.PayloadJson)
                      ?? new WeeklyReportPayload();

        // Sheet 1: Tickets
        var ws = wb.AddWorksheet("Tickets");
        SetSheetMeta(ws, $"Weekly Report — {report.StartDate:dd MMM} to {report.EndDate:dd MMM yyyy}");

        var headers = new[] { "Project", "Ticket #", "Description", "Owner", "Status", "Work Date" };
        WriteHeader(ws, 2, headers);

        int row = 3;
        foreach (var t in payload.Tickets)
        {
            ws.Cell(row, 1).Value = t.ProjectName;
            ws.Cell(row, 2).Value = t.TicketNumber;
            ws.Cell(row, 3).Value = t.Description;
            ws.Cell(row, 4).Value = t.OwnerName;
            ws.Cell(row, 5).Value = t.Status.ToString();
            ws.Cell(row, 6).Value = t.WorkDate.ToString("yyyy-MM-dd");

            if (t.Status == DailyStatus.Blocked)
                ws.Row(row).Cells(1, 6).Style.Fill.BackgroundColor = BlockedClr;
            else if (row % 2 == 0)
                ws.Row(row).Cells(1, 6).Style.Fill.BackgroundColor = AltRow;

            row++;
        }

        AutoFitAndFreeze(ws, headers.Length, 2);
        AddSummaryFooter(ws, row + 1, payload.Tickets.Count, report);

        // Sheet 2: Manager Notes
        if (!string.IsNullOrWhiteSpace(payload.ManagerNotes))
        {
            var ws2 = wb.AddWorksheet("Manager Notes");
            SetSheetMeta(ws2, "Manager Notes");
            ws2.Cell(2, 1).Value = payload.ManagerNotes;
            ws2.Cell(2, 1).Style.Alignment.WrapText = true;
            ws2.Column(1).Width = 100;
            ws2.Row(2).Height = 120;
        }
    }

    // ── Monthly ──────────────────────────────────────────────────────────────

    private static void BuildMonthly(XLWorkbook wb, ReportRecord report)
    {
        var payload = JsonSerializer.Deserialize<MonthlyReportSection>(report.PayloadJson)
                      ?? new MonthlyReportSection();

        // Sheet: Resource Utilization
        var wsRes = wb.AddWorksheet("Resource Utilization");
        SetSheetMeta(wsRes, $"Resource Utilization — {report.StartDate:MMMM yyyy}");
        WriteHeader(wsRes, 2, ["Billing Type", "Head Count"]);
        int row = 3;
        foreach (var kv in payload.ResourceUtilization)
        {
            wsRes.Cell(row, 1).Value = kv.Key;
            wsRes.Cell(row, 2).Value = kv.Value;
            if (row % 2 == 0) wsRes.Row(row).Cells(1, 2).Style.Fill.BackgroundColor = AltRow;
            row++;
        }
        AutoFitAndFreeze(wsRes, 2, 2);

        // Sheet: Engagements
        var wsEng = wb.AddWorksheet("Engagements");
        SetSheetMeta(wsEng, "Client Engagements");
        WriteHeader(wsEng, 2, ["Client", "Project", "Positions", "Details", "Created"]);
        row = 3;
        foreach (var e in payload.Engagements)
        {
            wsEng.Cell(row, 1).Value = e.ClientName;
            wsEng.Cell(row, 2).Value = e.ProjectName;
            wsEng.Cell(row, 3).Value = e.NumberOfPositions;
            wsEng.Cell(row, 4).Value = e.Details;
            wsEng.Cell(row, 5).Value = e.CreatedAtUtc.ToString("yyyy-MM-dd");
            if (row % 2 == 0) wsEng.Row(row).Cells(1, 5).Style.Fill.BackgroundColor = AltRow;
            row++;
        }
        AutoFitAndFreeze(wsEng, 5, 2);

        // Sheet: Achievements
        var wsAch = wb.AddWorksheet("Achievements");
        SetSheetMeta(wsAch, "Approved Achievements");
        WriteHeader(wsAch, 2, ["Category", "Title", "Description", "Proof URL", "Approved On"]);
        row = 3;
        foreach (var a in payload.ApprovedAchievements)
        {
            wsAch.Cell(row, 1).Value = a.Category;
            wsAch.Cell(row, 2).Value = a.Title;
            wsAch.Cell(row, 3).Value = a.Description;
            wsAch.Cell(row, 4).Value = a.ProofWorkDriveUrl ?? "";
            wsAch.Cell(row, 5).Value = a.ValidatedAtUtc?.ToString("yyyy-MM-dd") ?? "";
            if (row % 2 == 0) wsAch.Row(row).Cells(1, 5).Style.Fill.BackgroundColor = AltRow;
            row++;
        }
        AutoFitAndFreeze(wsAch, 5, 2);

        // Sheet: Sales Enquiries
        var wsSales = wb.AddWorksheet("Sales Enquiries");
        SetSheetMeta(wsSales, "Approved Sales Enquiries");
        WriteHeader(wsSales, 2, ["Client", "Requirement", "Technology", "Enquiry Date", "Status", "Coordinator"]);
        row = 3;
        foreach (var s in payload.ApprovedSalesEnquiries)
        {
            wsSales.Cell(row, 1).Value = s.ClientName;
            wsSales.Cell(row, 2).Value = s.Requirement;
            wsSales.Cell(row, 3).Value = s.Technology;
            wsSales.Cell(row, 4).Value = s.EnquiryDate.ToString("yyyy-MM-dd");
            wsSales.Cell(row, 5).Value = s.Status;
            wsSales.Cell(row, 6).Value = s.SalesCoordinator;
            if (row % 2 == 0) wsSales.Row(row).Cells(1, 6).Style.Fill.BackgroundColor = AltRow;
            row++;
        }
        AutoFitAndFreeze(wsSales, 6, 2);

        // Sheet: Sales Sessions
        var wsSess = wb.AddWorksheet("Sales Sessions");
        SetSheetMeta(wsSess, "Sales Enablement Sessions");
        WriteHeader(wsSess, 2, ["Title", "Session Date", "Description"]);
        row = 3;
        foreach (var s in payload.SalesSessions)
        {
            wsSess.Cell(row, 1).Value = s.Title;
            wsSess.Cell(row, 2).Value = s.SessionDate.ToString("yyyy-MM-dd");
            wsSess.Cell(row, 3).Value = s.Description;
            if (row % 2 == 0) wsSess.Row(row).Cells(1, 3).Style.Fill.BackgroundColor = AltRow;
            row++;
        }
        AutoFitAndFreeze(wsSess, 3, 2);

        // Sheet: Events
        var wsEvt = wb.AddWorksheet("Events");
        SetSheetMeta(wsEvt, "Team Events");
        WriteHeader(wsEvt, 2, ["Title", "Date", "Location", "Description"]);
        row = 3;
        foreach (var e in payload.Events)
        {
            wsEvt.Cell(row, 1).Value = e.Title;
            wsEvt.Cell(row, 2).Value = e.EventDate.ToString("yyyy-MM-dd");
            wsEvt.Cell(row, 3).Value = e.Location;
            wsEvt.Cell(row, 4).Value = e.Description;
            if (row % 2 == 0) wsEvt.Row(row).Cells(1, 4).Style.Fill.BackgroundColor = AltRow;
            row++;
        }
        AutoFitAndFreeze(wsEvt, 4, 2);
    }

    // ── Quarterly ────────────────────────────────────────────────────────────

    private static void BuildQuarterly(XLWorkbook wb, ReportRecord report)
    {
        var payload = JsonSerializer.Deserialize<QuarterlyReportSection>(report.PayloadJson)
                      ?? new QuarterlyReportSection();

        var ws = wb.AddWorksheet("Quarterly Summary");
        SetSheetMeta(ws, $"Q{payload.Quarter} {payload.Year} — Quarterly Summary");

        // Month headers
        var months = payload.EnquiryCountByMonth.Keys.OrderBy(k => k).ToList();
        ws.Cell(2, 1).Value = "Metric";
        for (int i = 0; i < months.Count; i++)
            ws.Cell(2, i + 2).Value = months[i];

        StyleHeaderRow(ws, 2, months.Count + 1);

        ws.Cell(3, 1).Value = "Sales Enquiries";
        ws.Cell(4, 1).Value = "Approved Achievements";
        ws.Cell(5, 1).Value = "Daily Update Entries";

        for (int i = 0; i < months.Count; i++)
        {
            ws.Cell(3, i + 2).Value = payload.EnquiryCountByMonth.GetValueOrDefault(months[i], 0);
            ws.Cell(4, i + 2).Value = payload.AchievementCountByMonth.GetValueOrDefault(months[i], 0);
            ws.Cell(5, i + 2).Value = payload.ParticipationByMonth.GetValueOrDefault(months[i], 0);
        }

        // Alternating row colour
        ws.Row(4).Cells(1, months.Count + 1).Style.Fill.BackgroundColor = AltRow;

        ws.Column(1).Width = 28;
        for (int i = 2; i <= months.Count + 1; i++) ws.Column(i).Width = 14;
        ws.SheetView.FreezeRows(2);
    }

    // ── Generic fallback ─────────────────────────────────────────────────────

    private static void BuildGeneric(XLWorkbook wb, ReportRecord report)
    {
        var ws = wb.AddWorksheet("Report");
        SetSheetMeta(ws, $"{report.ReportType} Report");
        ws.Cell(2, 1).Value = "Type";   ws.Cell(2, 2).Value = report.ReportType.ToString();
        ws.Cell(3, 1).Value = "Status"; ws.Cell(3, 2).Value = report.Status.ToString();
        ws.Cell(4, 1).Value = "Period"; ws.Cell(4, 2).Value = $"{report.StartDate:yyyy-MM-dd} to {report.EndDate:yyyy-MM-dd}";
        ws.Cell(5, 1).Value = "Payload"; ws.Cell(5, 2).Value = report.PayloadJson;
        ws.Column(1).Width = 16;
        ws.Column(2).Width = 80;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static void SetSheetMeta(IXLWorksheet ws, string title)
    {
        ws.Cell(1, 1).Value = title;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#17324A");
        ws.Row(1).Height = 22;
    }

    private static void WriteHeader(IXLWorksheet ws, int row, string[] columns)
    {
        for (int i = 0; i < columns.Length; i++)
            ws.Cell(row, i + 1).Value = columns[i];
        StyleHeaderRow(ws, row, columns.Length);
    }

    private static void StyleHeaderRow(IXLWorksheet ws, int row, int colCount)
    {
        var range = ws.Range(ws.Cell(row, 1), ws.Cell(row, colCount));
        range.Style.Fill.BackgroundColor = HeaderBg;
        range.Style.Font.FontColor = HeaderFg;
        range.Style.Font.Bold = true;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Row(row).Height = 18;
    }

    private static void AutoFitAndFreeze(IXLWorksheet ws, int colCount, int freezeRow)
    {
        for (int c = 1; c <= colCount; c++)
        {
            ws.Column(c).AdjustToContents();
            // Cap max width to avoid giant description columns
            if (ws.Column(c).Width > 55) ws.Column(c).Width = 55;
            if (ws.Column(c).Width < 10) ws.Column(c).Width = 10;
        }
        ws.SheetView.FreezeRows(freezeRow);
    }

    private static void AddSummaryFooter(IXLWorksheet ws, int row, int count, ReportRecord report)
    {
        ws.Cell(row, 1).Value = $"Total tickets: {count}";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontColor = XLColor.FromHtml("#51697F");
        ws.Cell(row + 1, 1).Value = $"Generated: {report.CreatedAtUtc:yyyy-MM-dd HH:mm} UTC";
        ws.Cell(row + 1, 1).Style.Font.FontColor = XLColor.FromHtml("#51697F");
    }
}
