using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Hymnal.Core.Interfaces;
using Hymnal.Infrastructure;
using ReactiveUI;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Hymnal.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public WorkspaceViewModel WorkspaceViewModel { get; }
    public EditorViewModel EditorViewModel { get; }
    public NotesViewModel NotesViewModel { get; }
    public ChapterInfoViewModel ChapterInfoViewModel { get; }
    public GanttViewModel GanttViewModel { get; }
    public CorkboardViewModel CorkboardViewModel { get; }

    private readonly IAppSettingsStore _settingsStore;

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

    // ── Centre-panel mode ─────────────────────────────────────────────────────

    private ShellMode _activeMode = ShellMode.Write;
    public ShellMode ActiveMode
    {
        get => _activeMode;
        private set
        {
            if (_activeMode == value)
                return;

            _activeMode = value;
            this.RaisePropertyChanged(nameof(ActiveMode));
            this.RaisePropertyChanged(nameof(IsEditorVisible));
            this.RaisePropertyChanged(nameof(IsGanttVisible));
            this.RaisePropertyChanged(nameof(IsCorkboardVisible));
        }
    }

    /// <summary>True when the writing/editor surface is the active centre-panel view.</summary>
    public bool IsEditorVisible => ActiveMode == ShellMode.Write;

    /// <summary>True when the corkboard/plan surface is the active centre-panel view.</summary>
    public bool IsCorkboardVisible => ActiveMode == ShellMode.Plan;

    /// <summary>True when the Gantt/manage surface is the active centre-panel view.</summary>
    public bool IsGanttVisible => ActiveMode == ShellMode.Manage;

    public ReactiveCommand<Unit, Unit> SelectResearchCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectPlanCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectWriteCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectManageCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleGanttCommand { get; }

    // ── Sidebar visibility ────────────────────────────────────────────────────

    private bool _isSidebarExpanded = true;
    public bool IsSidebarExpanded
    {
        get => _isSidebarExpanded;
        set => this.RaiseAndSetIfChanged(ref _isSidebarExpanded, value);
    }

    public ReactiveCommand<Unit, Unit> ToggleSidebarCommand { get; }

    // ── Right-rail pane aggregates ────────────────────────────────────────────

    private readonly ObservableAsPropertyHelper<bool> _isAnyRightPaneOpen;
    /// <summary>True when either the Notes or Chapter Info pane is visible.</summary>
    public bool IsAnyRightPaneOpen => _isAnyRightPaneOpen.Value;

    private readonly ObservableAsPropertyHelper<bool> _isBothRightPanesOpen;
    /// <summary>True when both the Notes and Chapter Info panes are visible.</summary>
    public bool IsBothRightPanesOpen => _isBothRightPanesOpen.Value;

    // ── Exit ─────────────────────────────────────────────────────────────────

    public ReactiveCommand<Unit, Unit> ExitCommand { get; } =
        ReactiveCommand.Create(() =>
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.Shutdown();
        });

    // ── About ─────────────────────────────────────────────────────────────────

    public ReactiveCommand<Unit, Unit> ShowAboutCommand { get; } =
        ReactiveCommand.Create(() => { /* handled in MainWindow code-behind */ });

    public MainWindowViewModel(
        WorkspaceViewModel workspaceViewModel,
        EditorViewModel editorViewModel,
        NotesViewModel notesViewModel,
        ChapterInfoViewModel chapterInfoViewModel,
        GanttViewModel ganttViewModel,
        CorkboardViewModel corkboardViewModel,
        NotificationService notificationService,
        IAppSettingsStore settingsStore)
    {
        WorkspaceViewModel = workspaceViewModel;
        EditorViewModel = editorViewModel;
        NotesViewModel = notesViewModel;
        ChapterInfoViewModel = chapterInfoViewModel;
        GanttViewModel = ganttViewModel;
        CorkboardViewModel = corkboardViewModel;
        _settingsStore = settingsStore;

        try
        {
            _isSidebarExpanded = _settingsStore.GetAsync<bool?>("sidebarExpanded").GetAwaiter().GetResult() ?? true;
        }
        catch
        {
            _isSidebarExpanded = true;
        }

        ToggleSidebarCommand = ReactiveCommand.Create(() =>
        {
            IsSidebarExpanded = !IsSidebarExpanded;
            _ = PersistSidebarExpandedAsync(IsSidebarExpanded);
        });

        SelectResearchCommand = ReactiveCommand.Create(() => { }, Observable.Return(false));
        SelectPlanCommand = ReactiveCommand.Create(() => { ActiveMode = ShellMode.Plan; });
        SelectWriteCommand = ReactiveCommand.Create(() => { ActiveMode = ShellMode.Write; });
        SelectManageCommand = ReactiveCommand.Create(() => { ActiveMode = ShellMode.Manage; });
        ToggleGanttCommand = ReactiveCommand.Create(() =>
        {
            ActiveMode = IsGanttVisible ? ShellMode.Write : ShellMode.Manage;
        });

        // ── Right-rail pane aggregates ────────────────────────────────────────
        _isAnyRightPaneOpen = Observable.CombineLatest(
                ChapterInfoViewModel.WhenAnyValue(x => x.IsVisible),
                NotesViewModel.WhenAnyValue(x => x.IsVisible),
                (a, b) => a || b)
            .ToProperty(this, x => x.IsAnyRightPaneOpen);
        Disposables.Add(_isAnyRightPaneOpen);

        _isBothRightPanesOpen = Observable.CombineLatest(
                ChapterInfoViewModel.WhenAnyValue(x => x.IsVisible),
                NotesViewModel.WhenAnyValue(x => x.IsVisible),
                (a, b) => a && b)
            .ToProperty(this, x => x.IsBothRightPanesOpen);
        Disposables.Add(_isBothRightPanesOpen);

        EditorViewModel.HasWorkspace = WorkspaceViewModel.HasWorkspace;
        Disposables.Add(
            CorkboardViewModel.OpenChapterRequested
                .Subscribe(chapter =>
                {
                    WorkspaceViewModel.SelectedNode = chapter;
                    ActiveMode = ShellMode.Write;
                }));

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

    private async Task PersistSidebarExpandedAsync(bool value)
    {
        try
        {
            await _settingsStore.SetAsync("sidebarExpanded", value).ConfigureAwait(false);
        }
        catch
        {
            // Non-fatal; layout preference may not persist across sessions if storage fails.
        }
    }
}
