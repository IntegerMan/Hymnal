---
id: T02
parent: S02
milestone: M004
key_files:
  - src/Hymnal/ViewModels/EditorViewModel.cs
  - src/Hymnal/ViewModels/MainWindowViewModel.cs
  - tests/Hymnal.Core.Tests/ViewModels/EditorViewModelArbitraryFileTests.cs
key_decisions:
  - Arbitrary supplemental docs are represented in the editor by `ActiveFilePath != null`, `ActiveNode == null`, and `IsBookSelected == false` rather than by extending chapter node types.
  - Main window title logic falls back to `ActiveFilePath` for non-chapter documents, preserving chapter-only metadata updates behind `ActiveNode` checks.
duration: 
verification_result: mixed
completed_at: 2026-06-04T04:29:31.988Z
blocker_discovered: false
---

# T02: Added arbitrary supplemental-doc file lifecycle support to the single-buffer editor while keeping chapter identity null and saves on the shared atomic metadata-store path.

**Added arbitrary supplemental-doc file lifecycle support to the single-buffer editor while keeping chapter identity null and saves on the shared atomic metadata-store path.**

## What Happened

Implemented `EditorViewModel.OpenArbitraryFileAsync(string absolutePath)` to mirror chapter opening for plain files: it stops the existing watcher, reads file content, sets `Text` and `OriginalText`, sets `ActiveFilePath`, clears `ActiveNode`, Book.txt, missing-chapter, and conflict state, then starts the watcher. Updated editor visibility derivations so a non-null `ActiveFilePath` shows the editor and suppresses the no-chapter prompt even when `ActiveNode == null`. Preserved `SaveAsync` behavior so arbitrary docs save through `IMetadataStore.WriteTextAtomicAsync`, update `OriginalText`, emit `Saved`, clear conflicts, and restart the watcher. Refactored external-file-change state application into a private synchronous helper used by the existing watcher path, making clean reload and dirty conflict behavior testable without changing production watcher behavior. Updated `MainWindowViewModel` title logic to use `ActiveFilePath` as a filename fallback when there is no chapter node, so docs receive a normal filename title without being treated as chapters. Added focused `EditorViewModelArbitraryFileTests` covering open state, editor visibility, atomic save path, dirty doc to chapter save-before-switch via the workspace path, dirty chapter to doc save-before-switch through a small docs-VM-like test seam, external clean reload, external dirty conflict text, and AcceptExternal reload for arbitrary docs.

## Verification

Task-specific verification passed with `"/c/Program Files/dotnet/dotnet" test Hymnal.sln --filter EditorViewModelArbitraryFileTests` (7/7 passing). Named regression tests from the task inputs passed with `"/c/Program Files/dotnet/dotnet" test Hymnal.sln --filter "MainWindowPlanModeTests|CorkboardViewModelTests"` (14/14 passing). A full solution test run was also attempted and failed in pre-existing/unrelated Gantt and Corkboard projection/view tests; an isolated Gantt failure reproduces independently under its own filter, confirming it is outside this editor/docs task scope.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `gsd_exec bash: dotnet test Hymnal.sln --filter EditorViewModelArbitraryFileTests` | 127 | ⚠️ environment issue: gsd_exec could not find dotnet | 3452ms |
| 2 | `"/c/Program Files/dotnet/dotnet" test Hymnal.sln --filter EditorViewModelArbitraryFileTests` | 0 | ✅ pass: 7/7 EditorViewModelArbitraryFileTests | 5698ms |
| 3 | `"/c/Program Files/dotnet/dotnet" test Hymnal.sln --filter "MainWindowPlanModeTests|CorkboardViewModelTests"` | 0 | ✅ pass: 14/14 named regression tests | 5710ms |
| 4 | `"/c/Program Files/dotnet/dotnet" test Hymnal.sln` | 1 | ⚠️ unrelated existing failures in Gantt/Corkboard tests; task-specific tests pass | 15000ms |

## Deviations

`gsd_exec` could not locate `dotnet` in its bash environment (`/bin/bash: line 1: dotnet: command not found`), so verification used the local shell with the explicit Windows dotnet executable path `/c/Program Files/dotnet/dotnet`. Added a private `ApplyExternalFileChange` helper to make watcher conflict/reload behavior unit-testable without needing an Avalonia dispatcher pump.

## Known Issues

Full-suite `dotnet test Hymnal.sln` currently fails in unrelated Gantt/Corkboard tests, including `GanttViewModel_PartRow_SpansMinStartToMaxEndOfChildChapters`, `GanttCanvasTests.Measure_*`, `CorkboardViewSmokeTests.CorkboardView_LoadsXaml_AndTogglesEmptyStateWithDataContext`, and other Gantt projection/edit tests. The isolated Gantt test still fails under its own filter, so these are not caused by the arbitrary-file editor changes.

## Files Created/Modified

- `src/Hymnal/ViewModels/EditorViewModel.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/EditorViewModelArbitraryFileTests.cs`
