---
estimated_steps: 16
estimated_files: 5
skills_used: []
---

# T01: Core models, ChapterRegistryService, and PhaseDataService

**Why:** All M002 tracking (status, phase dates, word counts, targets) hangs off stable UUIDs. The Core layer must be pure .NET with zero Avalonia refs so it is independently testable.

**Do:**
1. Create `src/Hymnal.Core/Models/ChapterStatus.cs` — `public enum ChapterStatus { Outlining, Drafting, Editing, Polishing, Reviewing, Done }`.
2. Create `src/Hymnal.Core/Models/PhaseData.cs` — `public sealed class PhaseData { public ChapterStatus Status { get; init; } public string? PhaseStartDate { get; init; } public string? PhaseEndDate { get; init; } }` (ISO 8601 strings, nullable).
3. Create `src/Hymnal.Core/Models/ChapterRegistryEntry.cs` — `public sealed class ChapterRegistryEntry { public string Uuid { get; init; } = ""; public string CurrentPath { get; init; } = ""; public bool Orphaned { get; init; } }`.
4. Create `src/Hymnal.Core/Services/ChapterRegistryService.cs`. Constructor takes `IMetadataStore`. Persists `chapter-registry.json` at `{workspaceRoot}/.hymnal-data/chapter-registry.json`. Schema: `{ schemaVersion: 1, entries: { "<uuid>": { uuid, currentPath, orphaned } } }`. JSON options: `JsonStringEnumConverter`, `JsonIgnoreCondition.WhenWritingNull`, `PropertyNamingPolicy.CamelCase`. Public API:
   - `Task<Dictionary<string, ChapterRegistryEntry>> LoadAsync(string workspaceRoot)` — returns empty dict if file absent; returns `Result.Fail` equivalent by throwing `InvalidDataException` on unknown schemaVersion.
   - `Task SaveAsync(string workspaceRoot, Dictionary<string, ChapterRegistryEntry> entries)` — writes via `IMetadataStore.WriteTextAtomicAsync`.
   - `(string uuid, bool wasNew) AssignUuid(Dictionary<string, ChapterRegistryEntry> registry, string relativePath)` — finds existing entry by path or creates new `Guid.NewGuid().ToString()` entry; returns uuid and whether it was newly created.
   - `Dictionary<string, ChapterRegistryEntry> ReconcileRename(Dictionary<string, ChapterRegistryEntry> registry, string oldPath, string newPath)` — updates CurrentPath for matching entry.
   - `Dictionary<string, ChapterRegistryEntry> ReconcileOrphans(Dictionary<string, ChapterRegistryEntry> registry, IEnumerable<string> activePaths)` — sets Orphaned=true for paths no longer in activePaths; sets Orphaned=false if they reappear.
5. Create `src/Hymnal.Core/Services/PhaseDataService.cs`. Constructor takes `IMetadataStore`. Persists `phases.json` at `{workspaceRoot}/.hymnal-data/phases.json`. Schema: `{ schemaVersion: 1, phases: { "<uuid>": { status, phaseStartDate?, phaseEndDate? } } }`. Same JSON options as above. Public API:
   - `Task<Dictionary<string, PhaseData>> LoadAsync(string workspaceRoot)` — returns empty dict if absent; throws `InvalidDataException` on unknown schemaVersion.
   - `Task SaveAsync(string workspaceRoot, Dictionary<string, PhaseData> phases)` — atomic write.
   - `PhaseData DefaultPhaseData` — static or const returning `new PhaseData { Status = ChapterStatus.Outlining }`.

**Done when:** `dotnet build src/Hymnal.Core/Hymnal.Core.csproj` exits 0.

## Inputs

- `src/Hymnal.Core/Infrastructure/MetadataStore.cs`
- `src/Hymnal.Core/Interfaces/IMetadataStore.cs`
- `src/Hymnal.Core/Models/ChapterNode.cs`

## Expected Output

- `src/Hymnal.Core/Models/ChapterStatus.cs`
- `src/Hymnal.Core/Models/PhaseData.cs`
- `src/Hymnal.Core/Models/ChapterRegistryEntry.cs`
- `src/Hymnal.Core/Services/ChapterRegistryService.cs`
- `src/Hymnal.Core/Services/PhaseDataService.cs`

## Verification

dotnet build src/Hymnal.Core/Hymnal.Core.csproj

## Observability Impact

Unknown schemaVersion throws InvalidDataException with the actual version number in the message, surfaced by callers via INotificationService.ShowError.
