using System.Text.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using KudosApp.Api.DTOs;
using KudosApp.Api.Models;
using D = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace KudosApp.Api.Services;

/// <summary>
/// Generates a 10-slide PPTX monthly report deck using DocumentFormat.OpenXml.
/// Slide dimensions: 10 × 5.63 in (widescreen 16:9). Units are EMU (914400 = 1 inch).
/// </summary>
public static class PptxExportService
{
    // ── EMU constants ────────────────────────────────────────────────────────
    private const long SlideW  = 9144000;   // 10 inches
    private const long SlideH  = 5143500;   // 5.625 inches
    private const long Margin  = 457200;    // 0.5 inch
    private const long ContentW = SlideW - Margin * 2;

    // ── Brand colours (RRGGBB, no #) ────────────────────────────────────────
    private const string Navy   = "17324A";
    private const string Blue   = "1E6EA7";
    private const string LtBlue = "EEF4FB";
    private const string Green  = "16A34A";
    private const string Orange = "EA580C";
    private const string Gray   = "51697F";
    private const string White  = "FFFFFF";
    private const string OffWht = "F8FAFC";

    // ── Entry point ──────────────────────────────────────────────────────────

    public static ExportArtifact Export(ReportRecord report)
    {
        var payload = JsonSerializer.Deserialize<MonthlyReportSection>(report.PayloadJson)
                      ?? new MonthlyReportSection();

        using var ms = new MemoryStream();
        using (var prs = PresentationDocument.Create(ms, PresentationDocumentType.Presentation))
        {
            var presPart = prs.AddPresentationPart();
            presPart.Presentation = BuildPresentation();

            var masterPart   = presPart.AddNewPart<SlideMasterPart>();
            var layoutPart   = masterPart.AddNewPart<SlideLayoutPart>();
            var themePart    = masterPart.AddNewPart<ThemePart>();
            themePart.Theme  = BuildTheme();
            layoutPart.SlideLayout = BuildLayout();
            masterPart.SlideMaster = BuildMaster(masterPart, layoutPart);

            var slideIds = new SlideIdList();
            uint slideId = 256;

            void AddSlide(Action<SlidePart, SlideLayoutPart> builder)
            {
                var slidePart = presPart.AddNewPart<SlidePart>();
                slidePart.AddPart(layoutPart);
                builder(slidePart, layoutPart);
                var id = new SlideId { Id = slideId++, RelationshipId = presPart.GetIdOfPart(slidePart) };
                slideIds.Append(id);
            }

            // 10 slides
            AddSlide((sp, lp) => BuildCoverSlide(sp, report));
            AddSlide((sp, lp) => BuildTeamSummarySlide(sp, payload, report));
            AddSlide((sp, lp) => BuildResourceSlide(sp, payload));
            AddSlide((sp, lp) => BuildEngagementsSlide(sp, payload));
            AddSlide((sp, lp) => BuildAchievementsSlide(sp, payload));
            AddSlide((sp, lp) => BuildSalesPipelineSlide(sp, payload));
            AddSlide((sp, lp) => BuildSalesSessionsSlide(sp, payload));
            AddSlide((sp, lp) => BuildEventsSlide(sp, payload));
            AddSlide((sp, lp) => BuildKpiSlide(sp, payload, report));
            AddSlide((sp, lp) => BuildClosingSlide(sp, report));

            presPart.Presentation.SlideIdList = slideIds;
            presPart.Presentation.SlideMasterIdList = new SlideMasterIdList(
                new SlideMasterId { Id = 2147483648U, RelationshipId = presPart.GetIdOfPart(masterPart) });
            presPart.Presentation.Save();
        }

        return new ExportArtifact
        {
            FileName      = $"KudosApp-Monthly-{report.StartDate:yyyyMMdd}.pptx",
            ContentType   = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            Base64Content = Convert.ToBase64String(ms.ToArray())
        };
    }

    // ── Slides ───────────────────────────────────────────────────────────────

