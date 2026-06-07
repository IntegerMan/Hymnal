using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using DynamicData.Binding;
using Hymnal.Core.Common;
using Unit = Hymnal.Core.Common.Unit;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Services;
using ReactiveUI;

namespace Hymnal.ViewModels;

public sealed record CorkboardStructuralError(string Operation, string? Path, string Message, string? BookTxtPath = null);

public sealed record ReorderCardRequest(
    string RelativePath,
    int? NewIndex = null,
    string? AfterRelativePath = null,
    string? BeforeRelativePath = null);

public sealed record RenameCardRequest(string ExistingPath, string ReplacementPath);

public sealed record CreateChapterRequest(string ChapterPath, string Content, int Index);

public sealed record IncludeExistingChapterRequest(string ChapterPath, int? Index = null, string? PartPath = null);

public sealed record RemoveChapterRequest(string ChapterPath);

public sealed record DeleteChapterRequest(string ChapterPath, bool Confirmed);

/// <summary>
/// Coordinates the plan-mode corkboard projection, selection state, open requests,
/// and all structural Book.txt edits.
/// </summary>
public sealed class CorkboardViewModel : ViewModelBase, IDisposable
{
    private readonly WorkspaceViewModel _workspace;
    private readonly IBookTxtStructureService _structureService;
    private readonly INotificationService _notificationService;
    private readonly ManuscriptService _manuscriptService;
    private readonly Subject<ChapterViewModel> _openChapterRequested = new();
    private readonly ObservableCollectionExtended<CorkboardItemViewModel> _items = new();
    private readonly Dictionary<string, bool> _partExpandedState = new(StringComparer.OrdinalIgnoreCase);

    public ReadOnlyObservableCollection<CorkboardItemViewModel> Items { get; }

    public bool HasItems => _items.Count > 0;

    public IObservable<ChapterViewModel> OpenChapterRequested => _openChapterRequested.AsObservable();

    private CardViewModel? _selectedCard;
    public CardViewModel? SelectedCard
    {
        get => _selectedCard;
        private set => this.RaiseAndSetIfChanged(ref _selectedCard, value);
    }

    private CorkboardStructuralError? _lastStructuralError;
    public CorkboardStructuralError? LastStructuralError
    {
        get => _lastStructuralError;
        private set => this.RaiseAndSetIfChanged(ref _lastStructuralError, value);
    }

    private CardDisplaySize _cardDisplaySize = CardDisplaySize.Large;
    public CardDisplaySize CardDisplaySize
    {
        get => _cardDisplaySize;
        private set => this.RaiseAndSetIfChanged(ref _cardDisplaySize, value);
    }

