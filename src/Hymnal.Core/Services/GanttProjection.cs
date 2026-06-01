using Hymnal.Core.Models;

namespace Hymnal.Core.Services;

/// <summary>
/// Pure projection helper: maps a <see cref="ChapterNode"/> + optional <see cref="PhaseData"/>
/// into a <see cref="GanttRowData"/> suitable for Gantt rendering.
/// Stateless and side-effect free — safe to call from any thread.
/// </summary>
public static class GanttProjection
{
    /// <summary>
    /// Project a chapter node and its phase metadata into a Gantt row.
    /// If <paramref name="phase"/> is null, <see cref="PhaseDataService.DefaultPhaseData"/> is used.
    /// Date parse failures produce a null date and set <see cref="GanttRowData.IsMissingDates"/>=true.
    /// </summary>
    public static GanttRowData Project(ChapterNode node, PhaseData? phase)
    {
        var pd = phase ?? PhaseDataService.DefaultPhaseData;
        var start = ParseDate(pd.PhaseStartDate);
        var end   = ParseDate(pd.PhaseEndDate);
        var isMissing = start is null || end is null;

        return new GanttRowData(
            RelativePath: node.RelativePath,
            Title:        node.Title,
            Kind:         node.Kind,
            Status:       pd.Status,
            StartDate:    start,
            EndDate:      end,
            IsMissingDates: isMissing);
    }

    /// <summary>
    /// Parse an ISO 8601 date string (yyyy-MM-dd) into a <see cref="DateOnly"/>.
    /// Returns null for null/whitespace/invalid input — never throws.
    /// </summary>
    public static DateOnly? ParseDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        return DateOnly.TryParseExact(raw, "yyyy-MM-dd", out var d) ? d : null;
    }
}
