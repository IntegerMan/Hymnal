using ReactiveUI;

namespace Hymnal.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private string _title = "Hymnal";
    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    /// <summary>Current routed view — null in S01, populated from S02 onward.</summary>
    public object? CurrentView { get; set; }
}
