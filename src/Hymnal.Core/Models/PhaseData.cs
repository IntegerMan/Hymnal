namespace Hymnal.Core.Models;

public sealed class PhaseData
{
    public ChapterStatus Status { get; init; }
    public string? PhaseStartDate { get; init; }
    public string? PhaseEndDate { get; init; }
}
