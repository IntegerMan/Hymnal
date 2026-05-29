using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Reactive.Disposables;
using DynamicData;
using DynamicData.Binding;
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
    private ManuscriptModel? _model;

    // Guards against re-entrancy when the VM resets SelectedNode programmatically.
    private bool _isSwitching;

    private readonly ObservableCollectionExtended<ChapterNode> _nodes = new();
    public ReadOnlyObservableCollection<ChapterNode> Nodes { get; }

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

    private ChapterNode? _selectedNode;
    public ChapterNode? SelectedNode
    {
        get => _selectedNode;
        set => this.RaiseAndSetIfChanged(ref _selectedNode, value);
    }

    public ReactiveCommand<Unit, Unit> OpenWorkspaceCommand { get; }
    public ReactiveCommand<Unit, Unit> CloseWorkspaceCommand { get; }

    public WorkspaceViewModel(
        ManuscriptService manuscriptService,
        IAppSettingsStore settingsStore,
        IFolderPickerService folderPicker,
        INotificationService notificationService,
        EditorViewModel editor)
    {
        _manuscriptService = manuscriptService;
        _settingsStore = settingsStore;
        _folderPicker = folderPicker;
        _notificationService = notificationService;
        _editor = editor;
        _editor.HasWorkspace = false;

        Nodes = new ReadOnlyObservableCollection<ChapterNode>(_nodes);

        OpenWorkspaceCommand = ReactiveCommand.CreateFromTask(OpenWorkspaceAsync);
        CloseWorkspaceCommand = ReactiveCommand.CreateFromTask(CloseWorkspaceAsync, this.WhenAnyValue(x => x.HasWorkspace));

        Disposables.Add(
            OpenWorkspaceCommand.ThrownExceptions
                .Subscribe(Observer.Create<Exception>(ex => _notificationService.ShowError(ex.Message))));
        Disposables.Add(
            CloseWorkspaceCommand.ThrownExceptions
                .Subscribe(Observer.Create<Exception>(ex => _notificationService.ShowError(ex.Message))));

        // React to user-initiated chapter selection (skip the initial null emission).
        Disposables.Add(
            this.WhenAnyValue(x => x.SelectedNode)
                .Skip(1)
                .Where(_ => !_isSwitching)
                .Subscribe(node =>
                {
                    if (node != null)
                        _ = TrySwitchChapterAsync(node);
                }));
    }

    // ── Chapter switch ────────────────────────────────────────────────────────

    /// <summary>
    /// Switches the active chapter to <paramref name="node"/>:
    /// <list type="bullet">
    ///   <item>Skips Part nodes and missing-file nodes (resets SelectedNode to null).</item>
    ///   <item>Saves the current buffer if IsDirty; aborts and resets SelectedNode on save failure.</item>
    ///   <item>Opens the new chapter and persists lastChapterPath.</item>
    /// </list>
    /// </summary>
    private async Task TrySwitchChapterAsync(ChapterNode node)
    {
        // Guard: Part nodes and missing-file nodes are non-selectable.
        if (node.Kind == NodeKind.Part || node.IsMissing)
        {
            _isSwitching = true;
            SelectedNode = null;
            _isSwitching = false;
            return;
        }

        var previousNode = _editor.ActiveNode;

        // Save-before-switch: if the buffer is dirty, attempt a save first.
        if (_editor.IsDirty)
        {
            try
            {
                await _editor.SaveAsync();
            }
            catch
            {
                // SaveAsync already surfaced the error via INotificationService.ShowError.
                // Revert the selection to the previously open chapter.
                _isSwitching = true;
                SelectedNode = previousNode;
                _isSwitching = false;
                return;
            }
        }

        var absolutePath = ResolveAbsolutePath(node);
        await _editor.OpenChapterAsync(node, absolutePath);
        await _settingsStore.SetAsync("lastChapterPath", absolutePath);
    }

    private string ResolveAbsolutePath(ChapterNode node) =>
        Path.Combine(_model!.ManuscriptRoot, node.RelativePath);

    // ── Workspace open ────────────────────────────────────────────────────────

    private async Task OpenWorkspaceAsync()
    {
        var path = await _folderPicker.PickFolderAsync();
        if (path == null)
            return;

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
        if (!HasWorkspace)
            return;

        if (_editor.IsDirty)
        {
            try
            {
                await _editor.SaveAsync();
            }
            catch
            {
                // SaveAsync already surfaced the error. Keep the workspace open.
                return;
            }
        }

        _manuscriptService.UnloadWorkspace();
        _editor.CloseChapter();
        _editor.HasWorkspace = false;

        _model = null;
        _nodes.Clear();
        _isSwitching = false;
        SelectedNode = null;
        HasWorkspace = false;
        ErrorMessage = null;
        IsLoading = false;

        await _settingsStore.SetAsync<string?>("lastWorkspacePath", null);
        await _settingsStore.SetAsync<string?>("lastChapterPath", null);
    }

    // ── Init / restore ────────────────────────────────────────────────────────

    public async Task InitAsync()
    {
        var lastPath = await _settingsStore.GetAsync<string>("lastWorkspacePath");
        if (lastPath == null)
            return;

        IsLoading = true;
        try
        {
            var result = await _manuscriptService.LoadWorkspaceAsync(lastPath);
            if (!result.IsSuccess)
                return;

            BindModel(result.Value!);
            _editor.HasWorkspace = true;

            // Restore last edited chapter silently (no error banner on restore failure).
            var lastChapterPath = await _settingsStore.GetAsync<string>("lastChapterPath");
            if (lastChapterPath != null)
            {
                ChapterNode? match = null;
                foreach (var n in _nodes)
                {
                    if (n.Kind == NodeKind.Chapter && !n.IsMissing &&
                        ResolveAbsolutePath(n) == lastChapterPath)
                    {
                        match = n;
                        break;
                    }
                }

                if (match != null)
                {
                    // Set SelectedNode with the guard so the WhenAnyValue subscription
                    // doesn't double-fire; then open the chapter directly.
                    _isSwitching = true;
                    SelectedNode = match;
                    _isSwitching = false;

                    try
                    {
                        await _editor.OpenChapterAsync(match, ResolveAbsolutePath(match));
                    }
                    catch
                    {
                        // Restore failure is silent — don't surface a banner on app startup.
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

        Disposables.Add(
            model.Nodes
                .Connect()
                .SortBy(n => n.Index)
                .Bind(_nodes)
                .Subscribe(Observer.Create<DynamicData.ISortedChangeSet<ChapterNode, string>>(_ => { })));
    }
}
