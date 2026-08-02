# S07: Gantt row drag reorder

**Goal:** Add Gantt row reorder as a thin consumer of the canonical Book.txt structure path, limited to included chapter rows within the same Part, with an accessible keyboard move command and GanttCanvas drag handling wired through GanttViewModel and WorkspaceViewModel.
**Demo:** After this, dragging a Gantt row within a Part reorders chapters using the same Book.txt write path as sidebar and Corkboard.

## Must-Haves

- Gantt exposes an accessible row move command and a canvas drag row-reorder gesture for included chapter rows.
- Legal within-Part Gantt chapter moves call `WorkspaceViewModel.ReorderChapterCommand` / `ReorderCardRequest`, which in turn uses the S04 `BookTxtStructureService.ReorderEntryAsync` path; Gantt does not write `Book.txt` directly and does not depend on `IBookTxtStructureService`.
- Book rollup rows, Part rollup rows, missing/excluded/non-editable rows, no-op drops, and cross-Part chapter moves do not mutate order; cross-Part failures remain user-visible via the existing workspace notification path.
- A legal move changes `Book.txt` order and the Gantt row order remains correct after workspace reload.
- Focused Gantt ViewModel, GanttCanvas, and Gantt view smoke tests pass; full solution verification is attempted with native Windows dotnet.

## Proof Level

- This slice proves: Integration proof. Real desktop automation is not required in S07, but focused ViewModel/View smoke tests must exercise the real Gantt-to-Workspace command path and persistence must be proven against temp workspace files. S08 remains responsible for final cross-surface desktop UAT.

## Integration Closure

Consumes S01/S04 structural contracts: `WorkspaceViewModel.ReorderChapterCommand`, `ReorderCardRequest`, `BookTxtStructureService.ReorderEntryAsync`, watcher-suppressed reload, same-Part chapter legality, Part-block semantics, and user-visible failure notifications. Introduces no new Core structural service and no new persistence format. Remaining milestone work: S08 integrated desktop UAT across Sidebar, Corkboard, and Gantt on one workspace.

## Verification

- Runtime signals remain desktop notifications and unchanged/reloaded UI state. Failure localization comes from focused tests that assert no direct Gantt persistence path, unchanged `Book.txt` on rejected moves, existing WorkspaceViewModel error notifications, and reload-persistent Gantt order.

## Tasks

- [x] **T01: Add Gantt reorder commands through WorkspaceViewModel** `est:2h`
  Task plan frontmatter: estimated_steps: 7; estimated_files: 2; skills_used: [tdd, verify-before-complete]
  - Files: `src/Hymnal/ViewModels/GanttViewModel.cs`, `tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs`
  - Verify: powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter GanttViewModelTests --verbosity minimal"

- [x] **T02: Add GanttCanvas row drag intent events** `est:2h`
  Task plan frontmatter: estimated_steps: 8; estimated_files: 2; skills_used: [tdd, verify-before-complete]
  - Files: `src/Hymnal/Views/GanttCanvas.cs`, `tests/Hymnal.Core.Tests/Views/GanttCanvasTests.cs`
  - Verify: powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter GanttCanvasTests --verbosity minimal"

- [x] **T03: Wire Gantt view drag and keyboard moves** `est:2h`
  Task plan frontmatter: estimated_steps: 9; estimated_files: 3; skills_used: [tdd, verify-before-complete]
  - Files: `src/Hymnal/Views/GanttView.axaml`, `src/Hymnal/Views/GanttView.axaml.cs`, `tests/Hymnal.Core.Tests/Views/GanttViewSmokeTests.cs`
  - Verify: powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter GanttViewSmokeTests --verbosity minimal"

- [x] **T04: Prove Gantt reorder persistence and canonical path** `est:2h`
  Task plan frontmatter: estimated_steps: 8; estimated_files: 2; skills_used: [tdd, verify-before-complete]
  - Files: `tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs`, `tests/Hymnal.Core.Tests/Views/GanttCanvasTests.cs`, `tests/Hymnal.Core.Tests/Views/GanttViewSmokeTests.cs`
  - Verify: powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test Hymnal.slnx --nologo --verbosity minimal"

## Files Likely Touched

- src/Hymnal/ViewModels/GanttViewModel.cs
- tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs
- src/Hymnal/Views/GanttCanvas.cs
- tests/Hymnal.Core.Tests/Views/GanttCanvasTests.cs
- src/Hymnal/Views/GanttView.axaml
- src/Hymnal/Views/GanttView.axaml.cs
- tests/Hymnal.Core.Tests/Views/GanttViewSmokeTests.cs
