using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Reactive.Disposables;
using DynamicData;
using DynamicData.Binding;
using PathHelper = Hymnal.Core.Common.PathHelper;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models;
using Hymnal.Core.Services;
using ReactiveUI;

namespace Hymnal.ViewModels;

public class WorkspaceViewModel : ViewModelBase
{
    private readonly ManuscriptService _manuscriptService;
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

    private readonly ObservableCollectionExtended<ChapterViewModel> _nodes = new();
    public ReadOnlyObservableCollection<ChapterViewModel> Nodes { get; }

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

    public ReactiveCommand<Unit, Unit> OpenWorkspaceCommand { get; }
    public ReactiveCommand<Unit, Unit> CloseWorkspaceCommand { get; }

    public WorkspaceViewModel(
        ManuscriptService manuscriptService,
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

        OpenWorkspaceCommand = ReactiveCommand.CreateFromTask(OpenWorkspaceAsync);
        CloseWorkspaceCommand = ReactiveCommand.CreateFromTask(CloseWorkspaceAsync, this.WhenAnyValue(x => x.HasWorkspace));

        Disposables.Add(
            OpenWorkspaceCommand.ThrownExceptions
                .Subscribe(Observer.Create<Exception>(ex => _notificationService.ShowError(ex.Message))));
        Disposables.Add(
            CloseWorkspaceCommand.ThrownExceptions
                .Subscribe(Observer.Create<Exception>(ex => _notificationService.ShowError(ex.Message))));

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

        // Subscribe to EditorViewModel.Saved: recount, update totals, record history.
        // Fires on the UI thread (Subject.OnNext is called from SaveAsync).
        Disposables.Add(
            _editor.Saved.Subscribe(saved =>
            {
                var activeNode = _editor.ActiveNode;
                if (activeNode == null || _model == null) return;

                var vm = _nodes.FirstOrDefault(v => v.Node == activeNode);
                if (vm == null) return;

                var count = _wordCountService.CountWords(_editor.Text);
                vm.UpdateWordCount(count);
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
        if (node.Kind == NodeKind.Part || node.IsMissing)
        {
            _isSwitching = true;
            SelectedNode = null;
            _isSwitching = false;
            return;
        }

        var previousNode = _editor.ActiveNode != null
            ? _nodes.FirstOrDefault(vm => vm.Node == _editor.ActiveNode)
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
        await _settingsStore.SetAsync("lastChapterPath", Path.GetFullPath(absolutePath));
    }

    private string ResolveAbsolutePath(ChapterNode node) =>
        Path.Combine(_model!.ManuscriptRoot, node.RelativePath);

    // ── Workspace open ────────────────────────────────────────────────────────

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
        _isSwitching = false;
        SelectedNode = null;
        HasWorkspace = false;
        WorkspaceName = null;
        ErrorMessage = null;
        IsLoading = false;

        await _settingsStore.SetAsync<string?>("lastWorkspacePath", null);
        await _settingsStore.SetAsync<string?>("lastChapterPath", null);
        TotalWordCount = 0;
    }

    // ── Init / restore ────────────────────────────────────────────────────────

    public async Task InitAsync()
    {
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

            // Launch background word count for each chapter (independent, failures are silent).
            foreach (var vm in vms)
            {
                if (vm.Node.Kind != NodeKind.Chapter || vm.Node.IsMissing)
                    continue;

                var localVm = vm;
                var absPath = Path.Combine(model.ManuscriptRoot, localVm.Node.RelativePath);
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var content = await File.ReadAllTextAsync(absPath).ConfigureAwait(false);
                        var count = _wordCountService.CountWords(content);
                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (_workspaceGeneration != generation) return;
                            localVm.UpdateWordCount(count);
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
                UpdateTotals();
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
        TotalWordCount = _nodes
            .Where(vm => vm.Node.Kind == NodeKind.Chapter)
            .Sum(vm => vm.WordCount);
        RecomputePartTotals();
    }

    /// <summary>
    /// Iterates _nodes in display order; for each Part node, accumulates the
    /// WordCount of the following Chapter nodes until the next Part or end of list.
    /// </summary>
    private void RecomputePartTotals()
    {
        ChapterViewModel? currentPart = null;
        int accumulated = 0;

        foreach (var vm in _nodes)
        {
            if (vm.Node.Kind == NodeKind.Part)
            {
                if (currentPart != null)
                    currentPart.PartTotalWordCount = accumulated;

                currentPart = vm;
                accumulated = 0;
            }
            else if (vm.Node.Kind == NodeKind.Chapter)
            {
                accumulated += vm.WordCount;
            }
        }

        if (currentPart != null)
            currentPart.PartTotalWordCount = accumulated;
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
}
