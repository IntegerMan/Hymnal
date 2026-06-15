using System.Text.Json;
using System.Text.Json.Serialization;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models;

namespace Hymnal.Core.Services;

public sealed class WordCountHistoryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly IMetadataStore _store;

    public WordCountHistoryService(IMetadataStore store)
    {
        _store = store;
    }

    // -------------------------------------------------------------------------
    // Append
    // -------------------------------------------------------------------------

    /// <summary>
    /// Appends (or replaces) a word-count entry for the given (uuid, date) pair.
    /// Acquires a workspace-level lock to prevent concurrent writes.
    /// </summary>
    public async Task AppendAsync(string workspaceRoot, string uuid, string date, int wordCount)
    {
        await WithWorkspaceLockAsync(workspaceRoot, async () =>
        {
            var entries = await LoadUnlockedAsync(workspaceRoot).ConfigureAwait(false);

            // Remove any existing entry for the same (uuid, date) pair
            entries.RemoveAll(e =>
                string.Equals(e.Uuid, uuid, StringComparison.Ordinal) &&
                string.Equals(e.Date, date, StringComparison.Ordinal));

            entries.Add(new WordCountHistoryEntry { Uuid = uuid, Date = date, WordCount = wordCount });

            await SaveUnlockedAsync(workspaceRoot, entries).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    // -------------------------------------------------------------------------
    // Read
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns all recorded word-count history entries for the workspace.
    /// Safe to call without a write lock; the file is written atomically.
    /// </summary>
    public async Task<IReadOnlyList<WordCountHistoryEntry>> GetAllAsync(string workspaceRoot)
        => await LoadUnlockedAsync(workspaceRoot).ConfigureAwait(false);

    // -------------------------------------------------------------------------
    // Internal helpers
    // -------------------------------------------------------------------------

    private static async Task<List<WordCountHistoryEntry>> LoadUnlockedAsync(string workspaceRoot)
    {
        var path = HistoryPath(workspaceRoot);
        if (!File.Exists(path))
            return new List<WordCountHistoryEntry>();

        var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
        var doc = JsonSerializer.Deserialize<HistoryFile>(json, JsonOptions)
                  ?? throw new InvalidDataException("wordcount-history.json deserialized to null.");

        if (doc.SchemaVersion != 1)
            throw new InvalidDataException(
                $"wordcount-history.json has unsupported schemaVersion {doc.SchemaVersion}. Expected 1.");

        return doc.Entries ?? new List<WordCountHistoryEntry>();
    }

    private async Task SaveUnlockedAsync(string workspaceRoot, List<WordCountHistoryEntry> entries)
    {
        var file = new HistoryFile { SchemaVersion = 1, Entries = entries };
        var json = JsonSerializer.Serialize(file, JsonOptions);
        await _store.WriteTextAtomicAsync(HistoryPath(workspaceRoot), json).ConfigureAwait(false);
    }

    private static async Task WithWorkspaceLockAsync(string workspaceRoot, Func<Task> action)
    {
        var lockPath = HistoryLockPath(workspaceRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(lockPath)!);
        using var lockStream = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        await action().ConfigureAwait(false);
    }

    private static string HistoryPath(string workspaceRoot) =>
        Path.Combine(workspaceRoot, ".hymnal-data", "wordcount-history.json");

    private static string HistoryLockPath(string workspaceRoot) =>
        Path.Combine(workspaceRoot, ".hymnal-data", "wordcount-history.lock");

    // -------------------------------------------------------------------------
    // Internal DTO for serialization
    // -------------------------------------------------------------------------

    private sealed class HistoryFile
    {
        public int SchemaVersion { get; set; }
        public List<WordCountHistoryEntry>? Entries { get; set; }
    }
}
