# S03: Inline Date Editing

**Goal:** Implement inline date editing for chapters in the Gantt timeline by providing a popup date picker bound to ChapterViewModel PhaseData.
**Demo:** Users can click on phase dates in the Gantt view to edit them inline, persisting changes back to the registry.

## Must-Haves

- Users can click on a phase box in the Gantt chart to edit the dates inline.\n- The editing experience commits immediately or lets users clear dates.\n- Valid updates are persisted through ChapterViewModel and phase service.

## Proof Level

- This slice proves: operational

## Integration Closure

This slice completes the interactive portion of the Gantt View, wiring up the UI to update PhaseData on the ChapterViewModel and persist via PhaseDataService.

## Verification

- Minimal. Exceptions in command execution are surfaced via INotificationService.

## Tasks

- [x] **T01: Add hit testing to GanttCanvas** `est:1h`
  Modify GanttCanvas to handle hit-testing on rows and expose events or commands when a chapter row is clicked. Update GanttRowViewModel to expose a command for editing. We need to raise an event or execute a command passing the clicked GanttRowViewModel.\n\n- Add `PointerPressed` override in `GanttCanvas.cs`.\n- Calculate which row was clicked based on `Y` coordinate (`HeaderHeight`, `RowHeight`).\n- If it's a chapter row, invoke an `EditDatesCommand` on `GanttRowViewModel` or raise an event.\n- Extend `GanttRowViewModel` to include a `ReactiveCommand` for editing that gets routed back to `GanttViewModel`.
  - Files: `src/Hymnal/Views/GanttCanvas.cs`, `src/Hymnal/ViewModels/GanttRowViewModel.cs`, `src/Hymnal/ViewModels/GanttViewModel.cs`
  - Verify: dotnet test Hymnal.sln --filter GanttViewModelTests -nologo

- [x] **T02: Implement inline DatePicker UI and wiring** `est:2h`
  Implement a DatePicker flyout/popup in GanttView.axaml and wire it to a new `EditingRow` property in `GanttViewModel`. When a row is clicked, show the flyout positioned near the row.\n\n- Add properties to `GanttViewModel` to track the currently editing row (`EditingRow`, `EditStartDate`, `EditEndDate`).\n- Add a command `CommitEditCommand` to save the dates back to the underlying `ChapterViewModel`.\n- Inject `WorkspaceViewModel` (or a lookup func) into `GanttViewModel` so it can find the `ChapterViewModel` by path/UUID to update its PhaseData via `ChangeStatusAsync` or a new date-updating command on `ChapterViewModel`.\n- Update `ChapterViewModel` to have a `UpdateDatesAsync` command if necessary, or just call `UpsertAsync` on PhaseDataService directly from `GanttViewModel` or `ChapterViewModel`.\n- Add a `Flyout` or inline overlay in `GanttView.axaml` that binds to these properties.
  - Files: `src/Hymnal/Views/GanttView.axaml`, `src/Hymnal/Views/GanttView.axaml.cs`, `src/Hymnal/ViewModels/GanttViewModel.cs`, `src/Hymnal/ViewModels/ChapterViewModel.cs`
  - Verify: dotnet test Hymnal.sln -nologo

## Files Likely Touched

- src/Hymnal/Views/GanttCanvas.cs
- src/Hymnal/ViewModels/GanttRowViewModel.cs
- src/Hymnal/ViewModels/GanttViewModel.cs
- src/Hymnal/Views/GanttView.axaml
- src/Hymnal/Views/GanttView.axaml.cs
- src/Hymnal/ViewModels/ChapterViewModel.cs
