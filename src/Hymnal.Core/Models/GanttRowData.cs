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
    /// <summary>True when either StartDate or EndDate could not be parsed.</summary>
    bool IsMissingDates
);
