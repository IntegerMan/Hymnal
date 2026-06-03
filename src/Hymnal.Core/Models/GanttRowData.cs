using System.Collections.Generic;

namespace Hymnal.Core.Models;

/// <summary>
/// Parsed, immutable projection of a single chapter's Gantt row.
/// Produced by <see cref="Hymnal.Core.Services.GanttProjection"/>.
/// Part nodes (NodeKind.Part) are included so the view can render section dividers.
/// </summary>
public sealed record GanttRowData(
    string RelativePath,
    string Title,
    NodeKind Kind,
    ChapterStatus Status,
    DateOnly? StartDate,
    DateOnly? EndDate,
    /// <summary>True when no phase has any date set.</summary>
    bool IsMissingDates,
    /// <summary>
    /// Fractional completion in the range [0.0, 1.0].
    /// For Part rollup rows: fraction of child chapters whose status is <see cref="ChapterStatus.Done"/>.
    /// For Chapter rows: 0.0 (chapter-level progress is not tracked here).
    /// </summary>
    double CompletionPercentage = 0.0,
    /// <summary>
    /// Per-phase segments for rendering multiple colored bars on the Gantt timeline.
    /// Null for Part rollup rows.
    /// </summary>
    IReadOnlyList<PhaseSegment>? PhaseSegments = null,
    /// <summary>
    /// True for the synthetic top-level book summary row that aggregates all chapters.
    /// When true the row renders with a more prominent style.
    /// </summary>
    bool IsBook = false
);
