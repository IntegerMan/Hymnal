---
id: T02
parent: S01
milestone: M002
key_files:
  - tests/Hymnal.Core.Tests/Services/ChapterRegistryServiceTests.cs
  - tests/Hymnal.Core.Tests/Services/PhaseDataServiceTests.cs
  - src/Hymnal.Core/Models/ChapterStatus.cs
  - src/Hymnal.Core/Models/PhaseData.cs
  - src/Hymnal.Core/Models/ChapterRegistryEntry.cs
  - src/Hymnal.Core/Services/ChapterRegistryService.cs
  - src/Hymnal.Core/Services/PhaseDataService.cs
key_decisions:
  - Used an inner RealFileMetadataStore (temp-rename) in tests that need real filesystem round-trips; all other cases use FakeMetadataStore/CaptureMetadataStore to stay in-memory.
  - T01 source files copied from main workspace into worktree — they were present but uncommitted to milestone/M002 branch.
duration: 
verification_result: passed
completed_at: 2026-05-30T19:14:23.648Z
blocker_discovered: false
---

# T02: Added 10 xUnit tests covering ChapterRegistryService (6) and PhaseDataService (4); all pass at exit 0

**Added 10 xUnit tests covering ChapterRegistryService (6) and PhaseDataService (4); all pass at exit 0**

## What Happened

T01 source files (ChapterStatus, PhaseData, ChapterRegistryEntry, ChapterRegistryService, PhaseDataService) existed in the main workspace but had not been committed to the milestone/M002 branch. They were copied into the worktree before writing tests.

ChapterRegistryServiceTests (6 cases):
- AssignUuid_NewPath_AssignsGuidAndMarksNew — first call for new path returns a valid GUID + wasNew=true
- AssignUuid_ExistingPath_ReturnsSameUuid — second call returns the same UUID + wasNew=false, no duplicate entries
- ReconcileOrphans_RemovesOrphanedEntries — path absent from activePaths → Orphaned=true
- ReconcileOrphans_RestoredEntry_ClearsOrphanFlag — path reappears in activePaths → Orphaned=false
- RoundTrip_SaveAndLoad_FidelityPreserved — save + reload via real temp directory, all fields match
- Load_UnknownSchemaVersion_ThrowsInvalidDataException — schemaVersion=99 throws InvalidDataException

PhaseDataServiceTests (4 cases):
- RoundTrip_SaveAndLoad_FidelityPreserved — Drafting + 2025-01-01 round-trips; PhaseEndDate stays null
- Load_AbsentFile_ReturnsEmptyDict — no phases.json → empty dictionary returned
- Load_UnknownSchemaVersion_ThrowsInvalidDataException — schemaVersion=99 throws InvalidDataException
- Save_NullPhaseEndDate_OmittedFromJson — verifies WhenWritingNull by asserting "phaseEndDate" absent in serialized JSON

Both test files use zero Avalonia dependencies. Temp directories are created and deleted in finally blocks. A RealFileMetadataStore inner class (temp-rename pattern) is used for round-trip tests; a FakeMetadataStore / CaptureMetadataStore is used for pure-unit cases.

## Verification

dotnet test --filter ChapterRegistryServiceTests: 6 passed, 0 failed, exit 0 (4.3 s)
dotnet test --filter PhaseDataServiceTests: 4 passed, 0 failed, exit 0 (3.0 s)

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter ChapterRegistryServiceTests --no-build` | 0 | ✅ pass — 6/6 passed | 4290ms |
| 2 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter PhaseDataServiceTests --no-build` | 0 | ✅ pass — 4/4 passed | 2963ms |

## Deviations

T01 source files (ChapterRegistryService, PhaseDataService, and the three model files) were not present in the worktree. They were copied from the uncommitted main-workspace files before writing tests. No plan changes were needed.

## Known Issues

None.

## Files Created/Modified

- `tests/Hymnal.Core.Tests/Services/ChapterRegistryServiceTests.cs`
- `tests/Hymnal.Core.Tests/Services/PhaseDataServiceTests.cs`
- `src/Hymnal.Core/Models/ChapterStatus.cs`
- `src/Hymnal.Core/Models/PhaseData.cs`
- `src/Hymnal.Core/Models/ChapterRegistryEntry.cs`
- `src/Hymnal.Core/Services/ChapterRegistryService.cs`
- `src/Hymnal.Core/Services/PhaseDataService.cs`