    public ReactiveCommand<CorkboardItemViewModel?, System.Reactive.Unit> SelectCardCommand { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> OpenSelectedCardCommand { get; }
    public ReactiveCommand<CorkboardItemViewModel?, System.Reactive.Unit> OpenCardCommand { get; }
    public ReactiveCommand<ReorderCardRequest, System.Reactive.Unit> ReorderCardCommand { get; }
    public ReactiveCommand<RenameCardRequest, System.Reactive.Unit> RenameCardCommand { get; }
    public ReactiveCommand<CreateChapterRequest, System.Reactive.Unit> CreateChapterCommand { get; }
    public ReactiveCommand<IncludeExistingChapterRequest, System.Reactive.Unit> IncludeExistingChapterCommand { get; }
    public ReactiveCommand<RemoveChapterRequest, System.Reactive.Unit> RemoveFromBookCommand { get; }
    public ReactiveCommand<DeleteChapterRequest, System.Reactive.Unit> DeleteChapterCommand { get; }
    public ReactiveCommand<CardDisplaySize, System.Reactive.Unit> SetCardSizeCommand { get; }
    public ReactiveCommand<PartDividerItemViewModel, System.Reactive.Unit> TogglePartExpandedCommand { get; }

    public CorkboardViewModel(
        WorkspaceViewModel workspace,
        IBookTxtStructureService structureService,
        INotificationService notificationService,
        ManuscriptService manuscriptService)
    {
        _workspace = workspace;
        _structureService = structureService;
        _notificationService = notificationService;
        _manuscriptService = manuscriptService;

        Items = new ReadOnlyObservableCollection<CorkboardItemViewModel>(_items);

        var nodeChanges = (INotifyCollectionChanged)_workspace.Nodes;
        Disposables.Add(
            Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                    handler => nodeChanges.CollectionChanged += handler,
                    handler => nodeChanges.CollectionChanged -= handler)
                .Subscribe(_ => RebuildItems()));

        Disposables.Add(_openChapterRequested);

        SelectCardCommand = ReactiveCommand.CreateFromTask<CorkboardItemViewModel?>(SelectCardAsync);
        OpenSelectedCardCommand = ReactiveCommand.CreateFromTask(OpenSelectedCardAsync,
            this.WhenAnyValue(x => x.SelectedCard).Select(card => card != null));
        OpenCardCommand = ReactiveCommand.CreateFromTask<CorkboardItemViewModel?>(OpenCardAsync);
        ReorderCardCommand = ReactiveCommand.CreateFromTask<ReorderCardRequest>(ReorderCardAsync);
        RenameCardCommand = ReactiveCommand.CreateFromTask<RenameCardRequest>(RenameCardAsync);
        CreateChapterCommand = ReactiveCommand.CreateFromTask<CreateChapterRequest>(CreateChapterAsync);
        IncludeExistingChapterCommand = ReactiveCommand.CreateFromTask<IncludeExistingChapterRequest>(IncludeExistingChapterAsync);
        RemoveFromBookCommand = ReactiveCommand.CreateFromTask<RemoveChapterRequest>(RemoveFromBookAsync);
        DeleteChapterCommand = ReactiveCommand.CreateFromTask<DeleteChapterRequest>(DeleteChapterAsync);
        SetCardSizeCommand = ReactiveCommand.Create<CardDisplaySize>(size => CardDisplaySize = size);
        TogglePartExpandedCommand = ReactiveCommand.Create<PartDividerItemViewModel>(TogglePartExpanded);

        Disposables.Add(SelectCardCommand.ThrownExceptions.Subscribe(ReportUnexpectedError));
        Disposables.Add(OpenSelectedCardCommand.ThrownExceptions.Subscribe(ReportUnexpectedError));
        Disposables.Add(OpenCardCommand.ThrownExceptions.Subscribe(ReportUnexpectedError));
        Disposables.Add(ReorderCardCommand.ThrownExceptions.Subscribe(ReportUnexpectedError));
        Disposables.Add(RenameCardCommand.ThrownExceptions.Subscribe(ReportUnexpectedError));
        Disposables.Add(CreateChapterCommand.ThrownExceptions.Subscribe(ReportUnexpectedError));
        Disposables.Add(IncludeExistingChapterCommand.ThrownExceptions.Subscribe(ReportUnexpectedError));
        Disposables.Add(RemoveFromBookCommand.ThrownExceptions.Subscribe(ReportUnexpectedError));
        Disposables.Add(DeleteChapterCommand.ThrownExceptions.Subscribe(ReportUnexpectedError));
        Disposables.Add(SetCardSizeCommand.ThrownExceptions.Subscribe(ReportUnexpectedError));
        Disposables.Add(TogglePartExpandedCommand.ThrownExceptions.Subscribe(ReportUnexpectedError));

        RebuildItems();
    }

    private void TogglePartExpanded(PartDividerItemViewModel part)
    {
        var path = part.RelativePath;
        var nextExpanded = !_partExpandedState.GetValueOrDefault(path, part.IsExpanded);
        _partExpandedState[path] = nextExpanded;
        part.IsExpanded = nextExpanded;
    }

    private void RebuildItems()
    {
        var selectedPath = SelectedCard?.RelativePath;

        CorkboardItemViewModel.DisposeItems(_items);
        _items.Clear();

        foreach (var item in CorkboardItemViewModel.Project(_workspace.Nodes, _partExpandedState))
            _items.Add(item);

        this.RaisePropertyChanged(nameof(HasItems));
        RestoreSelection(selectedPath);
    }

    private Task SelectCardAsync(CorkboardItemViewModel? item)
    {
        if (item is not ChapterCardItemViewModel cardItem)
            return Task.CompletedTask;

        SetSelectedCard(cardItem.Card);
        return Task.CompletedTask;
    }

    private Task OpenSelectedCardAsync()
    {
        if (SelectedCard == null)
            return Task.CompletedTask;

        _openChapterRequested.OnNext(SelectedCard.Chapter);
        return Task.CompletedTask;
    }

    private Task OpenCardAsync(CorkboardItemViewModel? item)
    {
        if (item is not ChapterCardItemViewModel cardItem)
            return Task.CompletedTask;

        SetSelectedCard(cardItem.Card);
        _openChapterRequested.OnNext(cardItem.Card.Chapter);
        return Task.CompletedTask;
    }

