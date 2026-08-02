---
id: S03
parent: M003
milestone: M003
provides:
  - Clickable chapter-row edit routing from the Gantt canvas.
  - Inline date-edit overlay with save/cancel behavior.
  - Persistence path from GanttViewModel into ChapterViewModel.UpdateDatesAsync.
requires:
  - slice: S01
    provides: Read-only Gantt canvas and chapter row rendering.
  - slice: S02
    provides: Part rollup rows and summary box layout.
affects:
  - M003/S04
  - M003/S05
key_files:
  - src/Hymnal/Views/GanttCanvas.cs
  - src/Hymnal/ViewModels/GanttRowViewModel.cs
  - src/Hymnal/ViewModels/GanttViewModel.cs
  - src/Hymnal/ViewModels/ChapterViewModel.cs
  - src/Hymnal/Views/GanttView.axaml
  - src/Hymnal/Views/GanttView.axaml.cs
  - src/Hymnal/App.axaml.cs
  - tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs
key_decisions:
  - All GanttRowViewModels expose EditDatesCommand, but only chapter rows are wired into the edit flow.
  - A fixed-position top-right overlay is preferable to a row-anchored flyout when the renderer owns the layout constants.
  - Chapter date updates must preserve existing chapter status and replace only the date fields.
patterns_established:
  - Keep UI edit triggers synchronous when they originate from the canvas pointer event path.
  - Use DateTime? in the view model when binding directly to CalendarDatePicker.SelectedDate.
  - Treat chapter metadata updates from planning views as narrowly scoped date mutations.
observability_surfaces:
  - none
drill_down_paths:
  []
duration: ""
verification_result: passed
completed_at: 2026-06-01T19:28:27.905Z
blocker_discovered: false
---

# S03: Inline Date Editing

**Added inline chapter date editing to the Gantt view so authors can click a chapter row, edit phase dates in an overlay, and persist the changes back to chapter metadata.**

## What Happened

S03 completed the interactive editing path for chapter rows in the Gantt timeline. T01 added hit-testing to GanttCanvas, introduced EditDatesCommand on GanttRowViewModel, and routed chapter-row clicks back into GanttViewModel while keeping Part rows inert. T02 added the editing state and commands in GanttViewModel, introduced ChapterViewModel.UpdateDatesAsync to persist date changes without altering status, and wired a CalendarDatePicker-based overlay into GanttView for save/cancel inline editing. The final wiring keeps the interaction synchronous on the UI thread, surfaces errors through INotificationService, and is covered by updated GanttViewModel tests for row selection, editing state, cancel behavior, and persistence.

## Verification

Task-level verification passed for both implementation tasks. T01 reported dotnet test Hymnal.sln --filter GanttViewModelTests -nologo with 30/30 pass, then dotnet test Hymnal.sln -nologo with 89/89 pass, 0 failures. T02 reported dotnet build src/Hymnal/Hymnal.csproj -nologo with 0 errors and 0 warnings, then dotnet test Hymnal.sln -nologo with 94/94 pass, 0 failures. The slice is also reflected in the completed task summaries for T01 and T02, and milestone status shows all S03 tasks complete.

## Requirements Advanced

- R005 — Added the inline date-edit interaction and persistence path required by the Gantt timeline capability.

## Requirements Validated

- R005 — T01 and T02 together prove authors can click a chapter row, edit dates inline, and persist the result back through the chapter metadata path while keeping the Gantt view responsive.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

Used a fixed top-right overlay instead of a row-positioned flyout to avoid coupling the view to canvas layout constants.

## Known Limitations

Only chapter rows are editable in this slice. Part rows remain read-only, and there is no drag-based editing or pixel-anchored popover positioning.

## Follow-ups

None.

## Files Created/Modified

- `src/Hymnal/Views/GanttCanvas.cs` — Added row hit-testing and pointer-click routing into the edit flow.
- `src/Hymnal/ViewModels/GanttRowViewModel.cs` — Added EditDatesCommand to row view models.
- `src/Hymnal/ViewModels/GanttViewModel.cs` — Added editing state, row selection routing, and commit/cancel behavior.
- `src/Hymnal/ViewModels/ChapterViewModel.cs` — Added UpdateDatesAsync to persist chapter phase dates without changing status.
- `src/Hymnal/Views/GanttView.axaml` — Added the inline editing overlay and CalendarDatePicker bindings.
- `src/Hymnal/Views/GanttView.axaml.cs` — Connected the view to row-edit notifications.
- `src/Hymnal/App.axaml.cs` — Updated dependency injection for the editing flow.
- `tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs` — Expanded tests for edit routing, editing state, cancel behavior, and persistence.
