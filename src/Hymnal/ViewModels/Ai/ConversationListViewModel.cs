using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models.Ai;
using ReactiveUI;

namespace Hymnal.ViewModels.Ai;

/// <summary>
/// Manages the conversation list drawer: listing, searching, archiving, deleting, and renaming.
/// </summary>
public class ConversationListViewModel : ViewModelBase
{
    private readonly IConversationStore _store;
    private readonly INotificationService _notifications;

    // ── State ────────────────────────────────────────────────────────────────

    private string _workspaceRoot = string.Empty;

    private string _searchQuery = string.Empty;
    public string SearchQuery
    {
        get => _searchQuery;
        set => this.RaiseAndSetIfChanged(ref _searchQuery, value);
    }

    private bool _showArchived;
    public bool ShowArchived
    {
        get => _showArchived;
        set => this.RaiseAndSetIfChanged(ref _showArchived, value);
    }

    private bool _isSearching;
    public bool IsSearching
    {
        get => _isSearching;
        private set => this.RaiseAndSetIfChanged(ref _isSearching, value);
    }

    public ObservableCollection<ConversationListItemViewModel> Conversations { get; } = new();

    // ── Commands ─────────────────────────────────────────────────────────────

    public ReactiveCommand<ConversationListItemViewModel, Unit> ArchiveCommand { get; }
    public ReactiveCommand<ConversationListItemViewModel, Unit> UnarchiveCommand { get; }
    public ReactiveCommand<ConversationListItemViewModel, Unit> DeleteCommand { get; }
    public ReactiveCommand<(string Id, string NewTitle), Unit> RenameCommand { get; }

    /// <summary>Fired when the user selects a conversation. Payload is the conversation ID.</summary>
    public IObservable<string> ConversationSelected => _conversationSelected;
    private readonly System.Reactive.Subjects.Subject<string> _conversationSelected = new();

    // ── Constructor ──────────────────────────────────────────────────────────

    public ConversationListViewModel(IConversationStore store, INotificationService notifications)
    {
        _store = store;
        _notifications = notifications;

        ArchiveCommand = ReactiveCommand.CreateFromTask<ConversationListItemViewModel>(ArchiveAsync);
        UnarchiveCommand = ReactiveCommand.CreateFromTask<ConversationListItemViewModel>(UnarchiveAsync);
        DeleteCommand = ReactiveCommand.CreateFromTask<ConversationListItemViewModel>(DeleteAsync);
        RenameCommand = ReactiveCommand.CreateFromTask<(string, string)>(t => RenameAsync(t.Item1, t.Item2));

        Disposables.Add(ArchiveCommand.ThrownExceptions
            .Subscribe(ex => _notifications.ShowError($"Archive failed: {ex.Message}")));
        Disposables.Add(UnarchiveCommand.ThrownExceptions
            .Subscribe(ex => _notifications.ShowError($"Unarchive failed: {ex.Message}")));
        Disposables.Add(DeleteCommand.ThrownExceptions
            .Subscribe(ex => _notifications.ShowError($"Delete failed: {ex.Message}")));
        Disposables.Add(RenameCommand.ThrownExceptions
            .Subscribe(ex => _notifications.ShowError($"Rename failed: {ex.Message}")));
        Disposables.Add(_conversationSelected);

        // Re-filter when query or showArchived changes
        Disposables.Add(
            this.WhenAnyValue(x => x.SearchQuery, x => x.ShowArchived)
                .Throttle(TimeSpan.FromMilliseconds(300))
                .Subscribe(tuple => { var t = RefreshAsync(); }));
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void SetWorkspaceRoot(string workspaceRoot)
    {
        _workspaceRoot = workspaceRoot;
        _ = RefreshAsync();
    }

    public void SelectConversation(string id) => _conversationSelected.OnNext(id);

    public async Task RefreshAsync()
    {
        if (string.IsNullOrEmpty(_workspaceRoot)) return;

        try
        {
            List<ConversationMetadata> items;

            if (!string.IsNullOrWhiteSpace(_searchQuery))
            {
                IsSearching = true;
                var results = await _store.SearchAsync(_workspaceRoot, _searchQuery, CancellationToken.None)
                    .ConfigureAwait(false);
                items = results.Select(c => c.ToMetadata()).ToList();
            }
            else
            {
                IsSearching = false;
                var all = await _store.LoadIndexAsync(_workspaceRoot).ConfigureAwait(false);
                items = _showArchived
                    ? all.ToList()
                    : all.Where(m => !m.Archived).ToList();
            }

            items = items.OrderByDescending(m => m.UpdatedAt).ToList();

            Conversations.Clear();
            foreach (var meta in items)
                Conversations.Add(new ConversationListItemViewModel(meta));
        }
        catch (Exception ex)
        {
            _notifications.ShowError($"Failed to load conversations: {ex.Message}");
        }
        finally
        {
            IsSearching = false;
        }
    }

    // ── Mutation operations ───────────────────────────────────────────────────

    private async Task ArchiveAsync(ConversationListItemViewModel item)
    {
        var conv = await _store.LoadConversationAsync(_workspaceRoot, item.Id).ConfigureAwait(false);
        if (conv is null) return;
        var updated = conv with { Archived = true, UpdatedAt = DateTimeOffset.UtcNow };
        await _store.SaveConversationAsync(_workspaceRoot, updated).ConfigureAwait(false);
        await RefreshAsync().ConfigureAwait(false);
    }

    private async Task UnarchiveAsync(ConversationListItemViewModel item)
    {
        var conv = await _store.LoadConversationAsync(_workspaceRoot, item.Id).ConfigureAwait(false);
        if (conv is null) return;
        var updated = conv with { Archived = false, UpdatedAt = DateTimeOffset.UtcNow };
        await _store.SaveConversationAsync(_workspaceRoot, updated).ConfigureAwait(false);
        await RefreshAsync().ConfigureAwait(false);
    }

    private async Task DeleteAsync(ConversationListItemViewModel item)
    {
        await _store.DeleteConversationAsync(_workspaceRoot, item.Id).ConfigureAwait(false);
        await RefreshAsync().ConfigureAwait(false);
    }

    private async Task RenameAsync(string id, string newTitle)
    {
        if (string.IsNullOrWhiteSpace(newTitle)) return;
        var conv = await _store.LoadConversationAsync(_workspaceRoot, id).ConfigureAwait(false);
        if (conv is null) return;
        var updated = conv with { Title = newTitle.Trim() };
        await _store.SaveConversationAsync(_workspaceRoot, updated).ConfigureAwait(false);
        await RefreshAsync().ConfigureAwait(false);
    }
}
