using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Hymnal.Core.Models;
using Hymnal.ViewModels;

namespace Hymnal.Views;

/// <summary>
/// Read-only custom Gantt chart control.
/// Accepts a list of <see cref="GanttRowViewModel"/> rows and renders a time axis with
/// per-row phase segments (one coloured bar per authoring phase that has dates set).
/// Part rows render as section dividers with a rollup progress bar.
/// All drawing exceptions are swallowed silently so the view never crashes the host.
/// </summary>
public sealed class GanttCanvas : Control
{
    public sealed class GanttCellEditRequestedEventArgs : EventArgs
    {
        public GanttRowViewModel Row { get; }
        public GanttEditableColumn Column { get; }
        public Rect CellBounds { get; }

        public GanttCellEditRequestedEventArgs(
            GanttRowViewModel row,
            GanttEditableColumn column,
            Rect cellBounds)
        {
            Row = row;
            Column = column;
            CellBounds = cellBounds;
        }
    }

    public event EventHandler<GanttCellEditRequestedEventArgs>? CellEditRequested;

    public static readonly StyledProperty<bool> ShowTableProperty =
        AvaloniaProperty.Register<GanttCanvas, bool>(nameof(ShowTable), true);

    public bool ShowTable
    {
        get => GetValue(ShowTableProperty);
        set => SetValue(ShowTableProperty, value);
    }

    // ── Layout constants ──────────────────────────────────────────────────────

    private const double TitleColumnWidth    = 220.0;
    private const double StatusColumnWidth   = 100.0;
    private const double StartColumnWidth    = 96.0;
    private const double EndColumnWidth      = 96.0;
    private const double ProgressColumnWidth = 80.0;

    private const double StatusColumnLeft   = TitleColumnWidth;
    private const double StartColumnLeft    = StatusColumnLeft + StatusColumnWidth;
    private const double EndColumnLeft      = StartColumnLeft + StartColumnWidth;
    private const double ProgressColumnLeft = EndColumnLeft + EndColumnWidth;
    private const double TimelineLeft       = ProgressColumnLeft + ProgressColumnWidth;

    private const double HeaderHeight     = 36.0;
    private const double RowHeight        = 28.0;
    private const double BoxVPadding      = 5.0;
    private const double MinTimelineWidth = 1100.0;

    // ── Brushes / pens ────────────────────────────────────────────────────────

    // Axis / grid
    private static readonly IBrush BackgroundBrush  = new SolidColorBrush(Color.Parse("#0D0B22"));
    private static readonly IBrush HeaderBgBrush    = new SolidColorBrush(Color.Parse("#120932"));
    private static readonly IBrush LabelColumnBrush = new SolidColorBrush(Color.Parse("#0F0828"));
    private static readonly IBrush GridLineBrush    = new SolidColorBrush(Color.FromArgb(0x28, 0xFF, 0xFF, 0xFF));
    private static readonly IBrush AxisTextBrush    = new SolidColorBrush(Color.Parse("#94A3B8"));
    private static readonly IBrush LabelTextBrush   = new SolidColorBrush(Color.Parse("#CBD5E1"));
    private static readonly IBrush LabelDimBrush    = new SolidColorBrush(Color.Parse("#64748B"));
    private static readonly IBrush PartRowBgBrush   = new SolidColorBrush(Color.FromArgb(0x18, 0x9D, 0x4E, 0xDD));
    private static readonly IBrush EmptyStateBrush  = new SolidColorBrush(Color.Parse("#475569"));
    private static readonly Pen   SeparatorPen      = new(new SolidColorBrush(Color.FromArgb(0x50, 0x9D, 0x4E, 0xDD)), 1.0);
    private static readonly Pen   GridLinePen       = new(GridLineBrush, 0.5);

    // Per-phase fill colours — keyed by the canonical phase name string.
    private static readonly Dictionary<string, Color> PhaseBarColors = new()
    {
        ["Outlining"] = Color.Parse("#9589B0"),
        ["Drafting"]  = Color.Parse("#38BDF8"),
        ["Editing"]   = Color.Parse("#9D4EDD"),
        ["Polishing"] = Color.Parse("#F5C842"),
        ["Reviewing"] = Color.Parse("#E91E8C"),
    };