    private static void BuildCoverSlide(SlidePart sp, ReportRecord report)
    {
        var shapes = new ShapeTree(Nv(), GroupShapeProperties());

        // Full-bleed navy background
        shapes.Append(Rect(0, 0, SlideW, SlideH, Navy));
        // Accent stripe bottom-left
        shapes.Append(Rect(0, SlideH - 180000, 2800000, 180000, Blue));

        // Title
        shapes.Append(TextBox(Margin, 900000, ContentW, 700000,
            "Monthly Report", 4800, White, bold: true));

        // Period subtitle
        shapes.Append(TextBox(Margin, 1680000, ContentW, 350000,
            $"{report.StartDate:MMMM yyyy}  ·  Microsoft Practice Team", 2000, LtBlue));

        // Generated date
        shapes.Append(TextBox(Margin, 4600000, ContentW, 280000,
            $"Generated {report.CreatedAtUtc:dd MMM yyyy}  ·  KudosApp", 1400, Gray));

        // Decorative circle top-right
        shapes.Append(Ellipse(SlideW - 900000, -300000, 1100000, 1100000, Blue, opacity: 40));
        shapes.Append(Ellipse(SlideW - 600000, 200000, 700000, 700000, Navy, opacity: 80));

        SetSlide(sp, shapes);
    }

    private static void BuildTeamSummarySlide(SlidePart sp, MonthlyReportSection payload, ReportRecord report)
    {
        var shapes = new ShapeTree(Nv(), GroupShapeProperties());
        shapes.Append(Rect(0, 0, SlideW, SlideH, OffWht));
        shapes.Append(Rect(0, 0, SlideW, 600000, Navy));
        shapes.Append(TextBox(Margin, 180000, ContentW, 320000, "Team at a Glance", 2800, White, bold: true));
        shapes.Append(TextBox(Margin, 440000, 3000000, 200000, $"{report.StartDate:MMMM yyyy}", 1400, LtBlue));

        // 4 KPI cards
        long cardW = 1900000, cardH = 1400000, cardY = 900000, gap = 90000;
        long totalW = cardW * 4 + gap * 3;
        long startX = (SlideW - totalW) / 2;

        KpiCard(shapes, startX, cardY, cardW, cardH,
            payload.ResourceUtilization.Values.Sum().ToString(), "Team Members", Blue);
        KpiCard(shapes, startX + cardW + gap, cardY, cardW, cardH,
            payload.ApprovedAchievements.Count.ToString(), "Achievements This Month", Green);
        KpiCard(shapes, startX + (cardW + gap) * 2, cardY, cardW, cardH,
            payload.ApprovedSalesEnquiries.Count.ToString(), "Sales Enquiries Won", Orange);
        KpiCard(shapes, startX + (cardW + gap) * 3, cardY, cardW, cardH,
            payload.Events.Count.ToString(), "Events Held", Navy);

        // Summary row
        shapes.Append(TextBox(Margin, 2500000, ContentW, 280000,
            $"Engagements: {payload.Engagements.Count}  ·  Sales Sessions: {payload.SalesSessions.Count}  ·  Period: {report.StartDate:dd MMM} – {report.EndDate:dd MMM yyyy}",
            1400, Gray));

        SetSlide(sp, shapes);
    }

    private static void BuildResourceSlide(SlidePart sp, MonthlyReportSection payload)
    {
        var shapes = new ShapeTree(Nv(), GroupShapeProperties());
        SlideHeader(shapes, "Resource Utilization", "Active billing type breakdown for this month");

        long rowH = 380000, startY = 1050000, maxCount = payload.ResourceUtilization.Values.DefaultIfEmpty(1).Max();
        var colours = new[] { Blue, Green, Orange, Navy, Gray, "7C3AED" };
        int i = 0;

        foreach (var kv in payload.ResourceUtilization)
        {
            string col = colours[i % colours.Length];
            long barMaxW = 4800000;
            long barW    = maxCount == 0 ? 0 : barMaxW * kv.Value / maxCount;

            shapes.Append(TextBox(Margin, startY + i * (rowH + 60000), 1600000, rowH - 60000,
                kv.Key, 1600, Navy));
            shapes.Append(Rect(Margin + 1650000, startY + i * (rowH + 60000) + 60000,
                Math.Max(barW, 80000), rowH - 150000, col));
            shapes.Append(TextBox(Margin + 1650000 + Math.Max(barW, 80000) + 80000,
                startY + i * (rowH + 60000), 600000, rowH - 60000,
                kv.Value.ToString(), 1600, col, bold: true));
            i++;
        }

        if (!payload.ResourceUtilization.Any())
            shapes.Append(TextBox(Margin, 1400000, ContentW, 400000, "No resource allocation data for this period.", 1600, Gray));

        SetSlide(sp, shapes);
    }

