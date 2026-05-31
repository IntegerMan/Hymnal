using System.Text.Json;
using System.Text.Json.Serialization;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models;

namespace Hymnal.Core.Services;

public sealed class PhaseDataService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly IMetadataStore _store;

    public PhaseDataService(IMetadataStore store)
    {
        _store = store;
    }

    // -------------------------------------------------------------------------
    // Defaults
    // -------------------------------------------------------------------------

    public static PhaseData DefaultPhaseData => new() { Status = ChapterStatus.Outlining };

    // -------------------------------------------------------------------------
    // Load / Save
    // -------------------------------------------------------------------------

    public async Task<Dictionary<string, PhaseData>> LoadAsync(string workspaceRoot)
    {
        var path = PhasesPath(workspaceRoot);
        if (!File.Exists(path))
            return new Dictionary<string, PhaseData>();

        var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
        var doc = JsonSerializer.Deserialize<PhasesFile>(json, JsonOptions)
                  ?? throw new InvalidDataException("phases.json deserialized to null.");

        if (doc.SchemaVersion != 1)
            throw new InvalidDataException(
                $"phases.json has unsupported schemaVersion {doc.SchemaVersion}. Expected 1.");

        return doc.Phases ?? new Dictionary<string, PhaseData>();
    }

    public async Task SaveAsync(string workspaceRoot, Dictionary<string, PhaseData> phases)
    {
        var file = new PhasesFile { SchemaVersion = 1, Phases = phases };
        var json = JsonSerializer.Serialize(file, JsonOptions);
        await _store.WriteTextAtomicAsync(PhasesPath(workspaceRoot), json).ConfigureAwait(false);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static string PhasesPath(string workspaceRoot) =>
        Path.Combine(workspaceRoot, ".hymnal-data", "phases.json");

    // -------------------------------------------------------------------------
    // Internal DTO for serialization
    // -------------------------------------------------------------------------

    private sealed class PhasesFile
    {
        public int SchemaVersion { get; set; }
        public Dictionary<string, PhaseData>? Phases { get; set; }
    }
}
