---
estimated_steps: 4
estimated_files: 2
skills_used: []
---

# T01: Add Gantt reorder commands through WorkspaceViewModel

Task plan frontmatter: estimated_steps: 7; estimated_files: 2; skills_used: [tdd, verify-before-complete]

Why: S07 must make Gantt a consumer of the already-shipped sidebar/BookTxtStructureService reorder path, not create another Book.txt write path. R005 is advanced by making the Gantt timeline structurally editable; R013 is supported by proving another visual surface uses the canonical M005 structural path.

Do: Add explicit reorder methods/commands to `GanttViewModel` for accessible moves, e.g. selected-row move up/down and a method that accepts a dragged source row plus before/after target row. The implementation must build `ReorderCardRequest` values and execute `WorkspaceViewModel.ReorderChapterCommand`, because S04 already validated same-Part chapter legality, watcher suppression, canonical `IBookTxtStructureService.ReorderEntryAsync`, reload, and failure notifications there. Keep Gantt-specific validation shallow: reject book rollup rows, Part rollup rows, missing/non-editable rows, no-op drops, and absent targets before invoking the workspace command. Prefer row-neighbor paths (`BeforeRelativePath`/`AfterRelativePath`) over raw indexes so excluded projections and Book.txt active rows remain resolved by `WorkspaceViewModel`. Preserve or restore `SelectedRow` by source relative path after the workspace reload/rebuild when possible.

Done when: `GanttViewModel` exposes a small, testable row-reorder API/commands for keyboard and drag consumers; it never references `IBookTxtStructureService` directly; illegal Gantt row requests do not call the workspace reorder command; legal same-Part chapter moves call `WorkspaceViewModel.ReorderChapterCommand` with the expected `ReorderCardRequest` neighbor path.

## Inputs

- `src/Hymnal/ViewModels/GanttViewModel.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs`
- `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarReorderTests.cs`

## Expected Output

- `src/Hymnal/ViewModels/GanttViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs`

## Verification

powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter GanttViewModelTests --verbosity minimal"

## Observability Impact

Adds user-visible failure behavior by relying on WorkspaceViewModel's existing reorder notifications; tests should prove rejected Gantt inputs do not silently mutate order.
