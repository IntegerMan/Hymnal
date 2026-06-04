using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using DynamicData.Binding;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models;
using ReactiveUI;
using Unit = System.Reactive.Unit;

namespace Hymnal.ViewModels;

/// <summary>
/// View-model for the supplemental DOCS sidebar section. Supplemental documents are
/// intentionally kept separate from manuscript chapter nodes and open through the
/// editor's arbitrary-file path with ActiveNode == null.
/// </summary>
public sealed class SupplementalDocsViewModel : ViewModelBase
{
    private readonly WorkspaceViewModel _workspace;
    private readonly ISupplementalDocsService _docsService;
    private readonly EditorViewModel _editor;
    private readonly INotificationService _notificationService;
    private readonly ObservableCollectionExtended<SupplementalDocNode> _nodes = new();
    private readonly Subject<SupplementalDocNode> _documentOpened = new();

    public ReadOnlyObservableCollection<SupplementalDocNode> Nodes { get; }

    public IObservable<SupplementalDocNode> DocumentOpened => _documentOpened.AsObservable();

    private SupplementalDocNode? _selectedNode;
    public SupplementalDocNode? SelectedNode
    {
        get => _selectedNode;
        set => this.RaiseAndSetIfChanged(ref _selectedNode, value);
    }

    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
    public ReactiveCommand<string, Unit> CreateFolderCommand { get; }
    public ReactiveCommand<string, Unit> CreateFileCommand { get; }
    public ReactiveCommand<SupplementalDocNode, Unit> OpenDocCommand { get; }

    public SupplementalDocsViewModel(
        WorkspaceViewModel workspace,
        ISupplementalDocsService docsService,
        EditorViewModel editor,
        INotificationService notificationService)
    {
        _workspace = workspace;
        _docsService = docsService;
        _editor = editor;
        _notificationService = notificationService;

        Nodes = new ReadOnlyObservableCollection<SupplementalDocNode>(_nodes);

        var hasWorkspace = _workspace.WhenAnyValue(x => x.HasWorkspace);
        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshAsync, hasWorkspace);
        CreateFolderCommand = ReactiveCommand.CreateFromTask<string>(CreateFolderAsync, hasWorkspace);
        CreateFileCommand = ReactiveCommand.CreateFromTask<string>(CreateFileAsync, hasWorkspace);
        OpenDocCommand = ReactiveCommand.CreateFromTask<SupplementalDocNode>(OpenDocAsync, hasWorkspace);

        Disposables.Add(RefreshCommand.ThrownExceptions.Subscribe(Observer.Create<Exception>(ex => _notificationService.ShowError($"Failed to refresh supplemental docs: {ex.Message}"))));
        Disposables.Add(CreateFolderCommand.ThrownExceptions.Subscribe(Observer.Create<Exception>(ex => _notificationService.ShowError($"Failed to create supplemental docs folder: {ex.Message}"))));
        Disposables.Add(CreateFileCommand.ThrownExceptions.Subscribe(Observer.Create<Exception>(ex => _notificationService.ShowError($"Failed to create supplemental docs file: {ex.Message}"))));
        Disposables.Add(OpenDocCommand.ThrownExceptions.Subscribe(Observer.Create<Exception>(ex => _notificationService.ShowError($"Failed to open supplemental doc: {ex.Message}"))));

        Disposables.Add(
            _workspace.WorkspaceChanged
                .Subscribe(__ => { _ = RefreshAsync(); }));

        Disposables.Add(Disposable.Create(() => _documentOpened.Dispose()));
    }

    public async Task RefreshAsync()
    {
        if (!_workspace.HasWorkspace)
        {
            _nodes.Clear();
            SelectedNode = null;
            return;
        }

        var result = await _docsService.LoadTreeAsync(_workspace.WorkspaceRoot);
        if (!result.IsSuccess)
        {
            _notificationService.ShowError(result.Error!);
            return;
        }

        _nodes.Clear();
        foreach (var node in result.Value!)
            _nodes.Add(node);
    }

    public async Task CreateFolderAsync(string folderName)
    {
        if (!_workspace.HasWorkspace)
            return;

        var result = await _docsService.CreateFolderAsync(_workspace.WorkspaceRoot, GetSelectedParentRelativePath(), folderName);
        if (!result.IsSuccess)
        {
            _notificationService.ShowError(result.Error!);
            return;
        }

        await RefreshAsync();
        SelectedNode = FindNode(result.Value!.RelativePath);
    }

    public async Task CreateFileAsync(string fileName)
    {
        if (!_workspace.HasWorkspace)
            return;

        var result = await _docsService.CreateFileAsync(_workspace.WorkspaceRoot, GetSelectedParentRelativePath(), fileName);
        if (!result.IsSuccess)
        {
            _notificationService.ShowError(result.Error!);
            return;
        }

        await RefreshAsync();
        var created = FindNode(result.Value!.RelativePath) ?? result.Value!;
        SelectedNode = created;
        await OpenDocAsync(created);
    }

    public async Task OpenDocAsync(SupplementalDocNode node)
    {
        if (!_workspace.HasWorkspace || node.Kind != SupplementalDocNodeKind.File)
            return;

        if (_editor.IsDirty)
        {
            try
            {
                await _editor.SaveAsync();
            }
            catch
            {
                return;
            }
        }

        try
        {
            await _editor.OpenArbitraryFileAsync(node.AbsolutePath);
            _workspace.ClearChapterSelectionForExternalDocument();
            SelectedNode = node;
            _documentOpened.OnNext(node);
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Failed to open supplemental doc '{node.DisplayName}': {ex.Message}");
        }
    }

    private string? GetSelectedParentRelativePath()
    {
        if (SelectedNode == null)
            return null;

        if (SelectedNode.Kind == SupplementalDocNodeKind.Folder)
            return SelectedNode.RelativePath;

        var parent = Path.GetDirectoryName(SelectedNode.RelativePath)?.Replace('\\', '/');
        return string.IsNullOrWhiteSpace(parent) ? null : parent;
    }

    private SupplementalDocNode? FindNode(string relativePath)
    {
        foreach (var node in _nodes)
        {
            var match = FindNode(node, relativePath);
            if (match != null)
                return match;
        }

        return null;
    }

    private static SupplementalDocNode? FindNode(SupplementalDocNode node, string relativePath)
    {
        if (string.Equals(node.RelativePath, relativePath, StringComparison.OrdinalIgnoreCase))
            return node;

        foreach (var child in node.Children)
        {
            var match = FindNode(child, relativePath);
            if (match != null)
                return match;
        }

        return null;
    }
}
