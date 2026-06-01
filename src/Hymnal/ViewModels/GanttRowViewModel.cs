using System;
using Hymnal.Core.Models;

namespace Hymnal.ViewModels;

/// <summary>
/// Represents a single row in the read-only Gantt chart.
/// Wraps an immutable <see cref="GanttRowData"/> produced by
/// <see cref="Hymnal.Core.Services.GanttProjection.Project"/>.
/// Part rows are included so the view can render section dividers inline.
/// </summary>
public sealed class GanttRowViewModel : ViewModelBase
{
    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>Forward-slash-normalized path relative to manuscript root.</summary>
    public string RelativePath { get; }

    /// <summary>Chapter or Part title shown in the row label column.</summary>
    public string Title { get; }

    /// <summary>Whether this row represents a Part (section divider) or a Chapter.</summary>
    public NodeKind Kind { get; }

    // ── Status / dates ────────────────────────────────────────────────────────

    /// <summary>Current authoring phase for this chapter.</summary>
    public ChapterStatus Status { get; }

    /// <summary>Parsed phase start date, or null when the value is absent/unparseable.</summary>
    public DateOnly? StartDate { get; }

    /// <summary>Parsed phase end date, or null when the value is absent/unparseable.</summary>
    public DateOnly? EndDate { get; }

    /// <summary>True when either StartDate or EndDate is null — row renders in a muted style.</summary>
    public bool IsMissingDates { get; }

    /// <summary>
    /// Fractional completion in the range [0.0, 1.0].
    /// For Part rollup rows this reflects child chapter completion (fraction with status Done).
    /// For Chapter rows this is always 0.0 (chapter-level progress is not tracked here).
    /// </summary>
    public double CompletionPercentage { get; }

    // ── Convenience ───────────────────────────────────────────────────────────

    public bool IsChapter => Kind == NodeKind.Chapter;
    public bool IsPart    => Kind == NodeKind.Part;

    // ── Constructor ───────────────────────────────────────────────────────────

    public GanttRowViewModel(GanttRowData data)
    {
        RelativePath       = data.RelativePath;
        Title              = data.Title;
        Kind               = data.Kind;
        Status             = data.Status;
        StartDate          = data.StartDate;
        EndDate            = data.EndDate;
        IsMissingDates     = data.IsMissingDates;
        CompletionPercentage = data.CompletionPercentage;
    }
}
