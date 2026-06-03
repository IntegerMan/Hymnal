using ReactiveUI;

namespace Hymnal.ViewModels;

/// <summary>
/// Represents one editable row in the per-phase schedule table in ChapterInfoView.
/// Each instance covers one authoring phase (e.g. "Drafting").
/// </summary>
public sealed class PhaseScheduleRowViewModel : ReactiveObject
{
    /// <summary>The phase name, e.g. "Outlining" or "Drafting". Immutable.</summary>
    public string PhaseName { get; }

    private string? _startDate;
    /// <summary>ISO 8601 date string "yyyy-MM-dd", or null. Bound two-way to a TextBox.</summary>
    public string? StartDate
    {
        get => _startDate;
        set => this.RaiseAndSetIfChanged(ref _startDate, value);
    }

    private string? _endDate;
    /// <summary>ISO 8601 date string "yyyy-MM-dd", or null. Bound two-way to a TextBox.</summary>
    public string? EndDate
    {
        get => _endDate;
        set => this.RaiseAndSetIfChanged(ref _endDate, value);
    }

    private double? _progress;
    /// <summary>Completion percentage 0–100, or null when not entered.</summary>
    public double? Progress
    {
        get => _progress;
        set => this.RaiseAndSetIfChanged(ref _progress, value);
    }

    public PhaseScheduleRowViewModel(string phaseName)
    {
        PhaseName = phaseName;
    }
}