    private static void BuildEngagementsSlide(SlidePart sp, MonthlyReportSection payload)
    {
        var shapes = new ShapeTree(Nv(), GroupShapeProperties());
        SlideHeader(shapes, "Client Engagements", $"{payload.Engagements.Count} engagement(s) this month");

        TableSlide(shapes, payload.Engagements.Take(8).Select(e => new[]
        {
            e.ClientName, e.ProjectName, e.NumberOfPositions.ToString(), TruncateStr(e.Details, 60)
        }).ToList(), ["Client", "Project", "Positions", "Details"]);

        SetSlide(sp, shapes);
    }

    private static void BuildAchievementsSlide(SlidePart sp, MonthlyReportSection payload)
    {
        var shapes = new ShapeTree(Nv(), GroupShapeProperties());
        SlideHeader(shapes, "Team Achievements", $"{payload.ApprovedAchievements.Count} approved this month");

        BulletList(shapes, payload.ApprovedAchievements.Take(10)
            .Select(a => $"[{a.Category}]  {a.Title}").ToList());

        SetSlide(sp, shapes);
    }

    private static void BuildSalesPipelineSlide(SlidePart sp, MonthlyReportSection payload)
    {
        var shapes = new ShapeTree(Nv(), GroupShapeProperties());
        SlideHeader(shapes, "Sales Pipeline", $"{payload.ApprovedSalesEnquiries.Count} approved enquiry(ies)");

        TableSlide(shapes, payload.ApprovedSalesEnquiries.Take(8).Select(s => new[]
        {
            s.ClientName, TruncateStr(s.Requirement, 50), s.Technology, s.Status
        }).ToList(), ["Client", "Requirement", "Technology", "Status"]);

        SetSlide(sp, shapes);
    }

    private static void BuildSalesSessionsSlide(SlidePart sp, MonthlyReportSection payload)
    {
        var shapes = new ShapeTree(Nv(), GroupShapeProperties());
        SlideHeader(shapes, "Sales Enablement Sessions", $"{payload.SalesSessions.Count} session(s) conducted");

        BulletList(shapes, payload.SalesSessions.Take(10)
            .Select(s => $"{s.SessionDate:dd MMM}  ·  {s.Title}").ToList());

        SetSlide(sp, shapes);
    }

    private static void BuildEventsSlide(SlidePart sp, MonthlyReportSection payload)
    {
        var shapes = new ShapeTree(Nv(), GroupShapeProperties());
        SlideHeader(shapes, "Team Events", $"{payload.Events.Count} event(s) this month");

        TableSlide(shapes, payload.Events.Take(8).Select(e => new[]
        {
            e.EventDate.ToString("dd MMM"), e.Title, e.Location, TruncateStr(e.Description, 55)
        }).ToList(), ["Date", "Event", "Location", "Description"]);

        SetSlide(sp, shapes);
    }

