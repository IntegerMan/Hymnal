using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using ReactiveUI;
using System.Reactive;

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

    public ReactiveCommand<Unit, Unit> ExitCommand { get; } =
        ReactiveCommand.Create(() =>
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.Shutdown();
        });

    public MainWindowViewModel(WorkspaceViewModel workspaceViewModel)
    {
        WorkspaceViewModel = workspaceViewModel;
        _ = workspaceViewModel.InitAsync();
    }
}
