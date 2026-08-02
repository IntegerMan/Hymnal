---
estimated_steps: 1
estimated_files: 4
skills_used: []
---

# T02: Implement inline DatePicker UI and wiring

Implement a DatePicker flyout/popup in GanttView.axaml and wire it to a new `EditingRow` property in `GanttViewModel`. When a row is clicked, show the flyout positioned near the row.\n\n- Add properties to `GanttViewModel` to track the currently editing row (`EditingRow`, `EditStartDate`, `EditEndDate`).\n- Add a command `CommitEditCommand` to save the dates back to the underlying `ChapterViewModel`.\n- Inject `WorkspaceViewModel` (or a lookup func) into `GanttViewModel` so it can find the `ChapterViewModel` by path/UUID to update its PhaseData via `ChangeStatusAsync` or a new date-updating command on `ChapterViewModel`.\n- Update `ChapterViewModel` to have a `UpdateDatesAsync` command if necessary, or just call `UpsertAsync` on PhaseDataService directly from `GanttViewModel` or `ChapterViewModel`.\n- Add a `Flyout` or inline overlay in `GanttView.axaml` that binds to these properties.

## Inputs

- `src/Hymnal/Views/GanttView.axaml`
- `src/Hymnal/ViewModels/GanttViewModel.cs`
- `src/Hymnal/ViewModels/ChapterViewModel.cs`

## Expected Output

- `src/Hymnal/Views/GanttView.axaml`
- `src/Hymnal/Views/GanttView.axaml.cs`
- `src/Hymnal/ViewModels/GanttViewModel.cs`
- `src/Hymnal/ViewModels/ChapterViewModel.cs`

## Verification

dotnet test Hymnal.sln -nologo