    private static void BuildKpiSlide(SlidePart sp, MonthlyReportSection payload, ReportRecord report)
    {
        var shapes = new ShapeTree(Nv(), GroupShapeProperties());
        shapes.Append(Rect(0, 0, SlideW, SlideH, OffWht));
        shapes.Append(Rect(0, 0, SlideW, 600000, Navy));
        shapes.Append(TextBox(Margin, 180000, ContentW, 320000, "Key Performance Indicators", 2800, White, bold: true));
        shapes.Append(TextBox(Margin, 440000, 3000000, 200000, $"{report.StartDate:MMMM yyyy}", 1400, LtBlue));

        long cardW = 2700000, cardH = 1200000, gap = 90000;
        long row1Y = 820000, row2Y = row1Y + cardH + gap;
        long col1X = Margin, col2X = Margin + cardW + gap, col3X = Margin + (cardW + gap) * 2;

        KpiCard(shapes, col1X, row1Y, cardW, cardH,
            payload.ResourceUtilization.Values.Sum().ToString(), "Total Headcount", Navy);
        KpiCard(shapes, col2X, row1Y, cardW, cardH,
            payload.ApprovedAchievements.Count.ToString(), "Achievements Approved", Green);
        KpiCard(shapes, col3X, row1Y, cardW, cardH,
            payload.Engagements.Select(e => e.NumberOfPositions).Sum().ToString(), "Open Positions", Blue);
        KpiCard(shapes, col1X, row2Y, cardW, cardH,
            payload.ApprovedSalesEnquiries.Count.ToString(), "Sales Enquiries Won", Orange);
        KpiCard(shapes, col2X, row2Y, cardW, cardH,
            payload.SalesSessions.Count.ToString(), "Sales Sessions", Blue);
        KpiCard(shapes, col3X, row2Y, cardW, cardH,
            payload.Events.Count.ToString(), "Team Events", Navy);

        SetSlide(sp, shapes);
    }

    private static void BuildClosingSlide(SlidePart sp, ReportRecord report)
    {
        var shapes = new ShapeTree(Nv(), GroupShapeProperties());
        shapes.Append(Rect(0, 0, SlideW, SlideH, Navy));
        shapes.Append(Rect(0, SlideH - 180000, SlideW, 180000, Blue));

        shapes.Append(TextBox(Margin, 1400000, ContentW, 700000,
            "Thank You", 5600, White, bold: true, align: "ctr"));
        shapes.Append(TextBox(Margin, 2200000, ContentW, 400000,
            "Microsoft Practice Team — Miracle", 2000, LtBlue, align: "ctr"));
        shapes.Append(TextBox(Margin, 2700000, ContentW, 300000,
            $"Report #{report.ReportRecordId}  ·  {report.StartDate:dd MMM} – {report.EndDate:dd MMM yyyy}",
            1400, Gray, align: "ctr"));
        shapes.Append(TextBox(Margin, 4600000, ContentW, 260000,
            "Powered by KudosApp", 1200, Gray, align: "ctr"));

        SetSlide(sp, shapes);
    }

    // ── Compound UI helpers ──────────────────────────────────────────────────

    private static void SlideHeader(ShapeTree shapes, string title, string subtitle)
    {
        shapes.Append(Rect(0, 0, SlideW, SlideH, OffWht));
        shapes.Append(Rect(0, 0, SlideW, 600000, Navy));
        shapes.Append(Rect(0, 590000, 1200000, 30000, Blue));
        shapes.Append(TextBox(Margin, 160000, ContentW, 320000, title, 2800, White, bold: true));
        shapes.Append(TextBox(Margin, 440000, ContentW, 200000, subtitle, 1400, LtBlue));
    }

    private static void KpiCard(ShapeTree shapes,
        long x, long y, long w, long h, string value, string label, string colour)
    {
        shapes.Append(Rect(x, y, w, h, White));
        shapes.Append(Rect(x, y, 18000, h, colour));  // left accent bar
        shapes.Append(TextBox(x + 60000, y + 120000, w - 80000, h / 2,
            value, 3600, colour, bold: true));
        shapes.Append(TextBox(x + 60000, y + h / 2 + 30000, w - 80000, h / 2 - 80000,
            label, 1200, Gray));
    }

