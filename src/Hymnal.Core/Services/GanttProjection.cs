using System;
using System.Collections.Generic;
using System.Linq;
using Hymnal.Core.Models;

namespace Hymnal.Core.Services;

/// <summary>
/// Pure projection helper: maps a <see cref="ChapterNode"/> + optional <see cref="PhaseData"/>
/// into a <see cref="GanttRowData"/> suitable for Gantt rendering.
/// Stateless and side-effect free — safe to call from any thread.
/// </summary>
public static class GanttProjection
{
    /// <summary>Canonical ordered authoring phases shown as columns in the Gantt.</summary>
    public static readonly string[] PhaseNames =
        ["Outlining", "Drafting", "Editing", "Polishing", "Reviewing"];

    /// <summary>
    /// Project a chapter node and its phase metadata into a Gantt row.
    /// If <paramref name="phase"/> is null, <see cref="PhaseDataService.DefaultPhaseData"/> is used.
    /// </summary>
    public static GanttRowData Project(ChapterNode node, PhaseData? phase)
    {
        var pd = phase ?? PhaseDataService.DefaultPhaseData;

        // Build per-phase segments from Schedule; fall back to legacy fields for the active phase.
        var segments = BuildSegments(pd);

        // Overall start/end span across all phases for rollup purposes.
        var allStarts = segments.Where(s => s.StartDate.HasValue).Select(s => s.StartDate!.Value).ToList();
        var allEnds   = segments.Where(s => s.EndDate.HasValue).Select(s => s.EndDate!.Value).ToList();

        DateOnly? overallStart = allStarts.Count > 0 ? allStarts.Min() : null;
        DateOnly? overallEnd   = allEnds.Count   > 0 ? allEnds.Max()   : null;

        bool isMissing = overallStart is null && overallEnd is null;

        return new GanttRowData(
            RelativePath: node.RelativePath,
            Title:        node.Title,
            Kind:         node.Kind,
            Status:       pd.Status,
            StartDate:    overallStart,
            EndDate:      overallEnd,
            IsMissingDates: isMissing,
            PhaseSegments:  segments);
    }

    /// <summary>
    /// Builds the ordered list of <see cref="PhaseSegment"/> objects for a chapter.
    /// Schedule entries take priority; the legacy <see cref="PhaseData.PhaseStartDate"/> /
    /// <see cref="PhaseData.PhaseEndDate"/> fields are used as a fallback for the active status phase.
    /// </summary>
    public static IReadOnlyList<PhaseSegment> BuildSegments(PhaseData pd)
    {
        var segments = new List<PhaseSegment>(PhaseNames.Length);
        foreach (var name in PhaseNames)
        {
            PhaseScheduleEntry? entry = null;
            pd.Schedule?.TryGetValue(name, out entry);

            string? startRaw = entry?.StartDate;
            string? endRaw   = entry?.EndDate;

            // Fall back to legacy start/end for the current status phase.
            if (startRaw == null && endRaw == null && pd.Status.ToString() == name)
            {
                startRaw = pd.PhaseStartDate;
                endRaw   = pd.PhaseEndDate;
            }

            double progress = entry?.Progress.HasValue == true
                ? Math.Clamp(entry.Progress.Value / 100.0, 0.0, 1.0)
                : 0.0;

            segments.Add(new PhaseSegment(
                PhaseName: name,
                StartDate: ParseDate(startRaw),
                EndDate:   ParseDate(endRaw),
                Progress:  progress));
        }
        return segments;
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
