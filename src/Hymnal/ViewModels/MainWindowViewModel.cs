using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Hymnal.Infrastructure;
using ReactiveUI;
using System;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Hymnal.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public WorkspaceViewModel WorkspaceViewModel { get; }
    public EditorViewModel EditorViewModel { get; }
    public NotesViewModel NotesViewModel { get; }

    // ── Window title ──────────────────────────────────────────────────────────

    private string _title = "Hymnal";
    public string Title
    {
        get => _title;
        private set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    // ── Notification banner ───────────────────────────────────────────────────

    private bool _hasBanner;
    public bool HasBanner
    {
        get => _hasBanner;
        set => this.RaiseAndSetIfChanged(ref _hasBanner, value);
    }

    private string? _bannerTitle = "Notice";
    public string? BannerTitle
    {
        get => _bannerTitle;
        set => this.RaiseAndSetIfChanged(ref _bannerTitle, value);
    }

    private string? _bannerMessage;
    public string? BannerMessage
    {
        get => _bannerMessage;
        set => this.RaiseAndSetIfChanged(ref _bannerMessage, value);
    }

    private Infrastructure.NotificationKind _bannerKind;
    public Infrastructure.NotificationKind BannerKind
    {
        get => _bannerKind;
        set => this.RaiseAndSetIfChanged(ref _bannerKind, value);
    }

    // ── Sidebar visibility ────────────────────────────────────────────────────

    private bool _isSidebarExpanded = true;
    public bool IsSidebarExpanded
    {
        get => _isSidebarExpanded;
        set => this.RaiseAndSetIfChanged(ref _isSidebarExpanded, value);
    }

    public ReactiveCommand<Unit, Unit> ToggleSidebarCommand { get; }

    // ── Exit ─────────────────────────────────────────────────────────────────

    public ReactiveCommand<Unit, Unit> ExitCommand { get; } =
        ReactiveCommand.Create(() =>
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.Shutdown();
        });

    public MainWindowViewModel(
        WorkspaceViewModel workspaceViewModel,
        EditorViewModel editorViewModel,
        NotesViewModel notesViewModel,
        NotificationService notificationService)
    {
        WorkspaceViewModel = workspaceViewModel;
        EditorViewModel = editorViewModel;
        NotesViewModel = notesViewModel;
        ToggleSidebarCommand = ReactiveCommand.Create(() => { IsSidebarExpanded = !IsSidebarExpanded; });

        EditorViewModel.HasWorkspace = WorkspaceViewModel.HasWorkspace;
        Disposables.Add(
            WorkspaceViewModel.WhenAnyValue(x => x.HasWorkspace)
                .Subscribe(hasWorkspace => EditorViewModel.HasWorkspace = hasWorkspace));

        // ── Reactive window title ────────────────────────────────────────────
        // Format: "Hymnal", "Hymnal - StoryTitle", "Hymnal - StoryTitle - file.md", or with " *" when dirty.
        Disposables.Add(
            Observable.CombineLatest(
                editorViewModel.WhenAnyValue(x => x.IsDirty),
                editorViewModel.WhenAnyValue(x => x.ActiveNode),
                workspaceViewModel.WhenAnyValue(x => x.WorkspaceName),
                (dirty, node, workspaceName) =>
                {
                    if (string.IsNullOrEmpty(workspaceName))
                        return "Hymnal";

                    if (node == null)
                        return $"Hymnal \u2014 {workspaceName}";

                    var fileName = Path.GetFileName(node.RelativePath);
                    return dirty
                        ? $"Hymnal \u2014 {workspaceName} \u2014 {fileName} *"
                        : $"Hymnal \u2014 {workspaceName} \u2014 {fileName}";
                })
            .Subscribe(t => Title = t));

        // ── Notification banner with 5-second auto-dismiss ───────────────────
        // SerialDisposable ensures only the latest timer is active; a new notification resets the clock.
        var timerDisposable = new SerialDisposable();
        Disposables.Add(timerDisposable);

        Disposables.Add(
            notificationService.Notifications
                .Subscribe(n =>
                {
                    HasBanner = true;
                    BannerKind = n.Kind;
                    BannerTitle = n.Kind switch
                    {
                        Hymnal.Infrastructure.NotificationKind.Error => "Error",
                        Hymnal.Infrastructure.NotificationKind.Success => "Success",
                        Hymnal.Infrastructure.NotificationKind.Info => "Info",
                        _ => "Notice"
                    };
                    BannerMessage = n.Message;

                    timerDisposable.Disposable = Observable
                        .Timer(TimeSpan.FromSeconds(5))
                        .Subscribe(_ => HasBanner = false);
                }));

        // Start workspace init (loads last workspace + restores last chapter).
        _ = workspaceViewModel.InitAsync();
    }
}
