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
/// per-row phase boxes. Rows with missing dates are rendered in a muted style rather than
/// causing any error. Part rows render as section dividers. All drawing exceptions are
/// swallowed silently so the view never crashes the host.
/// </summary>
public sealed class GanttCanvas : Control
{
    // ── Layout constants ──────────────────────────────────────────────────────

    private const double LabelColumnWidth  = 180.0;
    private const double HeaderHeight      = 36.0;
    private const double RowHeight         = 26.0;
    private const double BoxVPadding       = 4.0;
    private const double MinCanvasWidth    = 400.0;

    // ── Brushes / pens ────────────────────────────────────────────────────────

    // Axis / grid
    private static readonly IBrush BackgroundBrush   = new SolidColorBrush(Color.Parse("#0D0B22"));
    private static readonly IBrush HeaderBgBrush     = new SolidColorBrush(Color.Parse("#120932"));
    private static readonly IBrush LabelColumnBrush  = new SolidColorBrush(Color.Parse("#0F0828"));
    private static readonly IBrush GridLineBrush     = new SolidColorBrush(Color.FromArgb(0x28, 0xFF, 0xFF, 0xFF));
    private static readonly IBrush AxisTextBrush     = new SolidColorBrush(Color.Parse("#94A3B8"));
    private static readonly IBrush LabelTextBrush    = new SolidColorBrush(Color.Parse("#CBD5E1"));
    private static readonly IBrush LabelDimBrush     = new SolidColorBrush(Color.Parse("#64748B"));
    private static readonly IBrush PartRowBgBrush    = new SolidColorBrush(Color.FromArgb(0x18, 0x9D, 0x4E, 0xDD));
    private static readonly IBrush MissingBoxBrush   = new SolidColorBrush(Color.FromArgb(0x40, 0x64, 0x74, 0x8B));
    private static readonly IBrush EmptyStateBrush   = new SolidColorBrush(Color.Parse("#475569"));
    private static readonly Pen   SeparatorPen       = new(new SolidColorBrush(Color.FromArgb(0x50, 0x9D, 0x4E, 0xDD)), 1.0);
    private static readonly Pen   GridLinePen        = new(GridLineBrush, 0.5);

    // Phase-specific fill colours (synthwave-compatible palette)
    private static readonly Dictionary<ChapterStatus, Color> PhaseColors = new()
    {
        [ChapterStatus.Planned]   = Color.Parse("#6B7280"),
        [ChapterStatus.Outlining] = Color.Parse("#9D4EDD"),
        [ChapterStatus.Drafting]  = Color.Parse("#38BDF8"),
        [ChapterStatus.Editing]   = Color.Parse("#E91E8C"),
        [ChapterStatus.Polishing] = Color.Parse("#F59E0B"),
        [ChapterStatus.Reviewing] = Color.Parse("#10B981"),
        [ChapterStatus.Done]      = Color.Parse("#34D399"),
    };

    // Part rollup colours
    private static readonly IBrush PartRollupTrackBrush  = new SolidColorBrush(Color.FromArgb(0x60, 0x47, 0x55, 0x69)); // muted slate
    private static readonly IBrush PartRollupFillBrush   = new SolidColorBrush(Color.Parse("#34D399"));                  // Done green
    private static readonly Pen    PartRollupStrokePen   = new(new SolidColorBrush(Color.FromArgb(0xA0, 0x34, 0xD3, 0x99)), 1.0);

    // ── AvaloniaProperty: Rows ────────────────────────────────────────────────

    public static readonly StyledProperty<IReadOnlyList<GanttRowViewModel>?> RowsProperty =
        AvaloniaProperty.Register<GanttCanvas, IReadOnlyList<GanttRowViewModel>?>(
            nameof(Rows));

    public IReadOnlyList<GanttRowViewModel>? Rows
    {
        get => GetValue(RowsProperty);
        set => SetValue(RowsProperty, value);
    }

