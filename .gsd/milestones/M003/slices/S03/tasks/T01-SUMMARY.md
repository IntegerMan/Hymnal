---
id: T01
parent: S03
milestone: M003
key_files:
  - src/Hymnal/Views/GanttCanvas.cs
  - src/Hymnal/ViewModels/GanttRowViewModel.cs
  - src/Hymnal/ViewModels/GanttViewModel.cs
  - tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs
key_decisions:
  - All GanttRowViewModels carry EditDatesCommand but only chapter rows are subscribed in GanttViewModel, keeping Part rows inert from the edit flow
  - DisposeWith not available on IDisposable here — used phaseSubscriptions.Add() directly instead
  - SpinWait.SpinUntil pattern used for ReactiveCommand async assertions, matching existing test convention
duration: 
verification_result: passed
completed_at: 2026-06-01T19:13:04.311Z
blocker_discovered: false
---

# T01: Added pointer hit-testing to GanttCanvas and EditDatesCommand routing through GanttViewModel.RowEditRequested

**Added pointer hit-testing to GanttCanvas and EditDatesCommand routing through GanttViewModel.RowEditRequested**

## What Happened

Three files were changed to wire up hit-testing and command routing for inline date editing:

**GanttRowViewModel.cs**: Added `ReactiveCommand<Unit, Unit> EditDatesCommand` initialized with `ReactiveCommand.Create(() => { })`. Added `using System.Reactive` and `using ReactiveUI`. The command is present on all rows (chapter and part) but only chapter rows are wired in GanttViewModel and the canvas.

**GanttViewModel.cs**: Added `using System.Reactive.Subjects`. Added a private `Subject<GanttRowViewModel> _rowEditRequested` field and exposed it as `public IObservable<GanttRowViewModel> RowEditRequested`. In `RebuildRows()`, after creating each `GanttRowViewModel`, chapter rows get a subscription wired: `phaseSubscriptions.Add(rowVm.EditDatesCommand.Subscribe(_ => _rowEditRequested.OnNext(captured)))`. Part rows are skipped. The subject is completed on dispose via `Disposables.Add(Disposable.Create(() => _rowEditRequested.OnCompleted()))`.

**GanttCanvas.cs**: Added `using Avalonia.Input`. Inserted `OnPointerPressed` override that hit-tests the Y coordinate against rows: subtracts `HeaderHeight`, divides by `RowHeight` to get row index, bounds-checks, then calls `row.EditDatesCommand.Execute().Subscribe()` only if `row.IsChapter`. All exceptions are swallowed to keep the canvas crash-free, consistent with the existing `Render` pattern.

**GanttViewModelTests.cs**: Added three new tests:
- `GanttRowViewModel_EditDatesCommand_ExposedAndExecutable` — verifies the command exists and emits when executed (uses SpinWait for async scheduler compatibility)
- `GanttViewModel_RowEditRequested_FiresWhenEditDatesCommandExecutedOnChapterRow` — end-to-end: creates workspace+GanttViewModel, subscribes to RowEditRequested, executes the command, asserts the correct row was emitted
- `GanttViewModel_RowEditRequested_DoesNotFireForPartRows` — ensures part rows do not propagate through RowEditRequested

Fix applied: `DisposeWith` extension was unavailable on `IDisposable` in this context; switched to direct `phaseSubscriptions.Add(subscription)`. Tests for async ReactiveCommand execution used `SpinWait.SpinUntil` to match the scheduling pattern established in the existing `GanttViewModel_RefreshesWhenChapterPhaseDataChanges` test.

## Verification

dotnet test Hymnal.sln --filter GanttViewModelTests -nologo: 30/30 pass (3 new + 27 existing Gantt tests). dotnet test Hymnal.sln -nologo: 89/89 pass, 0 failures.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test Hymnal.sln --filter GanttViewModelTests -nologo` | 0 | ✅ pass | 8000ms |
| 2 | `dotnet test Hymnal.sln -nologo` | 0 | ✅ pass | 9000ms |

## Deviations

None. The task plan was followed exactly: PointerPressed override in GanttCanvas, row index calculation from Y coordinate, EditDatesCommand on GanttRowViewModel, routing back to GanttViewModel.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal/Views/GanttCanvas.cs`
- `src/Hymnal/ViewModels/GanttRowViewModel.cs`
- `src/Hymnal/ViewModels/GanttViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs`