    private async Task ReorderCardAsync(ReorderCardRequest request)
    {
        if (!TryFindCurrentChapter(request.RelativePath, out _))
        {
            ReportStructuralFailure("Reorder card", request.RelativePath,
                $"Chapter '{request.RelativePath}' is not on the current board.");
            return;
        }

        if (!TryResolveReorderIndex(request, out var newIndex, out var error))
        {
            ReportStructuralFailure("Reorder card", request.RelativePath, error!);
            return;
        }

        if (!_workspace.HasWorkspace || string.IsNullOrWhiteSpace(_workspace.BookTxtPath))
        {
            ReportStructuralFailure("Reorder card", request.RelativePath, "No workspace is open.");
            return;
        }

        var previousSelectionPath = SelectedCard?.RelativePath;

        try
        {
            using var _ = _manuscriptService.SuppressFileWatcher();

            var result = await _structureService.ReorderEntryAsync(
                _workspace.BookTxtPath, request.RelativePath, newIndex);

            if (!result.IsSuccess)
            {
                ReportStructuralFailure("Reorder card", request.RelativePath,
                    result.Error ?? "Structural action failed.");
                return;
            }

            LastStructuralError = null;
            var reorderResult = await _workspace.ReorderNodesAsync();
            if (!reorderResult.IsSuccess)
            {
                ReportStructuralFailure("Reorder card", request.RelativePath,
                    reorderResult.Error ?? "Node reorder failed.");
                return;
            }

            RestoreSelection(previousSelectionPath);
        }
        catch (Exception ex)
        {
            ReportStructuralFailure("Reorder card", request.RelativePath, ex.Message);
        }
    }

    private async Task RenameCardAsync(RenameCardRequest request)
    {
        if (!TryFindCurrentChapter(request.ExistingPath, out _))
        {
            ReportStructuralFailure("Rename card", request.ExistingPath,
                $"Chapter '{request.ExistingPath}' is not on the current board.");
            return;
        }

        var selectionPath = SelectedCard?.RelativePath;
        if (string.Equals(selectionPath, request.ExistingPath, StringComparison.OrdinalIgnoreCase))
            selectionPath = request.ReplacementPath;

        await ExecuteStructuralOperationAsync(
            "Rename card",
            request.ExistingPath,
            () => _structureService.RenameEntryAsync(_workspace.BookTxtPath, request.ExistingPath, request.ReplacementPath),
            selectionPath);
    }

    private async Task CreateChapterAsync(CreateChapterRequest request)
    {
        await ExecuteStructuralOperationAsync(
            "Create chapter",
            request.ChapterPath,
            () => _structureService.CreateNewChapterAsync(_workspace.BookTxtPath, request.ChapterPath, request.Content, request.Index));
    }

    private async Task IncludeExistingChapterAsync(IncludeExistingChapterRequest request)
    {
        Func<Task<Result<Unit>>> action;

        if (!string.IsNullOrWhiteSpace(request.PartPath))
        {
            action = () => _structureService.AddExistingEntryAfterPartAsync(
                _workspace.BookTxtPath,
                request.ChapterPath,
                request.PartPath!);
        }
        else if (request.Index.HasValue)
        {
            action = () => _structureService.AddExistingEntryAsync(
                _workspace.BookTxtPath,
                request.ChapterPath,
                request.Index.Value);
        }
        else
        {
            ReportStructuralFailure(
                "Include chapter",
                request.ChapterPath,
                "Include chapter requires either an index or a part path.");
            return;
        }

        await ExecuteStructuralOperationAsync("Include chapter", request.ChapterPath, action);
    }

    private async Task RemoveFromBookAsync(RemoveChapterRequest request)
    {
        if (!TryFindCurrentChapter(request.ChapterPath, out _))
        {
            ReportStructuralFailure("Remove from book", request.ChapterPath,
                $"Chapter '{request.ChapterPath}' is not on the current board.");
            return;
        }

        await ExecuteStructuralOperationAsync(
            "Remove from book",
            request.ChapterPath,
            () => _structureService.RemoveEntryAsync(_workspace.BookTxtPath, request.ChapterPath));
    }

    private async Task DeleteChapterAsync(DeleteChapterRequest request)
    {
        if (!request.Confirmed)
        {
            ReportStructuralFailure(
                "Delete chapter",
                request.ChapterPath,
                "Delete chapter was rejected because confirmation was not provided.");
            return;
        }

        if (!TryFindCurrentChapter(request.ChapterPath, out _))
        {
            ReportStructuralFailure("Delete chapter", request.ChapterPath,
                $"Chapter '{request.ChapterPath}' is not on the current board.");
            return;
        }

        await ExecuteStructuralOperationAsync(
            "Delete chapter",
            request.ChapterPath,
            () => _structureService.DeleteChapterFileAsync(_workspace.BookTxtPath, request.ChapterPath));
    }

