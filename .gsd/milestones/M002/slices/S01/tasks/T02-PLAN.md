---
estimated_steps: 15
estimated_files: 2
skills_used: []
---

# T02: Unit tests: ChapterRegistryServiceTests and PhaseDataServiceTests

**Why:** Registry reconciliation and round-trip JSON fidelity are the key correctness invariants for UUID-keyed identity. Tests must be zero-Avalonia and run without a real filesystem (use temp directories).

**Do:**
1. Create `tests/Hymnal.Core.Tests/Services/ChapterRegistryServiceTests.cs`. Use `xUnit`. For `IMetadataStore` use a simple in-memory fake (captures last-written content in a dictionary). Test cases:
   - `AssignUuid_NewPath_AssignsGuidAndMarksNew` — first call for a new path returns a valid GUID string and wasNew=true.
   - `AssignUuid_ExistingPath_ReturnsSameUuid` — second call for the same path returns same uuid and wasNew=false.
   - `ReconcileOrphans_RemovesOrphanedEntries` — entry not in activePaths gets Orphaned=true.
   - `ReconcileOrphans_RestoredEntry_ClearsOrphanFlag` — entry that reappears in activePaths gets Orphaned=false.
   - `RoundTrip_SaveAndLoad_FidelityPreserved` — save a dict with one entry, load it back via a real temp file, assert all fields equal. Use `System.IO.Path.GetTempFileName` + cleanup.
   - `Load_UnknownSchemaVersion_ThrowsInvalidDataException` — write JSON with schemaVersion:99 to temp file, assert LoadAsync throws `InvalidDataException`.
2. Create `tests/Hymnal.Core.Tests/Services/PhaseDataServiceTests.cs`. Test cases:
   - `RoundTrip_SaveAndLoad_FidelityPreserved` — status=Drafting, phaseStartDate=2025-01-01, round-trips correctly.
   - `Load_AbsentFile_ReturnsEmptyDict` — no file at path, returns empty dictionary.
   - `Load_UnknownSchemaVersion_ThrowsInvalidDataException`.
   - `Save_NullPhaseEndDate_OmittedFromJson` — serialized JSON must not contain "phaseEndDate" when null (JsonIgnoreCondition.WhenWritingNull).

**Done when:** `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter ChapterRegistryServiceTests` exits 0 **and** `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter PhaseDataServiceTests` exits 0.

## Inputs

- `src/Hymnal.Core/Services/ChapterRegistryService.cs`
- `src/Hymnal.Core/Services/PhaseDataService.cs`
- `src/Hymnal.Core/Models/ChapterStatus.cs`
- `src/Hymnal.Core/Models/PhaseData.cs`
- `src/Hymnal.Core/Models/ChapterRegistryEntry.cs`
- `src/Hymnal.Core/Interfaces/IMetadataStore.cs`

## Expected Output

- `tests/Hymnal.Core.Tests/Services/ChapterRegistryServiceTests.cs`
- `tests/Hymnal.Core.Tests/Services/PhaseDataServiceTests.cs`

## Verification

dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter ChapterRegistryServiceTests && dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter PhaseDataServiceTests
