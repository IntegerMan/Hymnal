---
id: T01
parent: S01
milestone: M002
key_files:
  - src/Hymnal.Core/Models/ChapterStatus.cs
  - src/Hymnal.Core/Models/PhaseData.cs
  - src/Hymnal.Core/Models/ChapterRegistryEntry.cs
  - src/Hymnal.Core/Services/ChapterRegistryService.cs
  - src/Hymnal.Core/Services/PhaseDataService.cs
key_decisions:
  - ChapterRegistryEntry and PhaseData are sealed classes (not records) — with expressions not supported, explicit constructors used in reconcile methods
  - InvalidDataException thrown on unknown schemaVersion so callers can surface via INotificationService.ShowError
  - JsonStringEnumConverter with camelCase policy used for status serialization
duration: 
verification_result: passed
completed_at: 2026-05-30T04:42:43.260Z
blocker_discovered: false
---

# T01: Added ChapterStatus enum, PhaseData/ChapterRegistryEntry models, ChapterRegistryService, and PhaseDataService to Hymnal.Core — build clean.

**Added ChapterStatus enum, PhaseData/ChapterRegistryEntry models, ChapterRegistryService, and PhaseDataService to Hymnal.Core — build clean.**

## What Happened

Created five files in Hymnal.Core with zero Avalonia dependencies. Models: ChapterStatus (6-value enum), PhaseData (Status + nullable ISO-8601 date strings), ChapterRegistryEntry (Uuid, CurrentPath, Orphaned). Services: ChapterRegistryService loads/saves chapter-registry.json (schemaVersion:1) via IMetadataStore, with AssignUuid, ReconcileRename, and ReconcileOrphans pure operations. PhaseDataService loads/saves phases.json (schemaVersion:1) with a static DefaultPhaseData returning Outlining. Both throw InvalidDataException on unknown schemaVersion. JSON options use camelCase, WhenWritingNull, and JsonStringEnumConverter throughout. One fix required: `with` expression on sealed class — replaced with explicit constructor calls.

## Verification

Ran `dotnet build src/Hymnal.Core/Hymnal.Core.csproj` — exit 0, 0 warnings, 0 errors in 3.11s.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build src/Hymnal.Core/Hymnal.Core.csproj` | 0 | ✅ pass | 3110ms |

## Deviations

ReconcileRename and ReconcileOrphans used `with` expressions which are invalid on sealed classes; fixed to explicit `new ChapterRegistryEntry { ... }` construction.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal.Core/Models/ChapterStatus.cs`
- `src/Hymnal.Core/Models/PhaseData.cs`
- `src/Hymnal.Core/Models/ChapterRegistryEntry.cs`
- `src/Hymnal.Core/Services/ChapterRegistryService.cs`
- `src/Hymnal.Core/Services/PhaseDataService.cs`
