using System.Text.Json;
using System.Text.Json.Serialization;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models.Ai;

namespace Hymnal.Core.Services.Ai;

/// <summary>
/// Persists conversations under {workspaceRoot}/.hymnal-data/conversations/.
/// index.json holds metadata; {uuid}.json holds full message content.
/// All writes go through IMetadataStore.WriteTextAtomicAsync.
/// </summary>
public sealed class ConversationStore : IConversationStore
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    private const int SchemaVersion = 1;
    private const int MaxSearchResults = 50;

    private readonly IMetadataStore _store;

    public ConversationStore(IMetadataStore store)
    {
        _store = store;
    }

    // ── Path helpers ───────────────────────────────────────────────────────

    private static string ConversationsDir(string workspaceRoot) =>
        Path.Combine(workspaceRoot, ".hymnal-data", "conversations");

    private static string IndexPath(string workspaceRoot) =>
        Path.Combine(ConversationsDir(workspaceRoot), "index.json");

    private static string ConversationPath(string workspaceRoot, string id) =>
        Path.Combine(ConversationsDir(workspaceRoot), $"{id}.json");

    // ── Index ──────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ConversationMetadata>> LoadIndexAsync(string workspaceRoot)
    {
        var path = IndexPath(workspaceRoot);
        if (!File.Exists(path))
            return Array.Empty<ConversationMetadata>();

        var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
        var envelope = JsonSerializer.Deserialize<IndexEnvelope>(json, _jsonOptions);
        if (envelope is null || envelope.SchemaVersion != SchemaVersion)
            return Array.Empty<ConversationMetadata>();

        if (envelope.Conversations is null) return Array.Empty<ConversationMetadata>();
        return envelope.Conversations;
    }

    public async Task SaveIndexEntryAsync(string workspaceRoot, ConversationMetadata entry)
    {
        var all = (await LoadIndexAsync(workspaceRoot).ConfigureAwait(false)).ToList();
        var idx = all.FindIndex(c => c.Id == entry.Id);
        if (idx >= 0)
            all[idx] = entry;
        else
            all.Add(entry);

        await WriteIndexAsync(workspaceRoot, all).ConfigureAwait(false);
    }

    private async Task WriteIndexAsync(string workspaceRoot, List<ConversationMetadata> entries)
    {
        var envelope = new IndexEnvelope(SchemaVersion, entries);
        var json = JsonSerializer.Serialize(envelope, _jsonOptions);
        await _store.WriteTextAtomicAsync(IndexPath(workspaceRoot), json).ConfigureAwait(false);
    }

    // ── Conversations ──────────────────────────────────────────────────────

    public async Task<Conversation?> LoadConversationAsync(string workspaceRoot, string conversationId)
    {
        var path = ConversationPath(workspaceRoot, conversationId);
        if (!File.Exists(path))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            var envelope = JsonSerializer.Deserialize<ConversationEnvelope>(json, _jsonOptions);
            if (envelope is null || envelope.SchemaVersion != SchemaVersion || envelope.Data is null)
                return null;
            return envelope.Data;
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveConversationAsync(string workspaceRoot, Conversation conversation)
    {
        var envelope = new ConversationEnvelope(SchemaVersion, conversation);
        var json = JsonSerializer.Serialize(envelope, _jsonOptions);
        await _store.WriteTextAtomicAsync(
            ConversationPath(workspaceRoot, conversation.Id), json).ConfigureAwait(false);

        // Keep index in sync
        await SaveIndexEntryAsync(workspaceRoot, conversation.ToMetadata()).ConfigureAwait(false);
    }

    public async Task DeleteConversationAsync(string workspaceRoot, string conversationId)
    {
        var filePath = ConversationPath(workspaceRoot, conversationId);
        if (File.Exists(filePath))
            File.Delete(filePath);

        var all = (await LoadIndexAsync(workspaceRoot).ConfigureAwait(false))
            .Where(c => c.Id != conversationId)
            .ToList();
        await WriteIndexAsync(workspaceRoot, all).ConfigureAwait(false);
    }

    // ── Search ─────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<Conversation>> SearchAsync(
        string workspaceRoot, string query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<Conversation>();

        var index = await LoadIndexAsync(workspaceRoot).ConfigureAwait(false);
        var candidates = index.Where(m => !m.Archived).ToList();

        var results = new List<Conversation>();

        foreach (var meta in candidates)
        {
            ct.ThrowIfCancellationRequested();

            // Tier 1: title match (no disk I/O)
            if (meta.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                var conv = await LoadConversationAsync(workspaceRoot, meta.Id).ConfigureAwait(false);
                if (conv is not null)
                    results.Add(conv);
                continue;
            }

            // Tier 2: full-text message scan
            var conversation = await LoadConversationAsync(workspaceRoot, meta.Id).ConfigureAwait(false);
            if (conversation is null) continue;

            if (conversation.Messages.Any(m =>
                    m.Content.Contains(query, StringComparison.OrdinalIgnoreCase)))
            {
                results.Add(conversation);
            }
        }

        return results
            .OrderByDescending(c => c.UpdatedAt)
            .Take(MaxSearchResults)
            .ToList();
    }

    // ── Private DTOs ──────────────────────────────────────────────────────

    private sealed record IndexEnvelope(int SchemaVersion, List<ConversationMetadata>? Conversations);

    private sealed record ConversationEnvelope(int SchemaVersion, Conversation? Data);
}
