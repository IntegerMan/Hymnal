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

    public WorkspaceViewModel WorkspaceViewModel { get; }

    public MainWindowViewModel(WorkspaceViewModel workspaceViewModel)
    {
        WorkspaceViewModel = workspaceViewModel;
        _ = workspaceViewModel.InitAsync();
    }
}