    private static void TableSlide(ShapeTree shapes, List<string[]> rows, string[] headers)
    {
        long startY = 800000, rowH = 340000, colW = ContentW / headers.Length;

        // Header row
        for (int c = 0; c < headers.Length; c++)
        {
            shapes.Append(Rect(Margin + c * colW, startY, colW - 10000, rowH, Navy));
            shapes.Append(TextBox(Margin + c * colW + 40000, startY + 60000,
                colW - 60000, rowH - 80000, headers[c], 1400, White, bold: true));
        }

        // Data rows
        for (int r = 0; r < rows.Count; r++)
        {
            string bg = r % 2 == 0 ? White : LtBlue;
            long rowY = startY + (r + 1) * (rowH + 10000);
            for (int c = 0; c < headers.Length && c < rows[r].Length; c++)
            {
                shapes.Append(Rect(Margin + c * colW, rowY, colW - 10000, rowH, bg));
                shapes.Append(TextBox(Margin + c * colW + 40000, rowY + 60000,
                    colW - 60000, rowH - 80000, rows[r][c], 1300, Navy));
            }
        }

        if (rows.Count == 0)
            shapes.Append(TextBox(Margin, startY + rowH + 200000, ContentW, 400000,
                "No data for this period.", 1600, Gray));
    }

    private static void BulletList(ShapeTree shapes, List<string> items)
    {
        long startY = 820000, itemH = 380000;
        for (int i = 0; i < items.Count; i++)
        {
            long y = startY + i * (itemH + 30000);
            shapes.Append(Ellipse(Margin, y + 120000, 90000, 90000, Blue));
            shapes.Append(TextBox(Margin + 160000, y, ContentW - 160000, itemH, items[i], 1500, Navy));
        }
        if (items.Count == 0)
            shapes.Append(TextBox(Margin, startY, ContentW, 400000, "No items for this period.", 1600, Gray));
    }

    // ── Shape primitives ─────────────────────────────────────────────────────

    private static OpenXmlElement Rect(long x, long y, long w, long h,
        string hexColour, int opacity = 100)
    {
        var sp = new P.Shape();
        sp.Append(new P.NonVisualShapeProperties(
            new P.NonVisualDrawingProperties { Id = NextId(), Name = "Rect" },
            new P.NonVisualShapeDrawingProperties(),
            new ApplicationNonVisualDrawingProperties()));

        var spPr = new P.ShapeProperties();
        spPr.Append(Xfrm(x, y, w, h));
        spPr.Append(new PresetGeometry(new AdjustValueList()) { Preset = ShapeTypeValues.Rectangle });

        var fill = new SolidFill();
        var rgb  = new RgbColorModelHex { Val = hexColour };
        if (opacity < 100) rgb.Append(new Alpha { Val = opacity * 1000 });
        fill.Append(rgb);
        spPr.Append(fill);
        spPr.Append(new D.Outline(new NoFill()));
        sp.Append(spPr);
        sp.Append(new P.TextBody(new BodyProperties(), new ListStyle()));
        return sp;
    }

    private static OpenXmlElement Ellipse(long x, long y, long w, long h,
        string hexColour, int opacity = 100)
    {
        var sp = new P.Shape();
        sp.Append(new P.NonVisualShapeProperties(
            new P.NonVisualDrawingProperties { Id = NextId(), Name = "Ellipse" },
            new P.NonVisualShapeDrawingProperties(),
            new ApplicationNonVisualDrawingProperties()));

        var spPr = new P.ShapeProperties();
        spPr.Append(Xfrm(x, y, w, h));
        spPr.Append(new PresetGeometry(new AdjustValueList()) { Preset = ShapeTypeValues.Ellipse });

        var fill = new SolidFill();
        var rgb  = new RgbColorModelHex { Val = hexColour };
        if (opacity < 100) rgb.Append(new Alpha { Val = opacity * 1000 });
        fill.Append(rgb);
        spPr.Append(fill);
        spPr.Append(new D.Outline(new NoFill()));
        sp.Append(spPr);
        sp.Append(new P.TextBody(new BodyProperties(), new ListStyle()));
        return sp;
    }

