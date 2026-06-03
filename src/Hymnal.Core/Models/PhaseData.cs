using System.Collections.Generic;

namespace Hymnal.Core.Models;

public sealed class PhaseData
{
    public ChapterStatus Status { get; init; }

    // ── Legacy single-phase fields (kept for backward-compat JSON round-trips) ──
    public string? PhaseStartDate { get; init; }
    public string? PhaseEndDate { get; init; }

    // ── Per-phase schedule (keys = phase names: Outlining/Drafting/Editing/Polishing/Reviewing) ──
    /// <summary>
    /// Independent schedule for each authoring phase.
    /// Keys are the phase name string (e.g. "Drafting"); values are nullable date/progress entries.
    /// Null (or absent key) means the author has not entered data for that phase.
    /// </summary>
    public Dictionary<string, PhaseScheduleEntry>? Schedule { get; init; }
}
