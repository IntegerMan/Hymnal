namespace Hymnal.Core.Models;

/// <summary>
/// Immutable snapshot of one authoring phase's timeline position for a single chapter.
/// Produced by <see cref="Hymnal.Core.Services.GanttProjection"/>; consumed by
/// <see cref="Hymnal.Views.GanttCanvas"/> for rendering.
/// </summary>
/// <param name="PhaseName">Phase name, e.g. "Outlining" or "Drafting".</param>
/// <param name="StartDate">Parsed start date, or null when absent/unparseable.</param>
/// <param name="EndDate">Parsed end date, or null when absent/unparseable.</param>
/// <param name="Progress">Fractional completion 0.0–1.0. Zero when unset.</param>
public sealed record PhaseSegment(
    string PhaseName,
    DateOnly? StartDate,
    DateOnly? EndDate,
    double Progress);
