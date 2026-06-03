namespace Hymnal.Core.Models;

/// <summary>
/// Independently-tracked schedule for one authoring phase on a single chapter.
/// Stored as values in <see cref="PhaseData.Schedule"/> keyed by phase name.
/// All fields are optional — null means the author has not entered a value.
/// </summary>
public sealed record PhaseScheduleEntry
{
    /// <summary>ISO 8601 date string "yyyy-MM-dd", or null when not set.</summary>
    public string? StartDate { get; init; }

    /// <summary>ISO 8601 date string "yyyy-MM-dd", or null when not set.</summary>
    public string? EndDate { get; init; }

    /// <summary>Manual completion percentage 0–100. Null when not entered.</summary>
    public double? Progress { get; init; }
}
