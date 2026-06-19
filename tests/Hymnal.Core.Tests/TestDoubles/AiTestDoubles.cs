using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Hymnal.Core.Common;
using Hymnal.Core.Infrastructure.Ai;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models.Ai;
using Hymnal.Core.Services.Ai;
using Hymnal.ViewModels;
using Hymnal.ViewModels.Ai;
using Microsoft.Extensions.AI;

namespace Hymnal.Core.Tests.TestDoubles;

/// <summary>
/// Thin synchronous stubs and fakes used in tests that need AI services.
/// </summary>
internal static class AiTestDoubles
{
    /// <summary>
    /// Creates a minimal AiChatViewModel wired to in-memory no-op fakes.
    /// Suitable for tests that construct MainWindowViewModel but don't exercise AI features.
    /// </summary>
    public static AiChatViewModel CreateStubAiChatViewModel(
        EditorViewModel editor,
        WorkspaceViewModel workspace,
        INotificationService notifications)
    {
        var chatSvc = new NoOpAiChatService();
        var convStore = new InMemoryConversationStore();
        var roleProvider = new RolePromptProvider();
        var reader = new NoOpManuscriptContextReader();
        var convList = new ConversationListViewModel(convStore, notifications);

        return new AiChatViewModel(
            chatSvc,
            convStore,
            editor,
            workspace,
            notifications,
            roleProvider,
            new WriteContextBuilder(reader),
            new PlanContextBuilder(reader),
            new ManageContextBuilder(reader),
            convList);
    }

    /// <summary>Creates a Settings view-model factory that returns a no-op instance.</summary>
    public static Func<SettingsViewModel> CreateStubSettingsFactory(INotificationService notifications)
    {
        return () => new SettingsViewModel(
            new InMemoryProviderProfileService(),
            new InMemoryCredentialStore(),
            new NoOpAiChatService(),
            notifications);
    }

    // ── In-memory fakes ──────────────────────────────────────────────────────

    public sealed class InMemoryConversationStore : IConversationStore
    {
        private readonly List<Conversation> _conversations = new();

        public Task<IReadOnlyList<ConversationMetadata>> LoadIndexAsync(string _) =>
            Task.FromResult<IReadOnlyList<ConversationMetadata>>(
                _conversations.Select(c => c.ToMetadata()).ToList());

        public Task<Conversation?> LoadConversationAsync(string _, string id) =>
            Task.FromResult(_conversations.FirstOrDefault(c => c.Id == id));

        public Task SaveConversationAsync(string _, Conversation c)
        {
            var i = _conversations.FindIndex(x => x.Id == c.Id);
            if (i >= 0) _conversations[i] = c; else _conversations.Add(c);
            return Task.CompletedTask;
        }

        public Task SaveIndexEntryAsync(string _, ConversationMetadata m) => Task.CompletedTask;

        public Task DeleteConversationAsync(string _, string id)
        {
            _conversations.RemoveAll(c => c.Id == id);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Conversation>> SearchAsync(string _, string q, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Conversation>>(
                _conversations.Where(c => c.Title.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList());
    }

    public sealed class NoOpAiChatService : IAiChatService
    {
        public bool IsProviderConfigured => false;
        public Task RefreshClientAsync() => Task.CompletedTask;

        public async IAsyncEnumerable<string> StreamAsync(
            IReadOnlyList<ChatMessage> _,
            [EnumeratorCancellation] CancellationToken ct)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    public sealed class InMemoryProviderProfileService : IProviderProfileService
    {
        private readonly List<ProviderProfile> _profiles = new();
        private string? _activeId;

        public Task<IReadOnlyList<ProviderProfile>> LoadAllAsync() =>
            Task.FromResult<IReadOnlyList<ProviderProfile>>(_profiles);

        public Task<ProviderProfile?> GetActiveAsync() =>
            Task.FromResult(_profiles.FirstOrDefault(p => p.Id == _activeId));

        public Task SaveAsync(ProviderProfile p)
        {
            var i = _profiles.FindIndex(x => x.Id == p.Id);
            if (i >= 0) _profiles[i] = p; else _profiles.Add(p);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id)
        {
            _profiles.RemoveAll(p => p.Id == id);
            return Task.CompletedTask;
        }

        public Task SetActiveAsync(string id) { _activeId = id; return Task.CompletedTask; }
        public Task<Result<Unit>> TestConnectionAsync(ProviderProfile _, string __, CancellationToken ct) =>
            Task.FromResult(Result<Unit>.Ok(Unit.Default));
    }

    public sealed class InMemoryCredentialStore : ICredentialStore
    {
        private readonly Dictionary<string, string> _store = new();
        public Task StoreAsync(string key, string value) { _store[key] = value; return Task.CompletedTask; }
        public Task<string?> RetrieveAsync(string key) => Task.FromResult(_store.TryGetValue(key, out var v) ? v : null);
        public Task DeleteAsync(string key) { _store.Remove(key); return Task.CompletedTask; }
    }

    public sealed class NoOpManuscriptContextReader : IManuscriptContextReader
    {
        public Task<string> ReadChapterTextAsync(string _, string __, CancellationToken ct) =>
            Task.FromResult(string.Empty);
        public Task<IReadOnlyList<(string RelativePath, string Title, bool IsPart)>> ReadBookOrderAsync(
            string _, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<(string, string, bool)>>(Array.Empty<(string, string, bool)>());
    }
}