    // Part rollup colours
    private static readonly IBrush PartRollupTrackBrush = new SolidColorBrush(Color.FromArgb(0x60, 0x47, 0x55, 0x69));
    private static readonly IBrush PartRollupFillBrush  = new SolidColorBrush(Color.Parse("#34D399"));
    private static readonly Pen    PartRollupStrokePen  = new(new SolidColorBrush(Color.FromArgb(0xA0, 0x34, 0xD3, 0x99)), 1.0);

    // ── AvaloniaProperty: Rows ────────────────────────────────────────────────

    public static readonly StyledProperty<IReadOnlyList<GanttRowViewModel>?> RowsProperty =
        AvaloniaProperty.Register<GanttCanvas, IReadOnlyList<GanttRowViewModel>?>(
            nameof(Rows));

    public IReadOnlyList<GanttRowViewModel>? Rows
    {
        get => GetValue(RowsProperty);
        set => SetValue(RowsProperty, value);
    }

    private INotifyCollectionChanged? _trackedCollection;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == RowsProperty)
        {
            if (_trackedCollection != null)
                _trackedCollection.CollectionChanged -= OnRowsCollectionChanged;

            _trackedCollection = Rows as INotifyCollectionChanged;
            if (_trackedCollection != null)
                _trackedCollection.CollectionChanged += OnRowsCollectionChanged;

            InvalidateVisual();
        }
    }

    private void OnRowsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => InvalidateVisual();

    // ── Pointer hit-test ──────────────────────────────────────────────────────

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        try
        {
            if (!ShowTable)
                return;

            var rows = Rows;
            if (rows == null || rows.Count == 0) return;

            var pos = e.GetPosition(this);
            if (pos.Y < HeaderHeight) return;

            int rowIndex = (int)((pos.Y - HeaderHeight) / RowHeight);
            if (rowIndex < 0 || rowIndex >= rows.Count) return;

            var row = rows[rowIndex];
            var editColumn = GetEditableColumnAtX(pos.X);
            bool clickedEditableTableCell = editColumn != GanttEditableColumn.None;

            if (row.IsChapter && !row.IsBook && clickedEditableTableCell)
            {
                row.PendingEditColumn = editColumn;
                var cellRect = GetCellBounds(rowIndex, editColumn);
                CellEditRequested?.Invoke(this, new GanttCellEditRequestedEventArgs(row, editColumn, cellRect));
                row.EditDatesCommand.Execute().Subscribe();
            }
        }
        catch { /* swallow */ }
    }

    // ── MeasureOverride ───────────────────────────────────────────────────────

    protected override Size MeasureOverride(Size availableSize)
    {
        int rowCount = Rows?.Count ?? 0;
        double desiredHeight = HeaderHeight + rowCount * RowHeight;

        double minWidth = ShowTable ? TimelineLeft + MinTimelineWidth : MinTimelineWidth;
        double width = double.IsFinite(availableSize.Width) && availableSize.Width > 0
            ? Math.Max(availableSize.Width, minWidth)
            : minWidth;

        double height = double.IsFinite(desiredHeight) && desiredHeight > 0
            ? desiredHeight
            : HeaderHeight;

        return new Size(width, height);
    }

    // ── Render ────────────────────────────────────────────────────────────────

    public override void Render(DrawingContext ctx)
    {
        try
        {
            var rows = Rows;
            bool showTable = ShowTable;
            double w = Bounds.Width;
            double logicalWidth = showTable ? w : (w + TimelineLeft);
            double h = Bounds.Height;

            // ── Background ────────────────────────────────────────────────────
            ctx.FillRectangle(BackgroundBrush, new Rect(0, 0, w, h));
            if (showTable)
                ctx.FillRectangle(LabelColumnBrush, new Rect(0, 0, TimelineLeft, h));
            ctx.FillRectangle(HeaderBgBrush,    new Rect(0, 0, w, HeaderHeight));

            IDisposable? transformScope = null;
            if (!showTable)
                transformScope = ctx.PushTransform(Matrix.CreateTranslation(-TimelineLeft, 0));

            if (showTable)
                DrawColumnHeaders(ctx);

            if (rows == null || rows.Count == 0)
            {
                transformScope?.Dispose();
                DrawEmptyState(ctx, w, h, "No chapters loaded.");
                return;
            }

            // ── Compute date range across all phase segments ──────────────────
            var allSegments = rows
                .Where(r => r.IsChapter)
                .SelectMany(r => r.PhaseSegments)
                .ToList();

            var allDates = allSegments
                .SelectMany(s => new[] { s.StartDate, s.EndDate })
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .ToList();

            // Fall back: also check row-level StartDate/EndDate (part rows).
            var rowDates = rows
                .SelectMany(r => new[] { r.StartDate, r.EndDate })
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .ToList();

            var combinedDates = allDates.Concat(rowDates).ToList();

            bool hasDateRange = combinedDates.Count > 0;

            DateOnly axisStart = hasDateRange
                ? combinedDates.Min()
                : DateOnly.FromDateTime(DateTime.Today);

            DateOnly axisEnd = hasDateRange
                ? combinedDates.Max()
                : DateOnly.FromDateTime(DateTime.Today.AddMonths(6));

            // Expand axis: start at the beginning of the month, end two months past.
            axisStart = new DateOnly(axisStart.Year, axisStart.Month, 1);
            axisEnd   = new DateOnly(axisEnd.Year, axisEnd.Month, 1).AddMonths(2);

            double chartWidth = Math.Max(0, logicalWidth - TimelineLeft);

            // ── Draw time axis ────────────────────────────────────────────────
            DrawTimeAxis(ctx, axisStart, axisEnd, chartWidth, logicalWidth, h, showTable);

            for (int i = 0; i < rows.Count; i++)
            {
                double rowY = HeaderHeight + i * RowHeight;
                DrawRow(ctx, rows[i], i, rowY, axisStart, axisEnd, chartWidth);
            }

            transformScope?.Dispose();
        }
        catch
        {
            // Swallow all rendering exceptions — must never crash the host.
        }
    }

    private static void DrawColumnHeaders(DrawingContext ctx)
    {
        try
        {
            DrawHeaderText(ctx, "CHAPTER", 8);
            DrawHeaderText(ctx, "STATUS", StatusColumnLeft + 6);
            DrawHeaderText(ctx, "START", StartColumnLeft + 6);
            DrawHeaderText(ctx, "END", EndColumnLeft + 6);
            DrawHeaderText(ctx, "PROGRESS", ProgressColumnLeft + 6);

            ctx.DrawLine(SeparatorPen, new Point(TitleColumnWidth, 0), new Point(TitleColumnWidth, HeaderHeight));
            ctx.DrawLine(SeparatorPen, new Point(StatusColumnLeft + StatusColumnWidth, 0), new Point(StatusColumnLeft + StatusColumnWidth, HeaderHeight));
            ctx.DrawLine(SeparatorPen, new Point(StartColumnLeft + StartColumnWidth, 0), new Point(StartColumnLeft + StartColumnWidth, HeaderHeight));
            ctx.DrawLine(SeparatorPen, new Point(EndColumnLeft + EndColumnWidth, 0), new Point(EndColumnLeft + EndColumnWidth, HeaderHeight));
            ctx.DrawLine(SeparatorPen, new Point(ProgressColumnLeft + ProgressColumnWidth, 0), new Point(ProgressColumnLeft + ProgressColumnWidth, HeaderHeight));
        }
        catch { /* swallow */ }
    }

    private static void DrawHeaderText(DrawingContext ctx, string text, double x)
    {
        var ft = MakeFormattedText(text, 9.0, AxisTextBrush);
        ctx.DrawText(ft, new Point(x, (HeaderHeight - ft.Height) / 2));
    }

    private static void DrawTimeAxis(
        DrawingContext ctx,
        DateOnly start, DateOnly end,
        double chartWidth, double totalWidth, double totalHeight, bool showTable)
    {
        try
        {
            var current = start;
            while (current <= end)
            {
                double x = TimelineLeft + DateToX(current, start, end, chartWidth);

                ctx.DrawLine(GridLinePen, new Point(x, HeaderHeight), new Point(x, totalHeight));

                // Month label lives only in the header band, starting 3 px past the grid line.
                string label = current.ToString("MMM yy");
                var ft = MakeFormattedText(label, 9.0, AxisTextBrush);
                double labelY = (HeaderHeight - ft.Height) / 2;
                ctx.DrawText(ft, new Point(x + 3, labelY));

                current = current.AddMonths(1);
            }

            if (showTable)
            {
                // Vertical separators: table | timeline.
                ctx.DrawLine(SeparatorPen, new Point(TitleColumnWidth, 0), new Point(TitleColumnWidth, totalHeight));
                ctx.DrawLine(SeparatorPen, new Point(StatusColumnLeft + StatusColumnWidth, 0), new Point(StatusColumnLeft + StatusColumnWidth, totalHeight));
                ctx.DrawLine(SeparatorPen, new Point(StartColumnLeft + StartColumnWidth, 0), new Point(StartColumnLeft + StartColumnWidth, totalHeight));
                ctx.DrawLine(SeparatorPen, new Point(EndColumnLeft + EndColumnWidth, 0), new Point(EndColumnLeft + EndColumnWidth, totalHeight));
                ctx.DrawLine(SeparatorPen, new Point(ProgressColumnLeft + ProgressColumnWidth, 0), new Point(ProgressColumnLeft + ProgressColumnWidth, totalHeight));
            }
            ctx.DrawLine(SeparatorPen, new Point(TimelineLeft,     0), new Point(TimelineLeft,     totalHeight));
        }
        catch { /* swallow */ }
    }

    private static void DrawRow(
        DrawingContext ctx,
        GanttRowViewModel row,
        int rowIndex,
        double rowY,
        DateOnly axisStart, DateOnly axisEnd,
        double chartWidth)
    {
        try
        {
            bool isEven = rowIndex % 2 == 0;
            if (isEven)
            {
                ctx.FillRectangle(
                    new SolidColorBrush(Color.FromArgb(0x0A, 0xFF, 0xFF, 0xFF)),
                    new Rect(TimelineLeft, rowY, chartWidth, RowHeight));
            }

            if (row.IsBook)
            {
                DrawBookRow(ctx, row, rowY, axisStart, axisEnd, chartWidth);
                return;
            }

            if (row.IsPart)
            {
                DrawPartRow(ctx, row, rowY, axisStart, axisEnd, chartWidth);
                return;
            }

            // ── Title column ──────────────────────────────────────────────────
            bool hasAnyDate = row.PhaseSegments.Any(s => s.StartDate.HasValue || s.EndDate.HasValue);
            var labelBrush  = hasAnyDate ? LabelTextBrush : LabelDimBrush;
            var labelFt     = MakeFormattedText(
                TruncateLabel(row.Title, TitleColumnWidth - 16),
                11.5, labelBrush);
            ctx.DrawText(labelFt, new Point(8, rowY + (RowHeight - labelFt.Height) / 2));

            // ── Table cells (status/start/end/progress) ─────────────────────
            var currentSeg = row.PhaseSegments.FirstOrDefault(s =>
                string.Equals(s.PhaseName, row.Status.ToString(), StringComparison.Ordinal));

            var startText = currentSeg?.StartDate?.ToString("yyyy-MM-dd")
                ?? row.StartDate?.ToString("yyyy-MM-dd")
                ?? "--";
            var endText = currentSeg?.EndDate?.ToString("yyyy-MM-dd")
                ?? row.EndDate?.ToString("yyyy-MM-dd")
                ?? "--";

            bool hasProgress = currentSeg != null && currentSeg.Progress > 0;
            var progressText = hasProgress
                ? $"{Math.Round(currentSeg!.Progress * 100.0):0}%"
                : "--";

            var statusBrush = new SolidColorBrush(StatusToColor(row.Status));
            DrawCellText(ctx, row.Status.ToString(), StatusColumnLeft, StatusColumnWidth, rowY, 10.5, statusBrush);
            DrawCellText(ctx, startText, StartColumnLeft, StartColumnWidth, rowY, 10.0, labelBrush);
            DrawCellText(ctx, endText, EndColumnLeft, EndColumnWidth, rowY, 10.0, labelBrush);
            DrawCellText(ctx, progressText, ProgressColumnLeft, ProgressColumnWidth, rowY, 10.0, labelBrush);

            // ── Phase segments on the timeline ────────────────────────────────
            foreach (var seg in row.PhaseSegments)
            {
                if (!seg.StartDate.HasValue && !seg.EndDate.HasValue)
                    continue;  // phase not scheduled — skip

                if (!PhaseBarColors.TryGetValue(seg.PhaseName, out var barColor))
                    barColor = Color.Parse("#64748B");

                // If only one date is set, draw a thin marker line instead of a bar.
                if (!seg.StartDate.HasValue || !seg.EndDate.HasValue)
                {
                    var markerDate = seg.StartDate ?? seg.EndDate!.Value;
                    double mx = TimelineLeft + DateToX(markerDate, axisStart, axisEnd, chartWidth);
                    ctx.DrawLine(
                        new Pen(new SolidColorBrush(barColor), 1.5),
                        new Point(mx, rowY + BoxVPadding),
                        new Point(mx, rowY + RowHeight - BoxVPadding));
                    continue;
                }

                double boxLeft  = TimelineLeft + DateToX(seg.StartDate.Value, axisStart, axisEnd, chartWidth);
                double boxRight = TimelineLeft + DateToX(seg.EndDate.Value,   axisStart, axisEnd, chartWidth);
                double boxW     = Math.Max(boxRight - boxLeft, 3.0);
                double boxH     = RowHeight - BoxVPadding * 2;
                double boxTop   = rowY + BoxVPadding;

                var fillBrush   = new SolidColorBrush(Color.FromArgb(0xCC, barColor.R, barColor.G, barColor.B));
                var strokeBrush = new SolidColorBrush(barColor);

                // Progress fill clipped inside the bar.
                if (seg.Progress > 0 && boxW > 4)
                {
                    var trackRect = new Rect(boxLeft, boxTop, boxW, boxH);
                    ctx.DrawRectangle(fillBrush, new Pen(strokeBrush, 1.0), trackRect, 3, 3);

                    double fillW = Math.Clamp(seg.Progress * boxW, 0, boxW);
                    var brightFill = new SolidColorBrush(Color.FromArgb(0xFF, barColor.R, barColor.G, barColor.B));
                    using (ctx.PushClip(trackRect))
                    {
                        ctx.FillRectangle(brightFill, new Rect(boxLeft, boxTop, fillW, boxH));
                    }
                }
                else
                {
                    ctx.DrawRectangle(fillBrush, new Pen(strokeBrush, 1.0),
                        new Rect(boxLeft, boxTop, boxW, boxH), 3, 3);
                }

                // Phase name inside bar when wide enough.
                if (boxW > 40)
                {
                    var labelText = seg.PhaseName.Length > 4 ? seg.PhaseName[..4] : seg.PhaseName;
                    var segFt = MakeFormattedText(labelText, 8.5, new SolidColorBrush(Colors.White));
                    if (segFt.Width + 4 < boxW)
                    {
                        ctx.DrawText(
                            segFt,
                            new Point(boxLeft + (boxW - segFt.Width) / 2,
                                      boxTop  + (boxH  - segFt.Height) / 2));
                    }
                }
            }
        }
        catch { /* swallow */ }
    }

    private static void DrawBookRow(
        DrawingContext ctx,
        GanttRowViewModel row,
        double rowY,
        DateOnly axisStart, DateOnly axisEnd,
        double chartWidth)
    {
        try
        {
            // Prominent background strip.
            var bookBgBrush = new SolidColorBrush(Color.FromArgb(0x28, 0x9D, 0x4E, 0xDD));
            ctx.FillRectangle(bookBgBrush,
                new Rect(0, rowY, TimelineLeft + chartWidth, RowHeight));

            ctx.DrawLine(SeparatorPen,
                new Point(0, rowY),
                new Point(TimelineLeft + chartWidth, rowY));
            ctx.DrawLine(SeparatorPen,
                new Point(0, rowY + RowHeight),
                new Point(TimelineLeft + chartWidth, rowY + RowHeight));

            var ft = MakeFormattedText("BOOK", 10.0,
                new SolidColorBrush(Color.Parse("#C4B5FD")));
            ctx.DrawText(ft, new Point(8, rowY + (RowHeight - ft.Height) / 2));

            DrawCellText(ctx, "Rollup", StatusColumnLeft, StatusColumnWidth, rowY, 10.0, LabelTextBrush);
            DrawCellText(ctx, row.StartDate?.ToString("yyyy-MM-dd") ?? "--", StartColumnLeft, StartColumnWidth, rowY, 10.0, LabelTextBrush);
            DrawCellText(ctx, row.EndDate?.ToString("yyyy-MM-dd") ?? "--", EndColumnLeft, EndColumnWidth, rowY, 10.0, LabelTextBrush);
            DrawCellText(ctx, $"{Math.Round(row.CompletionPercentage * 100.0):0}%", ProgressColumnLeft, ProgressColumnWidth, rowY, 10.0, LabelTextBrush);

            // Rollup bar — same as part row.
            if (!row.IsMissingDates && row.StartDate.HasValue && row.EndDate.HasValue)
            {
                double boxLeft  = TimelineLeft + DateToX(row.StartDate.Value, axisStart, axisEnd, chartWidth);
                double boxRight = TimelineLeft + DateToX(row.EndDate.Value,   axisStart, axisEnd, chartWidth);
                double boxW     = Math.Max(boxRight - boxLeft, 4.0);
                double boxH     = RowHeight - BoxVPadding * 2;
                double boxTop   = rowY + BoxVPadding;

                var trackRect = new Rect(boxLeft, boxTop, boxW, boxH);
                ctx.DrawRectangle(PartRollupTrackBrush, PartRollupStrokePen, trackRect, 3, 3);

                double fillW = Math.Max(Math.Min(row.CompletionPercentage * boxW, boxW), 0.0);
                if (fillW > 1.0)
                {
                    using (ctx.PushClip(trackRect))
                    {
                        ctx.FillRectangle(PartRollupFillBrush, new Rect(boxLeft, boxTop, fillW, boxH));
                    }
                }

                if (boxW > 36)
                {
                    string pctLabel = $"{(int)Math.Round(row.CompletionPercentage * 100)}%";
                    var pctFt = MakeFormattedText(pctLabel, 8.5, new SolidColorBrush(Colors.White));
                    if (pctFt.Width + 4 < boxW)
                    {
                        ctx.DrawText(pctFt,
                            new Point(boxLeft + (boxW - pctFt.Width) / 2,
                                      boxTop  + (boxH  - pctFt.Height) / 2));
                    }
                }
            }
        }
        catch { /* swallow */ }
    }

    private static void DrawPartRow(
        DrawingContext ctx,
        GanttRowViewModel row,
        double rowY,
        DateOnly axisStart, DateOnly axisEnd,
        double chartWidth)
    {
        try
        {
            ctx.FillRectangle(PartRowBgBrush,
                new Rect(0, rowY, TimelineLeft + chartWidth, RowHeight));

            ctx.DrawLine(SeparatorPen,
                new Point(0, rowY),
                new Point(TimelineLeft + chartWidth, rowY));

            var ft = MakeFormattedText(row.Title.ToUpperInvariant(), 9.5, LabelDimBrush);
            ctx.DrawText(ft, new Point(8, rowY + (RowHeight - ft.Height) / 2));

            DrawCellText(ctx, "Rollup", StatusColumnLeft, StatusColumnWidth, rowY, 10.0, LabelDimBrush);
            DrawCellText(ctx, row.StartDate?.ToString("yyyy-MM-dd") ?? "--", StartColumnLeft, StartColumnWidth, rowY, 10.0, LabelDimBrush);
            DrawCellText(ctx, row.EndDate?.ToString("yyyy-MM-dd") ?? "--", EndColumnLeft, EndColumnWidth, rowY, 10.0, LabelDimBrush);
            DrawCellText(ctx, $"{Math.Round(row.CompletionPercentage * 100.0):0}%", ProgressColumnLeft, ProgressColumnWidth, rowY, 10.0, LabelDimBrush);

            // Rollup span across all child dates.
            if (!row.IsMissingDates && row.StartDate.HasValue && row.EndDate.HasValue)
            {
                double boxLeft  = TimelineLeft + DateToX(row.StartDate.Value, axisStart, axisEnd, chartWidth);
                double boxRight = TimelineLeft + DateToX(row.EndDate.Value,   axisStart, axisEnd, chartWidth);
                double boxW     = Math.Max(boxRight - boxLeft, 4.0);
                double boxH     = RowHeight - BoxVPadding * 2;
                double boxTop   = rowY + BoxVPadding;

                var trackRect = new Rect(boxLeft, boxTop, boxW, boxH);
                ctx.DrawRectangle(PartRollupTrackBrush, PartRollupStrokePen, trackRect, 3, 3);

                double fillW = Math.Max(Math.Min(row.CompletionPercentage * boxW, boxW), 0.0);
                if (fillW > 1.0)
                {
                    using (ctx.PushClip(trackRect))
                    {
                        ctx.FillRectangle(PartRollupFillBrush, new Rect(boxLeft, boxTop, fillW, boxH));
                    }
                }

                if (boxW > 36)
                {
                    string pctLabel = $"{(int)Math.Round(row.CompletionPercentage * 100)}%";
                    var pctFt = MakeFormattedText(pctLabel, 8.5, new SolidColorBrush(Colors.White));
                    if (pctFt.Width + 4 < boxW)
                    {
                        ctx.DrawText(pctFt,
                            new Point(boxLeft + (boxW - pctFt.Width) / 2,
                                      boxTop  + (boxH  - pctFt.Height) / 2));
                    }
                }
            }
        }
        catch { /* swallow */ }
    }

    private static void DrawEmptyState(DrawingContext ctx, double w, double h, string message)
    {
        try
        {
            var ft = MakeFormattedText(message, 13.0, EmptyStateBrush);
            ctx.DrawText(ft, new Point((w - ft.Width) / 2, h / 2 - ft.Height / 2));
        }
        catch { /* swallow */ }
    }

    // ── Utility helpers ───────────────────────────────────────────────────────

    private static double DateToX(DateOnly date, DateOnly rangeStart, DateOnly rangeEnd, double chartWidth)
    {
        int totalDays = rangeEnd.DayNumber - rangeStart.DayNumber;
        if (totalDays <= 0) return 0;
        int elapsed = date.DayNumber - rangeStart.DayNumber;
        return Math.Clamp((double)elapsed / totalDays * chartWidth, 0, chartWidth);
    }

    private static GanttEditableColumn GetEditableColumnAtX(double x)
    {
        if (x >= StatusColumnLeft && x < StatusColumnLeft + StatusColumnWidth)
            return GanttEditableColumn.Status;
        if (x >= StartColumnLeft && x < StartColumnLeft + StartColumnWidth)
            return GanttEditableColumn.StartDate;
        if (x >= EndColumnLeft && x < EndColumnLeft + EndColumnWidth)
            return GanttEditableColumn.EndDate;
        if (x >= ProgressColumnLeft && x < ProgressColumnLeft + ProgressColumnWidth)
            return GanttEditableColumn.Progress;
        return GanttEditableColumn.None;
    }

    private static Rect GetCellBounds(int rowIndex, GanttEditableColumn column)
    {
        var rowY = HeaderHeight + rowIndex * RowHeight;
        return column switch
        {
            GanttEditableColumn.Status => new Rect(StatusColumnLeft, rowY, StatusColumnWidth, RowHeight),
            GanttEditableColumn.StartDate => new Rect(StartColumnLeft, rowY, StartColumnWidth, RowHeight),
            GanttEditableColumn.EndDate => new Rect(EndColumnLeft, rowY, EndColumnWidth, RowHeight),
            GanttEditableColumn.Progress => new Rect(ProgressColumnLeft, rowY, ProgressColumnWidth, RowHeight),
            _ => new Rect(0, rowY, 0, RowHeight)
        };
    }

    private static Color StatusToColor(ChapterStatus status)
    {
        return status switch
        {
            ChapterStatus.Planned => Color.Parse("#FF6B35"),
            ChapterStatus.Outlining => Color.Parse("#9589B0"),
            ChapterStatus.Drafting => Color.Parse("#38BDF8"),
            ChapterStatus.Editing => Color.Parse("#9D4EDD"),
            ChapterStatus.Polishing => Color.Parse("#F5C842"),
            ChapterStatus.Reviewing => Color.Parse("#E91E8C"),
            ChapterStatus.Done => Color.Parse("#22D3A0"),
            _ => Color.Parse("#CBD5E1")
        };
    }

    private static FormattedText MakeFormattedText(string text, double size, IBrush brush)
    {
        return new FormattedText(
            text,
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Inter, Segoe UI, sans-serif"),
            size,
            brush);
    }

    private static void DrawCellText(
        DrawingContext ctx,
        string text,
        double cellX,
        double cellW,
        double rowY,
        double fontSize,
        IBrush brush)
    {
        var ft = MakeFormattedText(TruncateLabel(text, cellW - 12), fontSize, brush);
        ctx.DrawText(ft, new Point(cellX + 6, rowY + (RowHeight - ft.Height) / 2));
    }

    private static string TruncateLabel(string label, double maxWidth)
    {
        int maxChars = Math.Max(1, (int)(maxWidth / 6.5));
        return label.Length <= maxChars ? label : label[..Math.Max(1, maxChars - 3)] + "...";
    }
}
