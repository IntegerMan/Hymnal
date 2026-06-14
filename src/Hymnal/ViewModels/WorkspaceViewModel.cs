using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Reactive.Disposables;
using DynamicData;
using DynamicData.Binding;
using Hymnal.Core.Common;
using Unit = Hymnal.Core.Common.Unit;
using PathHelper = Hymnal.Core.Common.PathHelper;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models;
using Hymnal.Core.Services;
using ReactiveUI;

namespace Hymnal.ViewModels;

public class WorkspaceViewModel : ViewModelBase
{
    private readonly ManuscriptService _manuscriptService;
    private readonly IBookTxtStructureService _structureService;
    private readonly IFilePickerService _filePicker;
    private readonly IAppSettingsStore _settingsStore;
    private readonly IFolderPickerService _folderPicker;
    private readonly INotificationService _notificationService;
    private readonly EditorViewModel _editor;
    private readonly ChapterRegistryService _registryService;
    private readonly PhaseDataService _phaseDataService;
    private readonly TargetsService _targetsService;
    private readonly WordCountService _wordCountService;
    private readonly WordCountHistoryService _historyService;
    private ManuscriptModel? _model;
    private int _workspaceGeneration;
    private Task? _hydrationTask;
    private bool _isSwitching;
    private readonly Subject<Unit> _workspaceChanged = new();
    private CompositeDisposable _partSubscriptions = new();