    private static OpenXmlElement TextBox(long x, long y, long w, long h,
        string text, int size, string hexColour,
        bool bold = false, string align = "l")
    {
        var sp = new P.Shape();
        sp.Append(new P.NonVisualShapeProperties(
            new P.NonVisualDrawingProperties { Id = NextId(), Name = "Txt" },
            new P.NonVisualShapeDrawingProperties(new ShapeLocks { NoGrouping = true }),
            new ApplicationNonVisualDrawingProperties(new PlaceholderShape { Type = PlaceholderValues.Body })));

        var spPr = new P.ShapeProperties();
        spPr.Append(Xfrm(x, y, w, h));
        spPr.Append(new PresetGeometry(new AdjustValueList()) { Preset = ShapeTypeValues.Rectangle });
        spPr.Append(new NoFill());
        spPr.Append(new D.Outline(new NoFill()));
        sp.Append(spPr);

        var jc = align == "ctr" ? TextAlignmentTypeValues.Center
               : align == "r"   ? TextAlignmentTypeValues.Right
               : TextAlignmentTypeValues.Left;

        var para = new D.Paragraph(
            new D.ParagraphProperties { Alignment = jc },
            new D.Run(
                new D.RunProperties
                {
                    Language = "en-US",
                    FontSize = size,
                    Bold = bold,
                    Dirty = false
                },
                new D.Text(text)));

        var bp = new BodyProperties
        {
            LeftInset   = 0,
            TopInset    = 0,
            RightInset  = 0,
            BottomInset = 0,
            Anchor      = TextAnchoringTypeValues.Center
        };
        bp.Append(new NormalAutoFit());

        // Apply colour
        var rpr = para.Descendants<D.RunProperties>().First();
        rpr.Append(new D.SolidFill(new RgbColorModelHex { Val = hexColour }));

        sp.Append(new P.TextBody(bp, new ListStyle(), para));
        return sp;
    }

    // ── OpenXML structure helpers ────────────────────────────────────────────

    private static Transform2D Xfrm(long x, long y, long w, long h) =>
        new(new Offset { X = x, Y = y }, new Extents { Cx = w, Cy = h });

    private static P.NonVisualGroupShapeProperties Nv() =>
        new(new P.NonVisualDrawingProperties { Id = 1, Name = "" },
            new P.NonVisualGroupShapeDrawingProperties(),
            new ApplicationNonVisualDrawingProperties());

    private static GroupShapeProperties GroupShapeProperties() =>
        new(new TransformGroup(
            new Offset { X = 0, Y = 0 },
            new Extents { Cx = 0, Cy = 0 },
            new ChildOffset { X = 0, Y = 0 },
            new ChildExtents { Cx = 0, Cy = 0 }));

    private static void SetSlide(SlidePart sp, ShapeTree shapes)
    {
        sp.Slide = new P.Slide(
            new CommonSlideData(shapes),
            new ColorMapOverride(new MasterColorMapping()));
        sp.Slide.Save();
    }

    // Monotonically increasing shape ID per process — fine for single-threaded generation
    private static uint _shapeId = 2;
    private static uint NextId() => _shapeId++;

