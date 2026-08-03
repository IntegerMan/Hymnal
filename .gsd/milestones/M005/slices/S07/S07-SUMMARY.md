---
id: S07
parent: M005
milestone: M005
provides:
  - A reusable Gantt reorder surface that follows the same canonical Book.txt mutation path already used by sidebar and Corkboard structure editing.
  - Focused regression coverage for Gantt drag intent, keyboard row moves, cross-Part rejection, and reload-persistent order projection.
requires:
  - slice: S01
    provides: Canonical Book.txt structure mutation service with rollback/reload semantics.
  - slice: S04
    provides: WorkspaceViewModel reorder command path, watcher suppression, and same-Part manuscript-order constraints used by Gantt as a thin consumer.
affects:
  - S08
key_files:
  - src/Hymnal/ViewModels/GanttViewModel.cs
  - src/Hymnal/Views/GanttCanvas.cs
  - src/Hymnal/Views/GanttView.axaml
  - src/Hymnal/Views/GanttView.axaml.cs
  - tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs
  - tests/Hymnal.Core.Tests/Views/GanttCanvasTests.cs
  - tests/Hymnal.Core.Tests/Views/GanttViewSmokeTests.cs
key_decisions:
  - Kept Gantt reorder as a thin consumer of `WorkspaceViewModel.ReorderChapterCommand` using `ReorderCardRequest`, with no direct `Book.txt` or `IBookTxtStructureService` dependency from the Gantt surface.
  - Scoped drag handling to editable chapter rows and rejected rollup, no-op, and cross-Part drag intents in `GanttCanvas` before the view delegates to the ViewModel.
patterns_established:
  - UI structural-edit surfaces should emit reorder intent and delegate to the shared workspace reorder command instead of introducing surface-specific persistence logic.
  - Focused desktop-surface tests can prove canonical-path delegation by combining ViewModel delegation tests, view/canvas smoke tests, and on-disk workspace persistence assertions.
observability_surfaces:
  - none
drill_down_paths:
  - .gsd/milestones/M005/slices/S07/tasks/T01-SUMMARY.md
  - .gsd/milestones/M005/slices/S07/tasks/T02-SUMMARY.md
  - .gsd/milestones/M005/slices/S07/tasks/T03-SUMMARY.md
duration: ""
verification_result: passed
completed_at: 2026-06-19T04:27:18.326Z
blocker_discovered: false
---

# S07: Gantt row drag reorder

**Added same-Part Gantt row drag and keyboard reorder as a thin consumer of the existing WorkspaceViewModel/Book.txt structural reorder path, with persistence and focused UI coverage proven.**

## What Happened

S07 extended the Gantt surface without introducing a second structure-write path. In `GanttViewModel`, keyboard move commands plus `MoveRowBeforeAsync`/`MoveRowAfterAsync` were added as thin adapters that validate included editable chapter rows, build `ReorderCardRequest` neighbor hints, and delegate into `WorkspaceViewModel.ReorderChapterCommand` rather than referencing `IBookTxtStructureService` directly. In `GanttCanvas`, row-drag intent detection was added for editable chapter rows only; it rejects header/out-of-bounds hits, book and Part rollups, no-op drops, and cross-Part drops before raising a reorder intent event. In `GanttView.axaml`/code-behind`, the canvas event was wired into the shared GanttViewModel reorder helpers and Alt+Up / Alt+Down row moves were added while preserving existing editing behavior and grid-selection sync. Focused tests cover the ViewModel delegation path, canvas drag-intent semantics, view wiring, keyboard handling, cross-Part rejection behavior, and an on-disk persistence proof showing the canonical Book.txt order changes and remains correct after reloading a fresh workspace projection. T04’s task summary artifact was missing due earlier auto-mode recovery, so slice closure re-ran the planned verification commands directly and used those fresh results as the canonical closeout evidence.

## Verification

Fresh closeout verification re-ran every slice-plan command through `gsd_exec` and all passed with exit code 0: (1) `powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter GanttViewModelTests --verbosity minimal"` — passed in 4412ms, covering WorkspaceViewModel delegation, blocked invalid row moves, cross-Part failure visibility, and persisted/reloaded Gantt order; (2) `powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter GanttCanvasTests --verbosity minimal"` — passed in 4392ms, covering legal row hit detection, before/after target placement, rollup rejection, no-op rejection, and same-Part drag constraints; (3) `powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter GanttViewSmokeTests --verbosity minimal"` — passed in 4326ms, covering XAML/code-behind wiring plus drag and Alt+arrow keyboard delegation into the shared reorder path; and (4) `powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test Hymnal.slnx --nologo --verbosity minimal"` — passed in 4338ms, proving the slice integrates cleanly with the current solution baseline. Reviewer pass found no blocker to closure, but did suggest future UX polish around adding a minimum drag threshold and a few extra blocked-path tests.

## Requirements Advanced

- R005 — Extended the Gantt timeline surface from read-only ordering to same-Part drag and keyboard row reorder while preserving the existing phase-grid behavior and reload-persistent projection.
- R013 — Completed the roadmap commitment that Gantt row reorder remains a thin consumer of the same canonical Book.txt structural edit path used by other manuscript-structure surfaces.

## Requirements Validated

None.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

T04’s auto-generated task summary was missing after recovery timeout, so slice closure used fresh direct re-runs of all four planned verification commands as the authoritative evidence set.

## Known Limitations

No blocking limitation found for S07 closure. Reviewer suggested future UX polish to add a minimum drag threshold and broaden a few blocked-path smoke tests, but same-Part reorder, canonical-path delegation, failure visibility, and reload persistence are all covered for this slice.

## Follow-ups

Consider folding a minimum drag-distance threshold and a few extra blocked-path view tests into S08 polish if manual desktop UAT shows accidental reorder sensitivity.

## Files Created/Modified

- `src/Hymnal/ViewModels/GanttViewModel.cs` — Added keyboard row-move commands and thin reorder delegation into WorkspaceViewModel via ReorderCardRequest.
- `src/Hymnal/Views/GanttCanvas.cs` — Added row drag intent detection and same-Part/no-op/rollup rejection before raising reorder events.
- `src/Hymnal/Views/GanttView.axaml` — Wired the Gantt canvas reorder event in the view markup.
- `src/Hymnal/Views/GanttView.axaml.cs` — Handled canvas reorder events and Alt+Up/Alt+Down keyboard moves while keeping selection in sync.
- `tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs` — Added focused delegation, cross-Part rejection, and persistence/reload coverage for Gantt row reorder.
- `tests/Hymnal.Core.Tests/Views/GanttCanvasTests.cs` — Added row drag intent and blocked-path tests for the canvas surface.
- `tests/Hymnal.Core.Tests/Views/GanttViewSmokeTests.cs` — Added smoke coverage for view wiring, drag delegation, and keyboard row moves.
