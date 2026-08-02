---
id: T02
parent: S03
milestone: M003
key_files:
  - src/Hymnal/ViewModels/GanttViewModel.cs
  - src/Hymnal/ViewModels/ChapterViewModel.cs
  - src/Hymnal/Views/GanttView.axaml
  - src/Hymnal/Views/GanttView.axaml.cs
  - src/Hymnal/App.axaml.cs
  - tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs
key_decisions:
  - Fixed-position top-right overlay used instead of row-positioned flyout — avoids sharing layout constants between GanttCanvas and GanttView while delivering the editing UX
  - DateTime? (not DateTimeOffset?) chosen for EditStartDate/EditEndDate to match CalendarDatePicker.SelectedDate type directly
  - UpdateDatesAsync in ChapterViewModel preserves existing Status (only replaces dates), consistent with the purpose of the Gantt date-edit feature
  - Removed Dispatcher.UIThread.Post from _rowEditRequested subscriber — EditDatesCommand.Execute fires synchronously on the UI thread from GanttCanvas.OnPointerPressed, so no dispatch is needed; also keeps tests synchronous
duration: 
verification_result: passed
completed_at: 2026-06-01T19:26:59.732Z
blocker_discovered: false
---

# T02: Added inline date-edit overlay to GanttView with CalendarDatePicker controls wired to GanttViewModel editing state and CommitEditCommand persisting via ChapterViewModel.UpdateDatesAsync

**Added inline date-edit overlay to GanttView with CalendarDatePicker controls wired to GanttViewModel editing state and CommitEditCommand persisting via ChapterViewModel.UpdateDatesAsync**

## What Happened

T02 built the full inline date-editing UI and wiring for the Gantt timeline:\n\n**GanttViewModel.cs** — Added INotificationService injection. Added four editing-state reactive properties (EditingRow, IsEditingDates, EditStartDate, EditEndDate). EditStartDate/EditEndDate are DateTime? so CalendarDatePicker binds directly. Added CommitEditCommand (async, persists dates) and CancelEditCommand (sync, clears state). A _rowEditRequested subscriber calls OpenEditForRow synchronously (same thread as EditDatesCommand.Execute, which fires on the UI thread from GanttCanvas.OnPointerPressed). CommitEditAsync finds the ChapterViewModel by RelativePath in workspace.Nodes, converts DateTime? to ISO string, delegates to ChapterViewModel.UpdateDatesAsync, then clears editing state using the CheckAccess pattern. Error surfacing goes through INotificationService.ShowError.\n\n**ChapterViewModel.cs** — Added public UpdateDatesAsync(string? startDate, string? endDate) method. Calls _phaseDataService.UpsertAsync preserving the existing Status, then calls ApplyPhaseData to sync the observable state. Consistent with the ChangeStatusAsync pattern.\n\n**GanttView.axaml** — Replaced the root Grid with a Panel so the edit overlay layers on top. Added a Border overlay (ZIndex=10, top-right position, Width=270) with chapter title, two CalendarDatePicker controls bound to EditStartDate/EditEndDate, and Save/Cancel buttons bound to CommitEditCommand/CancelEditCommand. Visibility driven by IsEditingDates. Using FallbackValue='' for EditingRow.Title null safety.\n\n**GanttView.axaml.cs** — Updated code-behind to subscribe to GanttViewModel.RowEditRequested via DataContextChanged, providing a hook point for future scroll-to-row behaviour. All overlay control is XAML-bound.\n\n**App.axaml.cs** — Injected INotificationService into GanttViewModel constructor.\n\n**GanttViewModelTests.cs** — Updated all 8 GanttViewModel constructor calls (new 3-arg signature). Added 5 new tests: IsEditingDates becomes true, EditStartDate/EndDate populated from row dates, null dates yield null edit dates, CancelEditCommand clears state, ChapterViewModel.UpdateDatesAsync persists and verifies via file read.\n\nKey deviation: The original plan mentioned positioning the flyout near the clicked row. A fixed top-right overlay was used instead — pixel-accurate row positioning would require layout constants shared between GanttCanvas and GanttView, adding complexity without meaningful UX benefit for this slice. The overlay is clearly associated with the row title it shows.

## Verification

dotnet build src/Hymnal/Hymnal.csproj -nologo: 0 errors, 0 warnings. dotnet test Hymnal.sln -nologo: 94/94 pass (89 existing + 5 new).

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build src/Hymnal/Hymnal.csproj -nologo` | 0 | ✅ pass | 7050ms |
| 2 | `dotnet test Hymnal.sln -nologo` | 0 | ✅ pass — 94/94 tests pass, 0 failures | 5000ms |

## Deviations

Flyout is a fixed top-right overlay rather than positioned near the clicked row. Precise row-Y positioning would require exposing layout constants (HeaderHeight, RowHeight) from GanttCanvas to GanttView, adding coupling. The overlay shows the chapter title so the user always knows which row is being edited.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal/ViewModels/GanttViewModel.cs`
- `src/Hymnal/ViewModels/ChapterViewModel.cs`
- `src/Hymnal/Views/GanttView.axaml`
- `src/Hymnal/Views/GanttView.axaml.cs`
- `src/Hymnal/App.axaml.cs`
- `tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs`
