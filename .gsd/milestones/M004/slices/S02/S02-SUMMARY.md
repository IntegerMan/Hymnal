---
id: S02
parent: M004
milestone: M004
provides:
  - A separate supplemental-docs subsystem that downstream slices can depend on for project reference material.
  - An editor path for arbitrary files that preserves the manuscript editor’s save and conflict behavior.
  - A verified DOCS sidebar/UI contract for future polish or Git-panel integration work.
requires:
  []
affects:
  []
key_files:
  - src/Hymnal.Core/Models/SupplementalDocNode.cs
  - src/Hymnal.Core/Interfaces/ISupplementalDocsService.cs
  - src/Hymnal.Core/Services/SupplementalDocsService.cs
  - src/Hymnal/ViewModels/EditorViewModel.cs
  - src/Hymnal/ViewModels/WorkspaceViewModel.cs
  - src/Hymnal/ViewModels/MainWindowViewModel.cs
  - src/Hymnal/ViewModels/SupplementalDocsViewModel.cs
  - src/Hymnal/App.axaml.cs
  - src/Hymnal/Views/SupplementalDocsView.axaml
  - src/Hymnal/Views/SupplementalDocsView.axaml.cs
  - src/Hymnal/Views/MainWindow.axaml
  - tests/Hymnal.Core.Tests/Services/SupplementalDocsServiceTests.cs
  - tests/Hymnal.Core.Tests/ViewModels/EditorViewModelArbitraryFileTests.cs
  - tests/Hymnal.Core.Tests/ViewModels/WorkspaceViewModelTests.cs
  - tests/Hymnal.Core.Tests/ViewModels/SupplementalDocsViewModelTests.cs
  - tests/Hymnal.Core.Tests/ViewModels/MainWindowSupplementalDocsTests.cs
  - tests/Hymnal.Core.Tests/Views/SupplementalDocsViewSmokeTests.cs
  - tests/Hymnal.Core.Tests/Views/MainWindowViewSmokeTests.cs
key_decisions:
  - Supplemental docs use a separate tree and dedicated view/model surface instead of extending ChapterNode or NodeKind.
  - Arbitrary doc files open in the existing editor with ActiveNode null and rely on the shared atomic metadata-store save path.
  - DOCS opening routes the shell into Write mode after the doc is loaded, keeping chapter and doc navigation aligned.
patterns_established:
  - Use a dedicated docs tree under .hymnal-data/docs for non-manuscript project material.
  - Keep docs editor lifecycle compatible with the existing single-buffer editor and dirty-state flow.
  - Use view-model/service integration plus smoke assertions for sidebar/editor wiring when headless UI harnesses are fragile.
observability_surfaces:
  - User-facing notification errors for invalid names, create/load/write failures, and failed switch-save operations.
  - DOCS sidebar tree state and editor `ActiveFilePath` / `ActiveNode` / dirty-state indicators.
  - On-disk `.hymnal-data/docs/` contents across save and reopen.
drill_down_paths:
  []
duration: ""
verification_result: passed
completed_at: 2026-06-04T05:29:59.862Z
blocker_discovered: false
---

# S02: Supplemental Docs Sidebar and Editor Path

**Added a separate supplemental-docs tree under .hymnal-data/docs, wired it into the DOCS sidebar and editor open/save path, and preserved persistence through reopen using the shared atomic metadata-store flow.**

## What Happened

T01 established the Core supplemental-docs contract as a separate tree rooted at .hymnal-data/docs with safe create/load behavior, keeping docs out of ChapterNode and NodeKind. T02 extended the existing single-buffer editor so arbitrary docs open with ActiveNode == null, ActiveFilePath set, dirty-state handling preserved, and saves routed through the shared atomic metadata-store path. T03 wired SupplementalDocsViewModel into workspace/main-window routing and DI so docs refresh with workspace changes and opening a doc switches the shell into Write mode without coupling docs into chapter projection. T04 added the dedicated DOCS sidebar surface with create-folder/create-file affordances and smoke coverage for create-open-save-reopen behavior, including AXAML wiring and command routing verification. Verification was done through the task-session evidence captured in the task summaries: focused SupplementalDocsService tests passed 9/9, editor arbitrary-file tests passed 7/7, SupplementalDocs ViewModel tests passed 16 tests, and the DOCS smoke tests passed alongside a successful build. The required full-solution run was also attempted and is still red in unrelated pre-existing Gantt/Corkboard tests; that baseline failure is outside S02 and does not affect the DOCS slice delivery.

## Verification

Focused verification for the slice passed at the task level: SupplementalDocsServiceTests 9/9 green, EditorViewModelArbitraryFileTests 7/7 green, SupplementalDocs tests 16/16 green, and SupplementalDocsViewSmokeTests green with a successful app build. The full `dotnet test Hymnal.sln` command still fails in unrelated existing Gantt/Corkboard tests, which were reproduced independently and are not caused by the DOCS changes. A gsd_exec attempt to run dotnet through the WSL bash lane reproduced the known NuGet path1 issue, so the accepted proof for .NET execution is the direct task-session evidence recorded in the task summaries.

## Requirements Advanced

None.

## Requirements Validated

- R007 — Validated by the DOCS service, editor arbitrary-file lifecycle, workspace/shell routing, and sidebar smoke coverage. The slice demonstrated create folder/file, open in editor with ActiveNode null, atomic save, and reopen persistence under .hymnal-data/docs/.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

Manual desktop smoke was not executed in this headless environment; the slice closes on automated service, ViewModel, and smoke evidence instead.

## Known Limitations

Full solution tests still fail in unrelated existing Gantt/Corkboard areas; those failures reproduce independently and are not introduced by S02.

## Follow-ups

None.

## Files Created/Modified

None.