    private async Task ExecuteStructuralOperationAsync(
        string operation,
        string? path,
        Func<Task<Result<Unit>>> action,
        string? selectionPathOverride = null)
    {
        if (!_workspace.HasWorkspace || string.IsNullOrWhiteSpace(_workspace.BookTxtPath))
        {
            ReportStructuralFailure(operation, path, "No workspace is open.");
            return;
        }

        var previousSelectionPath = SelectedCard?.RelativePath;

        try
        {
            using var _ = _manuscriptService.SuppressFileWatcher();

            var result = await action();
            if (!result.IsSuccess)
            {
                ReportStructuralFailure(operation, path, result.Error ?? "Structural action failed.");
                return;
            }

            LastStructuralError = null;
            var reloadResult = await _workspace.ReloadCurrentWorkspaceAsync();
            if (!reloadResult.IsSuccess)
            {
                ReportStructuralFailure(operation, path, reloadResult.Error ?? "Workspace reload failed.");
                return;
            }

            RestoreSelection(selectionPathOverride ?? previousSelectionPath);
        }
        catch (Exception ex)
        {
            ReportStructuralFailure(operation, path, ex.Message);
        }
    }

    private void RestoreSelection(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            SetSelectedCard(null);
            return;
        }

        var match = _items
            .OfType<ChapterCardItemViewModel>()
            .Select(item => item.Card)
            .FirstOrDefault(card => string.Equals(card.RelativePath, relativePath, StringComparison.OrdinalIgnoreCase));

        SetSelectedCard(match);
    }

    private void SetSelectedCard(CardViewModel? card)
    {
        if (ReferenceEquals(_selectedCard, card))
            return;

        if (_selectedCard != null)
            _selectedCard.IsSelected = false;

        _selectedCard = card;

        if (_selectedCard != null)
            _selectedCard.IsSelected = true;

        this.RaisePropertyChanged(nameof(SelectedCard));
    }

    private bool TryFindCurrentChapter(string relativePath, out ChapterViewModel chapter)
    {
        chapter = _workspace.Nodes.FirstOrDefault(node =>
            node.Node.Kind == Hymnal.Core.Models.NodeKind.Chapter &&
            string.Equals(node.Node.RelativePath, relativePath, StringComparison.OrdinalIgnoreCase))!;
        return chapter != null;
    }

    private bool TryResolveReorderIndex(ReorderCardRequest request, out int newIndex, out string? error)
    {
        // Use ALL nodes (parts + chapters) so the computed index matches Book.txt's all-entries list.
        // A chapter-only index would be shifted by the number of part headers before the target.
        var chapters = _workspace.Nodes.ToList();

        var sourceIndex = chapters.FindIndex(node =>
            string.Equals(node.Node.RelativePath, request.RelativePath, StringComparison.OrdinalIgnoreCase));

        if (sourceIndex < 0)
        {
            newIndex = 0;
            error = $"Chapter '{request.RelativePath}' is not on the current board.";
            return false;
        }

        if (request.NewIndex.HasValue)
        {
            newIndex = request.NewIndex.Value;
            error = null;
            return true;
        }

        if (!string.IsNullOrWhiteSpace(request.AfterRelativePath))
        {
            var afterIndex = chapters.FindIndex(node =>
                string.Equals(node.Node.RelativePath, request.AfterRelativePath, StringComparison.OrdinalIgnoreCase));

            if (afterIndex < 0)
            {
                newIndex = 0;
                error = $"Chapter '{request.AfterRelativePath}' was not found on the current board.";
                return false;
            }

            newIndex = sourceIndex > afterIndex ? afterIndex + 1 : afterIndex;
            error = null;
            return true;
        }

        if (!string.IsNullOrWhiteSpace(request.BeforeRelativePath))
        {
            var beforeIndex = chapters.FindIndex(node =>
                string.Equals(node.Node.RelativePath, request.BeforeRelativePath, StringComparison.OrdinalIgnoreCase));

            if (beforeIndex < 0)
            {
                newIndex = 0;
                error = $"Chapter '{request.BeforeRelativePath}' was not found on the current board.";
                return false;
            }

            newIndex = sourceIndex < beforeIndex ? beforeIndex - 1 : beforeIndex;
            error = null;
            return true;
        }

        newIndex = 0;
        error = "Reorder card requires either a target index or a neighbor path.";
        return false;
    }

    private void ReportStructuralFailure(string operation, string? path, string message)
    {
        var bookTxtPath = _workspace.BookTxtPath;
        LastStructuralError = new CorkboardStructuralError(operation, path, message, bookTxtPath);

        var target = string.IsNullOrWhiteSpace(path)
            ? $"{operation} in '{bookTxtPath}'"
            : $"{operation} for '{path}' in '{bookTxtPath}'";
        _notificationService.ShowError($"{target}: {message}");
    }

    private void ReportUnexpectedError(Exception ex) => ReportStructuralFailure("Corkboard operation", null, ex.Message);

    public void Dispose()
    {
        CorkboardItemViewModel.DisposeItems(_items);
        Disposables.Dispose();
    }
}