    // Invalidate visual whenever the rows collection reference or content changes.
    private INotifyCollectionChanged? _trackedCollection;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == RowsProperty)
        {
            // Unsubscribe from the previous collection.
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

    /// <summary>
    /// Hit-tests the pointer position against Gantt rows. When the click lands on a
    /// chapter row (not the header, not a Part row), the row's
    /// <see cref="GanttRowViewModel.EditDatesCommand"/> is executed so
    /// <see cref="GanttViewModel.RowEditRequested"/> emits and the date-picker can open.
    /// All exceptions are swallowed to keep the canvas crash-free.
    /// </summary>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        try
        {
            var rows = Rows;
            if (rows == null || rows.Count == 0) return;

            var pos = e.GetPosition(this);
            if (pos.Y < HeaderHeight) return; // click inside the header — ignore

            int rowIndex = (int)((pos.Y - HeaderHeight) / RowHeight);
            if (rowIndex < 0 || rowIndex >= rows.Count) return;

            var row = rows[rowIndex];
            if (row.IsChapter)
                row.EditDatesCommand.Execute().Subscribe();
        }
        catch { /* swallow — canvas must never crash the host */ }
    }

    // ── MeasureOverride ───────────────────────────────────────────────────────

    protected override Size MeasureOverride(Size availableSize)
    {
        int rowCount = Rows?.Count ?? 0;
        double height = HeaderHeight + rowCount * RowHeight;
        double width  = Math.Max(availableSize.Width, MinCanvasWidth);
        return new Size(width, height);
    }

    // ── Render ────────────────────────────────────────────────────────────────

    public override void Render(DrawingContext ctx)
    {
        try
        {
            var rows  = Rows;
            double w  = Bounds.Width;
            double h  = Bounds.Height;

            // ── Background ────────────────────────────────────────────────────
            ctx.FillRectangle(BackgroundBrush, new Rect(0, 0, w, h));
            ctx.FillRectangle(LabelColumnBrush, new Rect(0, 0, LabelColumnWidth, h));
            ctx.FillRectangle(HeaderBgBrush,    new Rect(0, 0, w, HeaderHeight));

            if (rows == null || rows.Count == 0)
            {
                DrawEmptyState(ctx, w, h, "No chapters loaded.");
                return;
            }

            // ── Compute date range ────────────────────────────────────────────
            var validRows = rows.Where(r => r.IsChapter && !r.IsMissingDates &&
                                            r.StartDate.HasValue && r.EndDate.HasValue).ToList();

            bool hasDateRange = validRows.Count > 0;

            DateOnly axisStart = hasDateRange
                ? validRows.Min(r => r.StartDate!.Value)
                : DateOnly.FromDateTime(DateTime.Today);

            DateOnly axisEnd = hasDateRange
                ? validRows.Max(r => r.EndDate!.Value)
                : DateOnly.FromDateTime(DateTime.Today.AddMonths(6));

            // Expand axis by one month on each side for breathing room.
            axisStart = new DateOnly(axisStart.Year, axisStart.Month, 1);
            axisEnd   = new DateOnly(axisEnd.Year, axisEnd.Month, 1).AddMonths(2);

            double chartWidth = w - LabelColumnWidth;

            // ── Draw time axis ────────────────────────────────────────────────
            DrawTimeAxis(ctx, axisStart, axisEnd, chartWidth, w, h);

            // If no valid dates exist, still render muted rows against the fallback axis.
            for (int i = 0; i < rows.Count; i++)
            {
                double rowY = HeaderHeight + i * RowHeight;
                DrawRow(ctx, rows[i], i, rowY, axisStart, axisEnd, chartWidth);
            }
        }
        catch
        {
            // Swallow all rendering exceptions — must never crash the host.
        }
    }

    // ── Private drawing helpers ───────────────────────────────────────────────

    private static void DrawTimeAxis(
        DrawingContext ctx,
        DateOnly start, DateOnly end,
        double chartWidth, double totalWidth, double totalHeight)
    {
        try
        {
            // Iterate months between start and end.
            var current = start;
            while (current <= end)
            {
                double x = LabelColumnWidth + DateToX(current, start, end, chartWidth);

                // Vertical grid line through the full chart.
                ctx.DrawLine(GridLinePen, new Point(x, HeaderHeight), new Point(x, totalHeight));

                // Month label.
                string label = current.ToString("MMM yy");
                var ft = MakeFormattedText(label, 9.5, AxisTextBrush);
                ctx.DrawText(ft, new Point(x + 3, (HeaderHeight - ft.Height) / 2));

                // Advance one month.
                current = current.AddMonths(1);
            }

            // Vertical separator between label column and chart area.
            ctx.DrawLine(SeparatorPen, new Point(LabelColumnWidth, 0), new Point(LabelColumnWidth, totalHeight));
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

            // Alternating row background in chart area.
            if (isEven)
            {
                ctx.FillRectangle(
                    new SolidColorBrush(Color.FromArgb(0x0A, 0xFF, 0xFF, 0xFF)),
                    new Rect(LabelColumnWidth, rowY, chartWidth, RowHeight));
            }

            if (row.IsPart)
            {
                DrawPartRow(ctx, row, rowY, axisStart, axisEnd, chartWidth);
                return;
            }

            // ── Label ─────────────────────────────────────────────────────────
            var labelBrush = row.IsMissingDates ? LabelDimBrush : LabelTextBrush;
            var labelFt    = MakeFormattedText(
                TruncateLabel(row.Title, LabelColumnWidth - 16),
                11.5, labelBrush);
            ctx.DrawText(labelFt, new Point(8, rowY + (RowHeight - labelFt.Height) / 2));

            // ── Phase box ─────────────────────────────────────────────────────
            if (row.IsMissingDates || !row.StartDate.HasValue || !row.EndDate.HasValue)
            {
                // Dashed placeholder spanning the full chart width.
                DrawMissingBox(ctx, rowY, chartWidth);
                return;
            }

            double boxLeft  = LabelColumnWidth + DateToX(row.StartDate.Value, axisStart, axisEnd, chartWidth);
            double boxRight = LabelColumnWidth + DateToX(row.EndDate.Value,   axisStart, axisEnd, chartWidth);
            double boxW     = Math.Max(boxRight - boxLeft, 4.0);
            double boxH     = RowHeight - BoxVPadding * 2;

            var phaseColor  = PhaseColors.TryGetValue(row.Status, out var pc) ? pc : Color.Parse("#6B7280");
            var fillBrush   = new SolidColorBrush(Color.FromArgb(0xCC, phaseColor.R, phaseColor.G, phaseColor.B));
            var strokeBrush = new SolidColorBrush(phaseColor);

            ctx.DrawRectangle(
                fillBrush,
                new Pen(strokeBrush, 1.0),
                new Rect(boxLeft, rowY + BoxVPadding, boxW, boxH),
                3, 3);

            // Status text inside the box (only when wide enough).
            if (boxW > 40)
            {
                string statusLabel = row.Status.ToString();
                var statusFt = MakeFormattedText(statusLabel, 9.0, new SolidColorBrush(Colors.White));
                if (statusFt.Width + 6 < boxW)
                {
                    ctx.DrawText(
                        statusFt,
                        new Point(boxLeft + (boxW - statusFt.Width) / 2,
                                  rowY + BoxVPadding + (boxH - statusFt.Height) / 2));
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
            // Muted full-width background.
            ctx.FillRectangle(PartRowBgBrush,
                new Rect(0, rowY, LabelColumnWidth + chartWidth, RowHeight));

            // Horizontal divider line at the top of the row.
            ctx.DrawLine(SeparatorPen,
                new Point(0, rowY),
                new Point(LabelColumnWidth + chartWidth, rowY));

            // Part title (left label column).
            var ft = MakeFormattedText(row.Title.ToUpperInvariant(), 9.5, LabelDimBrush);
            ctx.DrawText(ft, new Point(8, rowY + (RowHeight - ft.Height) / 2));

            // ── Rollup span box + progress fill ───────────────────────────────
            // Only draw when the Part row has valid aggregated dates from its children.
            if (!row.IsMissingDates && row.StartDate.HasValue && row.EndDate.HasValue)
            {
                double boxLeft  = LabelColumnWidth + DateToX(row.StartDate.Value, axisStart, axisEnd, chartWidth);
                double boxRight = LabelColumnWidth + DateToX(row.EndDate.Value,   axisStart, axisEnd, chartWidth);
                double boxW     = Math.Max(boxRight - boxLeft, 4.0);
                double boxH     = RowHeight - BoxVPadding * 2;
                double boxTop   = rowY + BoxVPadding;

                var trackRect = new Rect(boxLeft, boxTop, boxW, boxH);

                // Track (empty background of the span).
                ctx.DrawRectangle(PartRollupTrackBrush, PartRollupStrokePen, trackRect, 3, 3);

                // Progress fill — clipped to the track width.
                double fillW = Math.Max(Math.Min(row.CompletionPercentage * boxW, boxW), 0.0);
                if (fillW > 1.0)
                {
                    using (ctx.PushClip(trackRect))
                    {
                        ctx.FillRectangle(
                            PartRollupFillBrush,
                            new Rect(boxLeft, boxTop, fillW, boxH));
                    }
                }

                // Percentage label inside the box when wide enough to fit.
                if (boxW > 36)
                {
                    string pctLabel = $"{(int)Math.Round(row.CompletionPercentage * 100)}%";
                    var pctFt = MakeFormattedText(pctLabel, 8.5, new SolidColorBrush(Colors.White));
                    if (pctFt.Width + 4 < boxW)
                    {
                        ctx.DrawText(
                            pctFt,
                            new Point(boxLeft + (boxW - pctFt.Width) / 2,
                                      boxTop  + (boxH  - pctFt.Height) / 2));
                    }
                }
            }
        }
        catch { /* swallow */ }
    }

    private static void DrawMissingBox(DrawingContext ctx, double rowY, double chartWidth)
    {
        try
        {
            double boxH = RowHeight - BoxVPadding * 2;
            ctx.FillRectangle(
                MissingBoxBrush,
                new Rect(LabelColumnWidth + 4, rowY + BoxVPadding, chartWidth - 8, boxH));
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

    /// <summary>Converts a date to an X pixel offset within the chart area.</summary>
    private static double DateToX(DateOnly date, DateOnly rangeStart, DateOnly rangeEnd, double chartWidth)
    {
        int totalDays = rangeEnd.DayNumber - rangeStart.DayNumber;
        if (totalDays <= 0) return 0;
        int elapsed = date.DayNumber - rangeStart.DayNumber;
        return Math.Clamp((double)elapsed / totalDays * chartWidth, 0, chartWidth);
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

    /// <summary>
    /// Truncates <paramref name="label"/> so its rendered width stays within
    /// <paramref name="maxWidth"/> pixels (approximate, character-based).
    /// </summary>
    private static string TruncateLabel(string label, double maxWidth)
    {
        // Rough: average char ~6.5px at 11.5pt. Real ellipsis via FormattedText would need
        // iterative measurement; this heuristic is sufficient for the read-only view.
        int maxChars = Math.Max(1, (int)(maxWidth / 6.5));
        return label.Length <= maxChars ? label : label[..(maxChars - 1)] + "…";
    }
}