    private static string TruncateStr(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";

    // ── Minimal master / layout / theme ─────────────────────────────────────

    private static P.Presentation BuildPresentation() =>
        new(new SlideSize { Cx = (int)SlideW, Cy = (int)SlideH, Type = SlideSizeValues.Custom },
            new NotesSize { Cx = 6858000, Cy = 9144000 })
        { DefaultTextStyle = new DefaultTextStyle() };

    private static SlideMaster BuildMaster(SlideMasterPart masterPart, SlideLayoutPart layoutPart)
    {
        var master = new SlideMaster(
            new CommonSlideData(new ShapeTree(
                new P.NonVisualGroupShapeProperties(
                    new P.NonVisualDrawingProperties { Id = 1, Name = "" },
                    new P.NonVisualGroupShapeDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties()),
                new GroupShapeProperties(new TransformGroup(
                    new Offset { X = 0, Y = 0 }, new Extents { Cx = 0, Cy = 0 },
                    new ChildOffset { X = 0, Y = 0 }, new ChildExtents { Cx = 0, Cy = 0 })))),
            new P.ColorMap
            {
                Background1    = ColorSchemeIndexValues.Light1,
                Text1          = ColorSchemeIndexValues.Dark1,
                Background2    = ColorSchemeIndexValues.Light2,
                Text2          = ColorSchemeIndexValues.Dark2,
                Accent1        = ColorSchemeIndexValues.Accent1,
                Accent2        = ColorSchemeIndexValues.Accent2,
                Accent3        = ColorSchemeIndexValues.Accent3,
                Accent4        = ColorSchemeIndexValues.Accent4,
                Accent5        = ColorSchemeIndexValues.Accent5,
                Accent6        = ColorSchemeIndexValues.Accent6,
                Hyperlink      = ColorSchemeIndexValues.Hyperlink,
                FollowedHyperlink = ColorSchemeIndexValues.FollowedHyperlink
            },
            new SlideLayoutIdList(
                new SlideLayoutId { Id = 2147483649U, RelationshipId = masterPart.GetIdOfPart(layoutPart) }))
        { TextStyles = new TextStyles() };
        return master;
    }

    private static SlideLayout BuildLayout() =>
        new(new CommonSlideData(new ShapeTree(
            new P.NonVisualGroupShapeProperties(
                new P.NonVisualDrawingProperties { Id = 1, Name = "" },
                new P.NonVisualGroupShapeDrawingProperties(),
                new ApplicationNonVisualDrawingProperties()),
            new GroupShapeProperties(new TransformGroup(
                new Offset { X = 0, Y = 0 }, new Extents { Cx = 0, Cy = 0 },
                new ChildOffset { X = 0, Y = 0 }, new ChildExtents { Cx = 0, Cy = 0 })))),
        new ColorMapOverride(new MasterColorMapping()))
        { Type = SlideLayoutValues.Blank };

    private static D.Theme BuildTheme()
    {
        var theme = new D.Theme { Name = "KudosTheme" };
        theme.Append(new ThemeElements(
            new ColorScheme(
                new Dark1Color(new RgbColorModelHex { Val = "000000" }),
                new Light1Color(new RgbColorModelHex { Val = "FFFFFF" }),
                new Dark2Color(new RgbColorModelHex { Val = Navy }),
                new Light2Color(new RgbColorModelHex { Val = LtBlue }),
                new Accent1Color(new RgbColorModelHex { Val = Blue }),
                new Accent2Color(new RgbColorModelHex { Val = Green }),
                new Accent3Color(new RgbColorModelHex { Val = Orange }),
                new Accent4Color(new RgbColorModelHex { Val = "7C3AED" }),
                new Accent5Color(new RgbColorModelHex { Val = "0891B2" }),
                new Accent6Color(new RgbColorModelHex { Val = Gray }),
                new D.Hyperlink(new RgbColorModelHex { Val = Blue }))
            { Name = "KudosColors" },
            new FontScheme(
                new MajorFont(new LatinFont { Typeface = "Segoe UI" },
                    new EastAsianFont { Typeface = "" }, new ComplexScriptFont { Typeface = "" }),
                new MinorFont(new LatinFont { Typeface = "Segoe UI" },
                    new EastAsianFont { Typeface = "" }, new ComplexScriptFont { Typeface = "" }))
            { Name = "KudosFonts" },
            new FormatScheme(
                new FillStyleList(
                    new SolidFill(new SchemeColor { Val = SchemeColorValues.PhColor }),
                    new GradientFill(), new PatternFill()),
                new LineStyleList(new D.Outline(), new D.Outline(), new D.Outline()),
                new EffectStyleList(
                    new EffectStyle(new EffectList()),
                    new EffectStyle(new EffectList()),
                    new EffectStyle(new EffectList())),
                new BackgroundFillStyleList(
                    new SolidFill(new SchemeColor { Val = SchemeColorValues.PhColor }),
                    new GradientFill(), new PatternFill()))
            { Name = "KudosFormats" }));
        return theme;
    }
}
