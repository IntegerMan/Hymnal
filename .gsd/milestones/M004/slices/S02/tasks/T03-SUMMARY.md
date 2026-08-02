---
id: T03
parent: S02
milestone: M004
key_files:
  - src/Hymnal/ViewModels/SupplementalDocsViewModel.cs
  - src/Hymnal/ViewModels/WorkspaceViewModel.cs
  - src/Hymnal/ViewModels/MainWindowViewModel.cs
  - src/Hymnal/App.axaml.cs
  - tests/Hymnal.Core.Tests/ViewModels/SupplementalDocsViewModelTests.cs
  - tests/Hymnal.Core.Tests/ViewModels/MainWindowSupplementalDocsTests.cs
  - tests/Hymnal.Core.Tests/ViewModels/MainWindowPlanModeTests.cs
key_decisions:
  - Supplemental docs remain represented by SupplementalDocNode/SupplementalDocsViewModel and are not added to WorkspaceViewModel.Nodes, ChapterNode, or NodeKind.
  - Workspace-to-docs refresh uses a narrow WorkspaceChanged observable rather than coupling docs into chapter reload internals.
  - Doc-open routing uses SupplementalDocsViewModel.DocumentOpened to request ShellMode.Write from MainWindowViewModel after OpenArbitraryFileAsync succeeds.
duration: 
verification_result: mixed
completed_at: 2026-06-04T04:40:23.777Z
blocker_discovered: false
---

# T03: Wired supplemental documents into workspace state and shell navigation with a dedicated docs ViewModel, DI registration, save-before-switch behavior, and Write-mode routing.

**Wired supplemental documents into workspace state and shell navigation with a dedicated docs ViewModel, DI registration, save-before-switch behavior, and Write-mode routing.**

## What Happened

Added SupplementalDocsViewModel as the DOCS sidebar ViewModel backed by ISupplementalDocsService and the existing editor arbitrary-file path. The ViewModel exposes a read-only tree of SupplementalDocNode entries, refresh/create/open commands, selected-node tracking, notification-backed failure handling, save-before-open guards, and a DocumentOpened observable used by the shell. WorkspaceViewModel now exposes a narrow WorkspaceChanged signal for open/reload/close refreshes and a ClearChapterSelectionForExternalDocument method that clears manuscript selection without triggering chapter navigation. MainWindowViewModel now owns SupplementalDocsViewModel and switches to ShellMode.Write when a doc opens, matching corkboard open-to-Write routing. App.axaml.cs now registers ISupplementalDocsService and SupplementalDocsViewModel while preserving EditorViewModel-before-WorkspaceViewModel ordering. Added focused ViewModel tests for docs refresh, create folder/file, invalid create notifications, dirty chapter save before doc open, failed save abort behavior, dirty doc save before chapter switch, and MainWindow Write-mode routing.

## Verification

Ran the required focused verification command `dotnet test Hymnal.sln --filter SupplementalDocs`; it passed with 16 tests, 0 failures. Also attempted full solution regression with `dotnet test Hymnal.sln`; it failed in unrelated existing Gantt/Corkboard UI/view-model tests, while SupplementalDocs tests remained green. `gsd_exec` was also attempted first per execution-lane instruction, but this harness could not resolve `dotnet`, matching the known project gotcha; actionable verification used the normal shell.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test Hymnal.sln --filter SupplementalDocs` | 0 | ✅ pass — 16 SupplementalDocs tests passed, 0 failed | 609ms |
| 2 | `dotnet test Hymnal.sln` | 1 | ⚠️ partial regression check — unrelated Gantt/Corkboard tests failed; SupplementalDocs coverage not implicated | 11000ms |
| 3 | `gsd_exec bash: dotnet test Hymnal.sln --filter SupplementalDocs` | 0 | ⚠️ harness diagnostic only — gsd_exec shell reported /bin/bash: dotnet: command not found, so output was not a valid test run | 3445ms |

## Deviations

Added `WorkspaceChanged` and `ClearChapterSelectionForExternalDocument` as the narrow public WorkspaceViewModel seams needed for docs refresh and external-document selection clearing. No AXAML sidebar polish was added because the task scope was ViewModel/DI-level wiring.

## Known Issues

Full `dotnet test Hymnal.sln` currently fails in existing non-Supplemental areas: GanttCanvasTests, multiple GanttViewModelTests, CorkboardViewSmokeTests, and CorkboardProjectionTests. Focused SupplementalDocs verification passes. Existing xUnit analyzer warnings remain in pre-existing CorkboardViewModelTests.

## Files Created/Modified

- `src/Hymnal/ViewModels/SupplementalDocsViewModel.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal/App.axaml.cs`
- `tests/Hymnal.Core.Tests/ViewModels/SupplementalDocsViewModelTests.cs`
- `tests/Hymnal.Core.Tests/ViewModels/MainWindowSupplementalDocsTests.cs`
- `tests/Hymnal.Core.Tests/ViewModels/MainWindowPlanModeTests.cs`
