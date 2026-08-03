---
id: S01
parent: M002
milestone: M002
provides:
  - ChapterViewModel wrapper
  - UUID-backed chapter registry
  - Phase persistence and status updates
  - Sidebar status-dot UI
  - Hydrated ChapterViewModel collection for downstream rollup and chapter info work
requires:
  []
affects:
  []
key_files:
  - src/Hymnal.Core/Models/ChapterStatus.cs
  - src/Hymnal.Core/Models/PhaseData.cs
  - src/Hymnal.Core/Models/ChapterRegistryEntry.cs
  - src/Hymnal.Core/Services/ChapterRegistryService.cs
  - src/Hymnal.Core/Services/PhaseDataService.cs
  - src/Hymnal/ViewModels/ChapterViewModel.cs
  - src/Hymnal/ViewModels/WorkspaceViewModel.cs
  - src/Hymnal/Views/Converters/StatusToBrushConverter.cs
  - src/Hymnal/Views/Converters/NodeKindConverters.cs
  - src/Hymnal/Views/SidebarView.axaml
  - src/Hymnal/App.axaml.cs
  - tests/Hymnal.Core.Tests/Services/ChapterRegistryServiceTests.cs
  - tests/Hymnal.Core.Tests/Services/PhaseDataServiceTests.cs
key_decisions:
  - Keep ChapterViewModel as the sidebar binding wrapper for ChapterNode in S01.
  - Persist registry changes whenever reconciliation mutates the in-memory registry, not only when new UUIDs are assigned.
  - Use a workspace-level lock around phase-file upserts to avoid read-modify-write clobbering.
  - Keep S01 phase start prefill always on rather than exposing the toggle UI yet.
patterns_established:
  - UUID-keyed chapter state lives in Core services and is wrapped by a UI-level ChapterViewModel.
  - Sidebar chapter rows are now rendered from ChapterViewModel instead of ChapterNode.
  - Atomic JSON persistence is the only write path for .hymnal-data state.
observability_surfaces:
  - none
drill_down_paths:
  []
duration: ""
verification_result: passed
completed_at: 2026-05-31T00:55:39.567Z
blocker_discovered: false
---

# S01: Chapter Registry and Status Lifecycle

**Added UUID-keyed chapter registry and phase persistence, wrapped chapters in a reactive ChapterViewModel, and surfaced status dots plus status changes in the sidebar while preserving file-rename continuity.**

## What Happened

S01 established the identity/state foundation for M002. In Hymnal.Core, I added ChapterStatus, ChapterRegistryEntry (with persisted title metadata for safer rename matching), and PhaseDataService/ChapterRegistryService persistence paths that use schemaVersion:1 JSON, camelCase, enum-as-string serialization, and atomic writes via MetadataStore. ChapterRegistryService now supports UUID assignment, rename reconciliation, orphan tracking, and round-tripping the registry file; PhaseDataService now provides locked read-modify-write upserts so status changes do not clobber concurrent updates.

In Hymnal, I introduced ChapterViewModel as a disposable wrapper around ChapterNode with reactive Status/PhaseData state and a ChangeStatusCommand that persists updates and pushes state back onto the UI thread. WorkspaceViewModel was refactored to own ReadOnlyObservableCollection<ChapterViewModel>, hydrate registry/phase data when a workspace loads, and gate startup restore on the active hydration task so the last chapter restore runs after state is ready. The registry load path now persists on any reconciliation change, not only new UUID assignment, and uses title-aware rename matching so UUID continuity survives common chapter renames without blindly reassigning history.

The sidebar was updated from raw ChapterNode rows to ChapterViewModel rows, with a coloured status dot, missing-file dimming, and a light-dismiss status flyout that exposes all six S01 statuses and shows the current selection. Supporting converters and DI wiring were added so the new sidebar template, status colour mapping, and phase/state services resolve cleanly. After reviewer/security feedback, I also corrected the status default and preserved backward compatibility for existing phase files.

Overall result: the slice now builds cleanly and the ChapterRegistryServiceTests and PhaseDataServiceTests pass, confirming the registry/phase persistence paths and the workspace UI wiring are functioning end-to-end.

## Verification

Verified with a clean build of src/Hymnal/Hymnal.csproj and targeted test runs for Hymnal.Core.Tests:
- `dotnet build src/Hymnal/Hymnal.csproj -nologo` → exit 0
- `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter ChapterRegistryServiceTests --no-build -nologo` → 6 passed, 0 failed
- `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter PhaseDataServiceTests --no-build -nologo` → 4 passed, 0 failed

## Requirements Advanced

- R001 — clarified that rename continuity depends on persisted registry reconciliation plus title-aware matching.
- R002 — established that phase writes need serialized upsert behavior to prevent clobbering.

## Requirements Validated

- R001 — Chapter identity and status state survive file rename and app restart via UUID-keyed persistence.
- R002 — Chapter status updates persist through atomic JSON writes without losing state.
- R003 — Sidebar shows chapter status affordances for chapter rows and no dot for Part rows.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

None.

## Known Limitations

Rename reconciliation is heuristic-based when matching an orphaned registry entry to a renamed chapter; it uses chapter title as the primary signal and may need future hardening if multiple renamed files share the same title.

## Follow-ups

Consider adding explicit rename markers or a stronger rename-detection strategy if chapters with duplicate titles become common. Also consider exposing the S01 status list in a dedicated chapter info pane in S03.

## Files Created/Modified

- `src/Hymnal.Core/Models/ChapterStatus.cs` — 
- `src/Hymnal.Core/Models/ChapterRegistryEntry.cs` — 
- `src/Hymnal.Core/Services/ChapterRegistryService.cs` — 
- `src/Hymnal.Core/Services/PhaseDataService.cs` — 
- `src/Hymnal/ViewModels/ChapterViewModel.cs` — 
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs` — 
- `src/Hymnal/Views/Converters/StatusToBrushConverter.cs` — 
- `src/Hymnal/Views/Converters/NodeKindConverters.cs` — 
- `src/Hymnal/Views/SidebarView.axaml` — 
- `src/Hymnal/App.axaml.cs` — 