    public IObservable<Unit> WorkspaceChanged => _workspaceChanged.AsObservable();
    private readonly ObservableCollectionExtended<ChapterViewModel> _nodes = new();
    private readonly ObservableCollectionExtended<ChapterViewModel> _visibleNodes = new();
    private readonly Dictionary<string, ChapterViewModel> _nodesByPath = new(StringComparer.OrdinalIgnoreCase);
    public ReadOnlyObservableCollection<ChapterViewModel> Nodes { get; }
    public ReadOnlyObservableCollection<ChapterViewModel> VisibleNodes { get; }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        private set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    private bool _hasWorkspace;
    public bool HasWorkspace
    {
        get => _hasWorkspace;
        private set => this.RaiseAndSetIfChanged(ref _hasWorkspace, value);
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    private ChapterViewModel? _selectedNode;
    public ChapterViewModel? SelectedNode
    {
        get => _selectedNode;
        set => this.RaiseAndSetIfChanged(ref _selectedNode, value);
    }

    public string WorkspaceRoot => _model?.WorkspaceRoot ?? string.Empty;

    public string BookTxtPath => _model?.BookTxtPath ?? string.Empty;

    public string ManuscriptRoot => _model?.ManuscriptRoot ?? string.Empty;

    private string? _workspaceName;
    public string? WorkspaceName
    {
        get => _workspaceName;
        private set => this.RaiseAndSetIfChanged(ref _workspaceName, value);
    }

    private int _totalWordCount;
    public int TotalWordCount
    {
        get => _totalWordCount;
        private set => this.RaiseAndSetIfChanged(ref _totalWordCount, value);
    }

    private readonly ObservableAsPropertyHelper<string> _totalWordCountDisplay;
    /// <summary>Formatted book total for the CHAPTERS header, e.g. "12,345 w".</summary>
    public string TotalWordCountDisplay => _totalWordCountDisplay.Value;

    private readonly ObservableAsPropertyHelper<string> _totalWordCountTooltip;
    /// <summary>Full tooltip for the book total word count label.</summary>
    public string TotalWordCountTooltip => _totalWordCountTooltip.Value;

    private IReadOnlyList<StatusCount> _bookStatusSummary = Array.Empty<StatusCount>();
    /// <summary>Status breakdown for all chapters in the book, used by the Book header pie chart.</summary>
    public IReadOnlyList<StatusCount> BookStatusSummary
    {
        get => _bookStatusSummary;
        private set => this.RaiseAndSetIfChanged(ref _bookStatusSummary, value);
    }

    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> OpenWorkspaceCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> CloseWorkspaceCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> SelectBookCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> ToggleChaptersPaneCommand { get; }
    public ReactiveCommand<CreateChapterRequest, System.Reactive.Unit> CreateChapterCommand { get; }
    public ReactiveCommand<CreatePartRequest, System.Reactive.Unit> CreatePartCommand { get; }
    public ReactiveCommand<IncludeExistingChapterRequest, System.Reactive.Unit> IncludeExistingFileCommand { get; }
    public ReactiveCommand<string, System.Reactive.Unit> RemoveFromBookCommand { get; }

    private ChapterViewModel? _targetFlyoutChapter;
    private IDisposable? _targetFlyoutSubscription;
    /// <summary>Chapter whose shared target flyout is open in the sidebar.</summary>
    public ChapterViewModel? TargetFlyoutChapter
    {
        get => _targetFlyoutChapter;
        private set => SetTargetFlyoutChapter(value);
    }

    /// <summary>Null-safe open state for the shared target flyout Popup.</summary>
    public bool IsTargetFlyoutOpen
    {
        get => TargetFlyoutChapter?.IsTargetFlyoutOpen ?? false;
        set
        {
            if (value)
                return;

            CloseTargetFlyout();
        }
    }

    private bool _isChaptersPaneVisible = true;
    /// <summary>True when the CHAPTERS list content area is expanded in the left sidebar.</summary>
    public bool IsChaptersPaneVisible
    {
        get => _isChaptersPaneVisible;
        set
        {
            if (_isChaptersPaneVisible == value)
                return;

            this.RaiseAndSetIfChanged(ref _isChaptersPaneVisible, value);
            _ = PersistChaptersPaneVisibleAsync(value);
        }
    }

    public WorkspaceViewModel(
        ManuscriptService manuscriptService,
        IBookTxtStructureService structureService,
        IFilePickerService filePicker,
        IAppSettingsStore settingsStore,
        IFolderPickerService folderPicker,
        INotificationService notificationService,
        EditorViewModel editor,
        ChapterRegistryService registryService,
        PhaseDataService phaseDataService,
        TargetsService targetsService,
        WordCountService wordCountService,
        WordCountHistoryService historyService)
    {
        _manuscriptService = manuscriptService;
        _structureService = structureService;
        _filePicker = filePicker;
        _settingsStore = settingsStore;
        _folderPicker = folderPicker;
        _notificationService = notificationService;
        _editor = editor;
        _registryService = registryService;
        _phaseDataService = phaseDataService;
        _targetsService = targetsService;
        _wordCountService = wordCountService;
        _historyService = historyService;
        _editor.HasWorkspace = false;

        Nodes = new ReadOnlyObservableCollection<ChapterViewModel>(_nodes);
        VisibleNodes = new ReadOnlyObservableCollection<ChapterViewModel>(_visibleNodes);

        ToggleChaptersPaneCommand = ReactiveCommand.Create(
            () => { IsChaptersPaneVisible = !IsChaptersPaneVisible; },
            this.WhenAnyValue(x => x.HasWorkspace));

        OpenWorkspaceCommand = ReactiveCommand.CreateFromTask(OpenWorkspaceAsync);
        CloseWorkspaceCommand = ReactiveCommand.CreateFromTask(CloseWorkspaceAsync, this.WhenAnyValue(x => x.HasWorkspace));
        SelectBookCommand = ReactiveCommand.CreateFromTask(SelectBookAsync);

        CreateChapterCommand = ReactiveCommand.CreateFromTask<CreateChapterRequest>(CreateChapterAsync,
            this.WhenAnyValue(x => x.HasWorkspace));
        CreatePartCommand = ReactiveCommand.CreateFromTask<CreatePartRequest>(CreatePartAsync,
            this.WhenAnyValue(x => x.HasWorkspace));
        IncludeExistingFileCommand = ReactiveCommand.CreateFromTask<IncludeExistingChapterRequest>(IncludeExistingFileAsync,
            this.WhenAnyValue(x => x.HasWorkspace));
        RemoveFromBookCommand = ReactiveCommand.CreateFromTask<string>(RemoveFromBookAsync,
            this.WhenAnyValue(x => x.HasWorkspace));

        Disposables.Add(
            CreateChapterCommand.ThrownExceptions
                .Subscribe(Observer.Create<Exception>(ex => _notificationService.ShowError(ex.Message))));
        Disposables.Add(
            CreatePartCommand.ThrownExceptions
                .Subscribe(Observer.Create<Exception>(ex => _notificationService.ShowError(ex.Message))));
        Disposables.Add(
            IncludeExistingFileCommand.ThrownExceptions
                .Subscribe(Observer.Create<Exception>(ex => _notificationService.ShowError(ex.Message))));
        Disposables.Add(
            RemoveFromBookCommand.ThrownExceptions
                .Subscribe(Observer.Create<Exception>(ex => _notificationService.ShowError(ex.Message))));

        Disposables.Add(
            OpenWorkspaceCommand.ThrownExceptions
                .Subscribe(Observer.Create<Exception>(ex => _notificationService.ShowError(ex.Message))));
        Disposables.Add(
            CloseWorkspaceCommand.ThrownExceptions
                .Subscribe(Observer.Create<Exception>(ex => _notificationService.ShowError(ex.Message))));
        Disposables.Add(
            SelectBookCommand.ThrownExceptions
                .Subscribe(Observer.Create<Exception>(ex => _notificationService.ShowError(ex.Message))));
        Disposables.Add(Disposable.Create(() => _workspaceChanged.Dispose()));
        Disposables.Add(Disposable.Create(() => _targetFlyoutSubscription?.Dispose()));
        Disposables.Add(Disposable.Create(() => _partSubscriptions.Dispose()));

        Disposables.Add(
            this.WhenAnyValue(x => x.SelectedNode)
                .Skip(1)
                .Where(_ => !_isSwitching)
                .Subscribe(node =>
                {
                    if (node != null)
                        _ = TrySwitchChapterAsync(node);
                }));

        // TotalWordCountDisplay OAPH — updates whenever TotalWordCount changes.
        _totalWordCountDisplay = this.WhenAnyValue(x => x.TotalWordCount)
            .Select(c => $"{c:N0} w")
            .ToProperty(this, x => x.TotalWordCountDisplay);
        Disposables.Add(_totalWordCountDisplay);

        _totalWordCountTooltip = this.WhenAnyValue(x => x.TotalWordCount)
            .Select(c => $"{c:N0} words total")
            .ToProperty(this, x => x.TotalWordCountTooltip);
        Disposables.Add(_totalWordCountTooltip);

        // Subscribe to EditorViewModel.Saved: recount, update totals, record history,
        // and refresh sidebar title if the heading line changed.
        // Fires on the UI thread (Subject.OnNext is called from SaveAsync).
        Disposables.Add(
            _editor.Saved.Subscribe(_ =>
            {
                // Book.txt save: reload workspace first, before the activeNode guard.
                if (_editor.IsBookSelected && _model != null)
                {
                    var root = _model.WorkspaceRoot;
                    var bookTxtPath = _model.BookTxtPath;
                    var task = ReloadWorkspaceAsync(root, bookTxtPath);
                    task.ContinueWith(t =>
                    {
                        if (!t.IsCompletedSuccessfully && t.Exception != null)
                            _notificationService.ShowError(
                                $"Failed to reload workspace after Book.txt edit: {t.Exception.InnerException?.Message ?? t.Exception.Message}");
                    }, TaskScheduler.Default);
                    return;
                }

                var activeNode = _editor.ActiveNode;
                if (activeNode == null || _model == null) return;

                if (!_nodesByPath.TryGetValue(activeNode.RelativePath, out var vm))
                    return;

                var count = _wordCountService.CountWords(_editor.Text);
                vm.UpdateWordCount(count);

                // Refresh sidebar title if the # heading changed in the saved content.
                var newTitle = BookTxtParser.ExtractTitleFromText(_editor.Text);
                if (newTitle != null && !string.Equals(newTitle, vm.Node.Title, StringComparison.Ordinal))
                {
                    var updatedNode = vm.Node with { Title = newTitle };
                    vm.UpdateNode(updatedNode);
                    _editor.UpdateActiveNode(updatedNode);
                }

                UpdateTotals();

                if (!string.IsNullOrEmpty(vm.Uuid))
                {
                    var workspaceRoot = _model.WorkspaceRoot;
                    var uuid = vm.Uuid;
                    var date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
                    var histTask = _historyService.AppendAsync(workspaceRoot, uuid, date, count);
                    histTask.ContinueWith(t =>
                        {
                            if (!t.IsCompletedSuccessfully && t.Exception != null)
                            {
                                _notificationService.ShowError(
                                    $"Failed to save word count history: {t.Exception.InnerException?.Message ?? t.Exception.Message}");
                            }
                        }, TaskScheduler.Default);
                }

            }));
    }

    // ── Chapter switch ────────────────────────────────────────────────────────

    private async Task TrySwitchChapterAsync(ChapterViewModel viewModel)
    {
        var node = viewModel.Node;

        // Missing chapter: show warning overlay instead of deselecting.
        if (node.IsMissing)
        {
            if (_editor.IsDirty)
            {
                try { await _editor.SaveAsync(); }
                catch { /* Don't abort — still show the missing warning */ }
            }
            _editor.OpenMissingChapter(node);
            return;
        }

        var previousNode = _editor.ActiveNode != null
            && _nodesByPath.TryGetValue(_editor.ActiveNode.RelativePath, out var prev)
            ? prev
            : null;

        if (_editor.IsDirty)
        {
            try
            {
                await _editor.SaveAsync();
            }
            catch
            {
                _isSwitching = true;
                SelectedNode = previousNode;
                _isSwitching = false;
                return;
            }
        }

        var absolutePath = ResolveAbsolutePath(node);
        await _editor.OpenChapterAsync(node, absolutePath);

        // Only persist as "last chapter" for regular chapters, not parts.
        if (node.Kind == NodeKind.Chapter)
            await _settingsStore.SetAsync("lastChapterPath", Path.GetFullPath(absolutePath));
    }

    private async Task SelectBookAsync()
    {
        if (_editor.IsDirty)
        {
            try { await _editor.SaveAsync(); }
            catch { return; }
        }

        _isSwitching = true;
        SelectedNode = null;
        _isSwitching = false;
        await _editor.SelectBookAsync(_model?.BookTxtPath ?? string.Empty);
    }

    private string ResolveAbsolutePath(ChapterNode node) =>
        Path.Combine(_model!.ManuscriptRoot, node.RelativePath);

    public void ClearChapterSelectionForExternalDocument()
    {
        _isSwitching = true;
        SelectedNode = null;
        _isSwitching = false;
    }

    // ── Workspace open ────────────────────────────────────────────────────────

    /// <summary>
    /// Reload the current workspace from disk and rebuild the chapter list without
    /// changing the editor's active file.
    /// </summary>
    public virtual async Task<Result<Unit>> ReloadCurrentWorkspaceAsync()
    {
        if (_model == null)
            return Result<Unit>.Ok(Unit.Default);

        return await ReloadWorkspaceAsync(_model.WorkspaceRoot, _model.BookTxtPath, reselectBook: false);
    }

    /// <summary>
    /// Lightweight reorder that re-reads Book.txt and rearranges the existing
    /// <see cref="Nodes"/> to match, without re-hydrating the registry, phases,
    /// targets, or word counts.
    /// </summary>
    public virtual async Task<Result<Unit>> ReorderNodesAsync()
    {
        if (_model == null)
            return Result<Unit>.Ok(Unit.Default);

        try
        {
            var bookTxtPath = _model.BookTxtPath;
            if (string.IsNullOrWhiteSpace(bookTxtPath) || !File.Exists(bookTxtPath))
                return Result<Unit>.Fail("Book.txt not found.");

            var lines = await File.ReadAllLinesAsync(bookTxtPath).ConfigureAwait(false);
            var newOrder = lines
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrEmpty(l))
                .Select(l => l.Replace('\\', '/'))
                .ToList();

            var reordered = new List<ChapterViewModel>(newOrder.Count);
            foreach (var path in newOrder)
            {
                if (_nodesByPath.TryGetValue(path, out var vm))
                    reordered.Add(vm);
            }

            using (_nodes.SuspendNotifications())
            {
                _nodes.Clear();
                foreach (var vm in reordered)
                    _nodes.Add(vm);
            }

            RebuildNodeLookup();
            UpdateTotals();
            UpdateVisibility();
            _workspaceChanged.OnNext(Unit.Default);
            return Result<Unit>.Ok(Unit.Default);
        }
        catch (Exception ex)
        {
            return Result<Unit>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Reload the workspace from disk (called after Book.txt is saved) without disturbing
    /// the editor. Keeps Book.txt open so the user can continue editing.
    /// </summary>
    private async Task<Result<Unit>> ReloadWorkspaceAsync(string workspaceRoot, string bookTxtPath, bool reselectBook = true)
    {
        var result = await _manuscriptService.LoadWorkspaceAsync(workspaceRoot);
        if (!result.IsSuccess)
        {
            _notificationService.ShowError($"Failed to reload workspace: {result.Error}");
            return Result<Unit>.Fail(result.Error!);
        }

        // Rebuild node list without closing the editor.
        BindModel(result.Value!);

        if (reselectBook)
        {
            // Re-open Book.txt in the editor so the title bar and watcher stay current.
            await _editor.SelectBookAsync(bookTxtPath);
        }

        return Result<Unit>.Ok(Unit.Default);
    }

    private async Task OpenWorkspaceAsync()
    {
        var path = await _folderPicker.PickFolderAsync();
        if (path == null) return;

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var result = await _manuscriptService.LoadWorkspaceAsync(path);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error;
                _notificationService.ShowError(result.Error!);
                return;
            }

            ErrorMessage = null;
            await _settingsStore.SetAsync("lastWorkspacePath", path);
            _editor.HasWorkspace = true;
            BindModel(result.Value!);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CloseWorkspaceAsync()
    {
        if (!HasWorkspace) return;

        if (_editor.IsDirty)
        {
            try { await _editor.SaveAsync(); }
            catch { return; }
        }

        _manuscriptService.UnloadWorkspace();
        _editor.CloseChapter();
        _editor.HasWorkspace = false;

        foreach (var vm in _nodes) vm.Dispose();
        _model = null;
        _nodes.Clear();
        _nodesByPath.Clear();
        TargetFlyoutChapter = null;
        _isSwitching = false;
        SelectedNode = null;
        HasWorkspace = false;
        WorkspaceName = null;
        this.RaisePropertyChanged(nameof(WorkspaceRoot));
        this.RaisePropertyChanged(nameof(BookTxtPath));
        _workspaceChanged.OnNext(Unit.Default);
        ErrorMessage = null;
        IsLoading = false;

        await _settingsStore.SetAsync<string?>("lastWorkspacePath", null);
        await _settingsStore.SetAsync<string?>("lastChapterPath", null);
        TotalWordCount = 0;
    }

    // ── Init / restore ────────────────────────────────────────────────────────

    public async Task InitAsync()
    {
        await RestorePaneSettingsAsync().ConfigureAwait(false);

        var lastPath = await _settingsStore.GetAsync<string>("lastWorkspacePath");
        if (lastPath == null) return;

        IsLoading = true;
        try
        {
            var result = await _manuscriptService.LoadWorkspaceAsync(lastPath);
            if (!result.IsSuccess) return;

            BindModel(result.Value!);
            _editor.HasWorkspace = true;

            var hydrationTask = _hydrationTask;
            if (hydrationTask != null)
                await hydrationTask;

            var lastChapterPath = await _settingsStore.GetAsync<string>("lastChapterPath");
            if (lastChapterPath != null)
            {
                ChapterViewModel? match = null;
                foreach (var vm in _nodes)
                {
                    if (vm.Node.Kind == NodeKind.Chapter && !vm.Node.IsMissing &&
                        PathHelper.IsSamePath(ResolveAbsolutePath(vm.Node), lastChapterPath))
                    {
                        match = vm;
                        break;
                    }
                }

                if (match != null)
                {
                    try
                    {
                        await _editor.OpenChapterAsync(match.Node, ResolveAbsolutePath(match.Node));

                        _isSwitching = true;
                        SelectedNode = match;
                        _isSwitching = false;
                    }
                    catch
                    {
                        // Restore failure is silent on startup.
                    }
                }
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Model binding ─────────────────────────────────────────────────────────

    private void BindModel(ManuscriptModel model)
    {
        _model = model;
        HasWorkspace = true;
        WorkspaceName = Path.GetFileName(model.WorkspaceRoot.TrimEnd(
            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        this.RaisePropertyChanged(nameof(WorkspaceRoot));
        this.RaisePropertyChanged(nameof(BookTxtPath));
        _workspaceChanged.OnNext(Unit.Default);

        // Track registry/phase hydration so restore waits for the active workspace.
        var generation = ++_workspaceGeneration;
        _hydrationTask = LoadRegistryAndPhaseDataAsync(model, generation);
    }

    private async Task LoadRegistryAndPhaseDataAsync(ManuscriptModel model, int generation)
    {
        try
        {
            var activeNodes = model.Nodes.Items.OrderBy(n => n.Index).ToList();
            var activePaths = activeNodes.Select(n => n.RelativePath).ToList();

            var registry = await _registryService.LoadAsync(model.WorkspaceRoot).ConfigureAwait(false);
            var originalRegistry = new Dictionary<string, ChapterRegistryEntry>(registry);

            // Mark orphans first so delete/reopen state is preserved.
            registry = _registryService.ReconcileOrphans(registry, activePaths);

            // Preserve UUIDs across renames when the old and new chapter titles match.
            var orphanedEntries = registry.Values.Where(entry => entry.Orphaned).ToList();
            var matchedOrphans = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var node in activeNodes)
            {
                var hasCurrentPath = registry.Values.Any(entry =>
                    string.Equals(entry.CurrentPath, node.RelativePath, StringComparison.OrdinalIgnoreCase));

                if (!hasCurrentPath)
                {
                    var titleMatches = orphanedEntries
                        .Where(entry => !matchedOrphans.Contains(entry.Uuid)
                            && !string.IsNullOrWhiteSpace(entry.Title)
                            && string.Equals(entry.Title, node.Title, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (titleMatches.Count == 1)
                    {
                        registry = _registryService.ReconcileRename(
                            registry,
                            titleMatches[0].CurrentPath,
                            node.RelativePath);
                        matchedOrphans.Add(titleMatches[0].Uuid);
                    }
                }

                _registryService.AssignUuid(registry, node.RelativePath, node.Title);
            }

            if (RegistryChanged(originalRegistry, registry))
                await _registryService.SaveAsync(model.WorkspaceRoot, registry).ConfigureAwait(false);

            // Load phase data.
            var phases = await _phaseDataService.LoadAsync(model.WorkspaceRoot).ConfigureAwait(false);

            // Load word-count targets so they can be passed into ChapterViewModel constructors.
            var targets = await _targetsService.LoadAsync(model.WorkspaceRoot).ConfigureAwait(false);

            if (generation != _workspaceGeneration)
                return;

            // Load previously collapsed Part paths for this workspace.
            Dictionary<string, List<string>>? collapsedMap = null;
            try { collapsedMap = await _settingsStore.GetAsync<Dictionary<string, List<string>>>("partCollapsedStates").ConfigureAwait(false); } catch { }
            var collapsedPartPaths = collapsedMap != null && collapsedMap.TryGetValue(model.WorkspaceRoot, out var savedPaths)
                ? new HashSet<string>(savedPaths, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Build ChapterViewModels in index order.
            var vms = activeNodes
                .Select(node =>
                {
                    // Find UUID for this node.
                    string uuid = string.Empty;
                    foreach (var (u, entry) in registry)
                    {
                        if (string.Equals(entry.CurrentPath, node.RelativePath,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            uuid = u;
                            break;
                        }
                    }
                    phases.TryGetValue(uuid, out var phaseData);
                    var target = _targetsService.GetTarget(targets, uuid);
                    return new ChapterViewModel(
                        node, uuid, phaseData,
                        _phaseDataService, _targetsService, _settingsStore, _notificationService,
                        model.WorkspaceRoot, target);
                })
                .ToList();

            foreach (var vm in vms)
            {
                vm.TargetFlyoutOpenRequested += OpenTargetFlyout;
                vm.TargetFlyoutCloseRequested += CloseTargetFlyout;
            }

            // Launch background word count for all chapters in one batch.
            var countTargets = vms
                .Where(vm => vm.Node.Kind == NodeKind.Chapter && !vm.Node.IsMissing)
                .Select(vm => (Vm: vm, AbsPath: Path.Combine(model.ManuscriptRoot, vm.Node.RelativePath)))
                .ToList();

            if (countTargets.Count > 0)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var counts = await Task.WhenAll(countTargets.Select(async target =>
                        {
                            try
                            {
                                var content = await File.ReadAllTextAsync(target.AbsPath).ConfigureAwait(false);
                                return (target.Vm, Count: _wordCountService.CountWords(content));
                            }
                            catch
                            {
                                return (target.Vm, Count: -1);
                            }
                        })).ConfigureAwait(false);

                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (_workspaceGeneration != generation)
                                return;

                            foreach (var (vm, count) in counts)
                            {
                                if (count >= 0)
                                    vm.UpdateWordCount(count);
                            }

                            UpdateTotals();
                        });
                    }
                    catch
                    {
                        // Intentionally silent — chapter stays at '—' in sidebar.
                    }
                });
            }

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (generation != _workspaceGeneration)
                    return;

                foreach (var old in _nodes) old.Dispose();
                _nodes.Clear();
                foreach (var vm in vms) _nodes.Add(vm);
                RebuildNodeLookup();
                UpdateTotals();

                // Restore Part expand/collapse state before wiring subscriptions so
                // the initial restore doesn't trigger spurious saves.
                foreach (var vm in _nodes.Where(v => v.Node.Kind == NodeKind.Part))
                    vm.IsExpanded = !collapsedPartPaths.Contains(vm.Node.RelativePath);

                _partSubscriptions.Dispose();
                _partSubscriptions = new CompositeDisposable();
                foreach (var vm in _nodes)
                {
                    if (vm.Node.Kind == NodeKind.Part)
                    {
                        _partSubscriptions.Add(
                            vm.WhenAnyValue(x => x.IsExpanded)
                                .Skip(1)
                                .Subscribe(expanded =>
                                {
                                    UpdateVisibility();
                                    var root = _model?.WorkspaceRoot;
                                    if (root == null) return;
                                    var collapsed = _nodes
                                        .Where(v => v.Node.Kind == NodeKind.Part && !v.IsExpanded)
                                        .Select(v => v.Node.RelativePath)
                                        .ToList();
                                    _ = PersistPartExpandedStatesAsync(root, collapsed);
                                }));
                    }
                    else
                    {
                        _partSubscriptions.Add(
                            vm.WhenAnyValue(x => x.Status)
                                .Skip(1)
                                .Subscribe(_ => UpdateTotals()));
                    }
                }
                UpdateVisibility();
            });
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Failed to load chapter registry: {ex.Message}");
        }
    }

    // ── Word count totals ──────────────────────────────────────────────────────

    /// <summary>
    /// Recomputes the book total word count from all known chapter counts,
    /// then recalculates per-Part subtotals. Must be called on the UI thread.
    /// </summary>
    private void UpdateTotals()
    {
        int total = 0;
        ChapterViewModel? currentPart = null;
        int accumulated = 0;
        var partStatuses = new Dictionary<ChapterStatus, int>();
        var bookStatuses = new Dictionary<ChapterStatus, int>();

        foreach (var vm in _nodes)
        {
            if (vm.Node.Kind == NodeKind.Part)
            {
                if (currentPart != null)
                {
                    currentPart.PartTotalWordCount = accumulated;
                    currentPart.PartStatusSummary = ToStatusList(partStatuses);
                }

                currentPart = vm;
                accumulated = 0;
                partStatuses = new Dictionary<ChapterStatus, int>();
            }
            else if (vm.Node.Kind == NodeKind.Chapter)
            {
                total += vm.WordCount;
                accumulated += vm.WordCount;
                bookStatuses[vm.Status] = bookStatuses.GetValueOrDefault(vm.Status) + 1;
                partStatuses[vm.Status] = partStatuses.GetValueOrDefault(vm.Status) + 1;
            }
        }

        if (currentPart != null)
        {
            currentPart.PartTotalWordCount = accumulated;
            currentPart.PartStatusSummary = ToStatusList(partStatuses);
        }

        TotalWordCount = total;
        BookStatusSummary = ToStatusList(bookStatuses);
    }

    private static IReadOnlyList<StatusCount> ToStatusList(Dictionary<ChapterStatus, int> counts)
        => counts.Count == 0
            ? Array.Empty<StatusCount>()
            : counts.Select(kv => new StatusCount(kv.Key, kv.Value)).ToList();

    private void UpdateVisibility()
    {
        bool partExpanded = true;
        _visibleNodes.Clear();
        foreach (var vm in _nodes)
        {
            if (vm.Node.Kind == NodeKind.Part)
            {
                partExpanded = vm.IsExpanded;
                _visibleNodes.Add(vm);
            }
            else if (partExpanded)
            {
                _visibleNodes.Add(vm);
            }
        }
    }

    private void RebuildNodeLookup()
    {
        _nodesByPath.Clear();
        foreach (var vm in _nodes)
            _nodesByPath[vm.Node.RelativePath] = vm;
    }

    internal void OpenTargetFlyout(ChapterViewModel chapter)
    {
        if (TargetFlyoutChapter != null && !ReferenceEquals(TargetFlyoutChapter, chapter))
            TargetFlyoutChapter.IsTargetFlyoutOpen = false;

        TargetFlyoutChapter = chapter;
        chapter.IsTargetFlyoutOpen = true;
        this.RaisePropertyChanged(nameof(IsTargetFlyoutOpen));
    }

    internal void CloseTargetFlyout(ChapterViewModel? chapter = null)
    {
        var target = chapter ?? TargetFlyoutChapter;
        if (target != null)
            target.IsTargetFlyoutOpen = false;

        if (ReferenceEquals(TargetFlyoutChapter, target))
            TargetFlyoutChapter = null;

        this.RaisePropertyChanged(nameof(IsTargetFlyoutOpen));
    }

    private void SetTargetFlyoutChapter(ChapterViewModel? chapter)
    {
        _targetFlyoutSubscription?.Dispose();
        _targetFlyoutSubscription = null;

        this.RaiseAndSetIfChanged(ref _targetFlyoutChapter, chapter, nameof(TargetFlyoutChapter));

        if (chapter != null)
        {
            _targetFlyoutSubscription = chapter
                .WhenAnyValue(x => x.IsTargetFlyoutOpen)
                .Subscribe(_ => this.RaisePropertyChanged(nameof(IsTargetFlyoutOpen)));
        }

        this.RaisePropertyChanged(nameof(IsTargetFlyoutOpen));
    }

    public async Task RestorePaneSettingsAsync()
    {
        try
        {
            var stored = await _settingsStore.GetAsync<bool?>("chaptersPaneVisible").ConfigureAwait(false);
            if (stored == null)
                stored = await _settingsStore.GetAsync<bool?>("sidebarExpanded").ConfigureAwait(false);

            _isChaptersPaneVisible = stored ?? true;
            this.RaisePropertyChanged(nameof(IsChaptersPaneVisible));
        }
        catch
        {
            IsChaptersPaneVisible = true;
        }
    }

    private static bool RegistryChanged(
        IReadOnlyDictionary<string, ChapterRegistryEntry> before,
        IReadOnlyDictionary<string, ChapterRegistryEntry> after)
    {
        if (before.Count != after.Count)
            return true;

        foreach (var (uuid, entry) in before)
        {
            if (!after.TryGetValue(uuid, out var current))
                return true;

            if (!string.Equals(entry.Uuid, current.Uuid, StringComparison.Ordinal) ||
                !string.Equals(entry.CurrentPath, current.CurrentPath, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(entry.Title, current.Title, StringComparison.OrdinalIgnoreCase) ||
                entry.Orphaned != current.Orphaned)
            {
                return true;
            }
        }

        return false;
    }

    public int GetBookEntryCount() => _nodes.Count;

    public async Task<string?> PickManuscriptFileAsync()
    {
        if (string.IsNullOrWhiteSpace(ManuscriptRoot))
            return null;

        return await _filePicker.PickFileAsync(ManuscriptRoot).ConfigureAwait(false);
    }

    public string ToManuscriptRelativePath(string absolutePath)
    {
        if (string.IsNullOrWhiteSpace(ManuscriptRoot))
            return absolutePath;

        var manuscriptRoot = Path.GetFullPath(ManuscriptRoot);
        var fullPath = Path.GetFullPath(absolutePath);
        if (!fullPath.StartsWith(manuscriptRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase)
            && !string.Equals(fullPath, manuscriptRoot, StringComparison.OrdinalIgnoreCase))
        {
            return absolutePath.Replace('\\', '/');
        }

        return Path.GetRelativePath(manuscriptRoot, fullPath).Replace('\\', '/');
    }

    private async Task CreateChapterAsync(CreateChapterRequest request)
    {
        await ExecuteStructuralOperationAsync(
            "Create chapter",
            request.ChapterPath,
            () => _structureService.CreateNewChapterAsync(BookTxtPath, request.ChapterPath, request.Content, request.Index))
            .ConfigureAwait(false);
    }

    private async Task CreatePartAsync(CreatePartRequest request)
    {
        await ExecuteStructuralOperationAsync(
            "Create part",
            request.PartPath,
            () => _structureService.CreateNewPartAsync(BookTxtPath, request.PartPath, request.Title, request.Index))
            .ConfigureAwait(false);
    }

    private async Task IncludeExistingFileAsync(IncludeExistingChapterRequest request)
    {
        Func<Task<Result<Unit>>> action;

        if (!string.IsNullOrWhiteSpace(request.PartPath))
        {
            action = () => _structureService.AddExistingEntryAfterPartAsync(
                BookTxtPath,
                request.ChapterPath,
                request.PartPath!);
        }
        else if (request.Index.HasValue)
        {
            action = () => _structureService.AddExistingEntryAsync(
                BookTxtPath,
                request.ChapterPath,
                request.Index.Value);
        }
        else
        {
            _notificationService.ShowError("Include file requires either an index or a part path.");
            return;
        }

        await ExecuteStructuralOperationAsync("Include file", request.ChapterPath, action).ConfigureAwait(false);
    }

    private async Task RemoveFromBookAsync(string chapterPath)
    {
        if (_model == null) return;
        var result = await _structureService.RemoveEntryAsync(_model.BookTxtPath, chapterPath).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            _notificationService.ShowError($"Failed to remove chapter: {result.Error}");
            return;
        }
        await ReloadWorkspaceAsync(_model.WorkspaceRoot, _model.BookTxtPath, reselectBook: false).ConfigureAwait(false);
    }

    private async Task ExecuteStructuralOperationAsync(
        string operation,
        string? path,
        Func<Task<Result<Unit>>> action)
    {
        if (!HasWorkspace || string.IsNullOrWhiteSpace(BookTxtPath))
        {
            _notificationService.ShowError("No workspace is open.");
            return;
        }

        try
        {
            using var _ = _manuscriptService.SuppressFileWatcher();

            var result = await action().ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                var target = string.IsNullOrWhiteSpace(path)
                    ? $"{operation} in '{BookTxtPath}'"
                    : $"{operation} for '{path}' in '{BookTxtPath}'";
                _notificationService.ShowError($"{target}: {result.Error}");
                return;
            }

            var reloadResult = await ReloadCurrentWorkspaceAsync().ConfigureAwait(false);
            if (!reloadResult.IsSuccess)
                _notificationService.ShowError($"Failed to reload workspace after {operation.ToLowerInvariant()}: {reloadResult.Error}");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"{operation} failed: {ex.Message}");
        }
    }

    private async Task PersistChaptersPaneVisibleAsync(bool value)
    {
        try
        {
            await _settingsStore.SetAsync("chaptersPaneVisible", value).ConfigureAwait(false);
        }
        catch
        {
            // Non-fatal; layout preference may not persist across sessions if storage fails.
        }
    }

    private async Task PersistPartExpandedStatesAsync(string workspaceRoot, List<string> collapsedPaths)
    {
        try
        {
            var existing = await _settingsStore.GetAsync<Dictionary<string, List<string>>>("partCollapsedStates")
                .ConfigureAwait(false) ?? new();
            existing[workspaceRoot] = collapsedPaths;
            await _settingsStore.SetAsync("partCollapsedStates", existing).ConfigureAwait(false);
        }
        catch
        {
            // Non-fatal; expand/collapse state may not persist across sessions if storage fails.
        }
    }
}
