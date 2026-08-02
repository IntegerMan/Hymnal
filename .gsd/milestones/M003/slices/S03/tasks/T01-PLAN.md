---
estimated_steps: 1
estimated_files: 3
skills_used: []
---

# T01: Add hit testing to GanttCanvas

Modify GanttCanvas to handle hit-testing on rows and expose events or commands when a chapter row is clicked. Update GanttRowViewModel to expose a command for editing. We need to raise an event or execute a command passing the clicked GanttRowViewModel.\n\n- Add `PointerPressed` override in `GanttCanvas.cs`.\n- Calculate which row was clicked based on `Y` coordinate (`HeaderHeight`, `RowHeight`).\n- If it's a chapter row, invoke an `EditDatesCommand` on `GanttRowViewModel` or raise an event.\n- Extend `GanttRowViewModel` to include a `ReactiveCommand` for editing that gets routed back to `GanttViewModel`.

## Inputs

- `src/Hymnal/Views/GanttCanvas.cs`
- `src/Hymnal/ViewModels/GanttRowViewModel.cs`
- `src/Hymnal/ViewModels/GanttViewModel.cs`

## Expected Output

- `src/Hymnal/Views/GanttCanvas.cs`
- `src/Hymnal/ViewModels/GanttRowViewModel.cs`

## Verification

dotnet test Hymnal.sln --filter GanttViewModelTests -nologo
