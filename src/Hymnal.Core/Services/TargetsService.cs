using System.Text.Json;
using System.Text.Json.Serialization;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models;

namespace Hymnal.Core.Services;

public sealed class TargetsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly IMetadataStore _store;

    public TargetsService(IMetadataStore store)
    {
        _store = store;
    }

    // -------------------------------------------------------------------------
    // Load / Save
    // -------------------------------------------------------------------------

    public async Task<Dictionary<string, WordCountTarget>> LoadAsync(string workspaceRoot)
    {
        var path = TargetsPath(workspaceRoot);
        if (!File.Exists(path))
            return new Dictionary<string, WordCountTarget>();

        var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
        var doc = JsonSerializer.Deserialize<TargetsFile>(json, JsonOptions)
                  ?? throw new InvalidDataException("targets.json deserialized to null.");

        if (doc.SchemaVersion != 1)
            throw new InvalidDataException(
                $"targets.json has unsupported schemaVersion {doc.SchemaVersion}. Expected 1.");

        return doc.Targets ?? new Dictionary<string, WordCountTarget>();
    }

    public async Task SaveAsync(string workspaceRoot, Dictionary<string, WordCountTarget> targets)
    {
        var file = new TargetsFile { SchemaVersion = 1, Targets = targets };
        var json = JsonSerializer.Serialize(file, JsonOptions);
        await _store.WriteTextAtomicAsync(TargetsPath(workspaceRoot), json).ConfigureAwait(false);
    }

    // -------------------------------------------------------------------------
    // Operations
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the target for <paramref name="uuid"/>, or null if not found.
    /// </summary>
    public WordCountTarget? GetTarget(Dictionary<string, WordCountTarget> targets, string uuid)
    {
        return targets.TryGetValue(uuid, out var target) ? target : null;
    }

    /// <summary>
    /// Upserts (or removes when <paramref name="target"/> is null) the entry for
    /// <paramref name="uuid"/> under a file lock.
    /// </summary>
    public async Task UpsertAsync(string workspaceRoot, string uuid, WordCountTarget? target)
    {
        await WithWorkspaceLockAsync(workspaceRoot, async () =>
        {
            var targets = await LoadUnlockedAsync(workspaceRoot).ConfigureAwait(false);
            if (target is null)
                targets.Remove(uuid);
            else
                targets[uuid] = target;
            await SaveUnlockedAsync(workspaceRoot, targets).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    // -------------------------------------------------------------------------
    // Internal helpers
    // -------------------------------------------------------------------------

    private async Task<Dictionary<string, WordCountTarget>> LoadUnlockedAsync(string workspaceRoot)
    {
        var path = TargetsPath(workspaceRoot);
        if (!File.Exists(path))
            return new Dictionary<string, WordCountTarget>();

        var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
        var doc = JsonSerializer.Deserialize<TargetsFile>(json, JsonOptions)
                  ?? throw new InvalidDataException("targets.json deserialized to null.");

        if (doc.SchemaVersion != 1)
            throw new InvalidDataException(
                $"targets.json has unsupported schemaVersion {doc.SchemaVersion}. Expected 1.");

        return doc.Targets ?? new Dictionary<string, WordCountTarget>();
    }

    private async Task SaveUnlockedAsync(string workspaceRoot, Dictionary<string, WordCountTarget> targets)
    {
        var file = new TargetsFile { SchemaVersion = 1, Targets = targets };
        var json = JsonSerializer.Serialize(file, JsonOptions);
        await _store.WriteTextAtomicAsync(TargetsPath(workspaceRoot), json).ConfigureAwait(false);
    }

    private static async Task WithWorkspaceLockAsync(string workspaceRoot, Func<Task> action)
    {
        var lockPath = TargetsLockPath(workspaceRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(lockPath)!);
        using var lockStream = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        await action().ConfigureAwait(false);
    }

    private static string TargetsPath(string workspaceRoot) =>
        Path.Combine(workspaceRoot, ".hymnal-data", "targets.json");

    private static string TargetsLockPath(string workspaceRoot) =>
        Path.Combine(workspaceRoot, ".hymnal-data", "targets.lock");

    // -------------------------------------------------------------------------
    // Internal DTO for serialization
    // -------------------------------------------------------------------------

    private sealed class TargetsFile
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, WordCountTarget>? Targets { get; set; }
    }
}
