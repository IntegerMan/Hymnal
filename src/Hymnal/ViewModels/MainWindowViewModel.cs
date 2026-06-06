using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Hymnal.Core.Models;
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
    public ResearchViewModel ResearchViewModel { get; }
    public SupplementalDocsViewModel SupplementalDocsViewModel { get; }
    public GitPanelViewModel GitPanelViewModel { get; }

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
            EditorViewModel.IsResearchSurface = value == ShellMode.Research;
            this.RaisePropertyChanged(nameof(ActiveMode));
            this.RaisePropertyChanged(nameof(IsEditorVisible));
            this.RaisePropertyChanged(nameof(IsGanttVisible));
            this.RaisePropertyChanged(nameof(IsCorkboardVisible));
            this.RaisePropertyChanged(nameof(IsResearchVisible));
        }
    }

    /// <summary>True when the writing/editor surface is the active centre-panel view.</summary>
    public bool IsEditorVisible => ActiveMode == ShellMode.Write;

    /// <summary>True when the corkboard/plan surface is the active centre-panel view.</summary>
    public bool IsCorkboardVisible => ActiveMode == ShellMode.Plan;

    /// <summary>True when the Gantt/manage surface is the active centre-panel view.</summary>
    public bool IsGanttVisible => ActiveMode == ShellMode.Manage;

    /// <summary>True when the research surface is the active centre-panel view.</summary>
    public bool IsResearchVisible => ActiveMode == ShellMode.Research;

    public ReactiveCommand<Unit, Unit> SelectResearchCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectPlanCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectWriteCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectManageCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleGanttCommand { get; }

    // ── Left-rail pane aggregates ─────────────────────────────────────────────

    private readonly ObservableAsPropertyHelper<bool> _isAnyLeftPaneOpen;
    /// <summary>True when either the Chapters or Docs content area is visible.</summary>
    public bool IsAnyLeftPaneOpen => _isAnyLeftPaneOpen.Value;

    private readonly ObservableAsPropertyHelper<bool> _isBothLeftPanesOpen;
    /// <summary>True when both the Chapters and Docs content areas are visible.</summary>
    public bool IsBothLeftPanesOpen => _isBothLeftPanesOpen.Value;

    // ── Sidebar visibility (Ctrl+B) ───────────────────────────────────────────

    public ReactiveCommand<Unit, Unit> ToggleSidebarCommand { get; }

    // ── Right-rail pane aggregates ────────────────────────────────────────────

    private readonly ObservableAsPropertyHelper<bool> _isAnyRightPaneOpen;
    /// <summary>True when either the Notes or Chapter Info pane is visible.</summary>
    public bool IsAnyRightPaneOpen => _isAnyRightPaneOpen.Value;

    private readonly ObservableAsPropertyHelper<bool> _isBothRightPanesOpen;
    /// <summary>True when both the Notes and Chapter Info panes are visible.</summary>
    public bool IsBothRightPanesOpen => _isBothRightPanesOpen.Value;

    private readonly ObservableAsPropertyHelper<bool> _isEditorChapterBarVisible;
    /// <summary>True when the editor chapter metadata bar should be shown.</summary>
    public bool IsEditorChapterBarVisible => _isEditorChapterBarVisible.Value;

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
        ResearchViewModel researchViewModel,
        SupplementalDocsViewModel supplementalDocsViewModel,
        GitPanelViewModel gitPanelViewModel,
        NotificationService notificationService)
    {
        WorkspaceViewModel = workspaceViewModel;
        EditorViewModel = editorViewModel;
        NotesViewModel = notesViewModel;
        ChapterInfoViewModel = chapterInfoViewModel;
        GanttViewModel = ganttViewModel;
        CorkboardViewModel = corkboardViewModel;
        ResearchViewModel = researchViewModel;
        SupplementalDocsViewModel = supplementalDocsViewModel;
        GitPanelViewModel = gitPanelViewModel;

        _isAnyLeftPaneOpen = Observable.CombineLatest(
                workspaceViewModel.WhenAnyValue(x => x.IsChaptersPaneVisible),
                supplementalDocsViewModel.WhenAnyValue(x => x.IsVisible),
                (chapters, docs) => chapters || docs)
            .ToProperty(this, x => x.IsAnyLeftPaneOpen);
        Disposables.Add(_isAnyLeftPaneOpen);

        _isBothLeftPanesOpen = Observable.CombineLatest(
                workspaceViewModel.WhenAnyValue(x => x.IsChaptersPaneVisible),
                supplementalDocsViewModel.WhenAnyValue(x => x.IsVisible),
                (chapters, docs) => chapters && docs)
            .ToProperty(this, x => x.IsBothLeftPanesOpen);
        Disposables.Add(_isBothLeftPanesOpen);

        ToggleSidebarCommand = ReactiveCommand.Create(() =>
        {
            if (IsAnyLeftPaneOpen)
            {
                workspaceViewModel.IsChaptersPaneVisible = false;
                supplementalDocsViewModel.IsVisible = false;
            }
            else
            {
                workspaceViewModel.IsChaptersPaneVisible = true;
            }
        });

        SelectResearchCommand = ReactiveCommand.CreateFromTask(EnterResearchModeAsync);
        Disposables.Add(SelectResearchCommand.ThrownExceptions.Subscribe(ex =>
            notificationService.ShowError($"Failed to enter research mode: {ex.Message}")));
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

        _isEditorChapterBarVisible = Observable.CombineLatest(
                this.WhenAnyValue(x => x.ActiveMode),
                workspaceViewModel.WhenAnyValue(x => x.SelectedNode),
                editorViewModel.WhenAnyValue(x => x.ShowBookTxtWarning),
                (mode, selected, bookTxt) =>
                    mode == ShellMode.Write
                    && selected?.Node.Kind == NodeKind.Chapter
                    && !bookTxt)
            .ToProperty(this, x => x.IsEditorChapterBarVisible);
        Disposables.Add(_isEditorChapterBarVisible);

        EditorViewModel.HasWorkspace = WorkspaceViewModel.HasWorkspace;
        Disposables.Add(
            CorkboardViewModel.OpenChapterRequested
                .Subscribe(chapter =>
                {
                    WorkspaceViewModel.SelectedNode = chapter;
                    ActiveMode = ShellMode.Write;
                }));

        Disposables.Add(
            SupplementalDocsViewModel.DocumentOpened
                .Subscribe(_ =>
                {
                    if (ActiveMode != ShellMode.Research)
                        ActiveMode = ShellMode.Write;
                }));

        // ── Reactive window title ────────────────────────────────────────────
        // Format: "Hymnal", "Hymnal - Workspace", "Hymnal - Workspace - file.md", or with " *" when dirty.
        Disposables.Add(
            Observable.CombineLatest(
                editorViewModel.WhenAnyValue(x => x.IsDirty),
                editorViewModel.WhenAnyValue(x => x.ActiveNode),
                editorViewModel.WhenAnyValue(x => x.ActiveFilePath),
                workspaceViewModel.WhenAnyValue(x => x.WorkspaceName),
                (dirty, node, activeFilePath, workspaceName) =>
                {
                    if (string.IsNullOrEmpty(workspaceName))
                        return "Hymnal";

                    var fileName = node != null
                        ? Path.GetFileName(node.RelativePath)
                        : activeFilePath != null
                            ? Path.GetFileName(activeFilePath)
                            : null;

                    if (string.IsNullOrEmpty(fileName))
                        return $"Hymnal \u2014 {workspaceName}";

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

    private async Task EnterResearchModeAsync()
    {
        if (EditorViewModel.ActiveNode != null
            || EditorViewModel.IsBookSelected
            || EditorViewModel.ShowMissingChapterPrompt)
        {
            if (EditorViewModel.IsDirty)
            {
                try
                {
                    await EditorViewModel.SaveAsync();
                }
                catch
                {
                    return;
                }
            }

            EditorViewModel.CloseChapter();
            WorkspaceViewModel.ClearChapterSelectionForExternalDocument();
        }

        ActiveMode = ShellMode.Research;
    }
}
