---
estimated_steps: 1
estimated_files: 3
skills_used: []
---

# T01: Added GanttProjection (Core), GanttRowData, GanttRowViewModel, and GanttViewModel with 21 passing tests covering projection and strict ISO 8601 date parsing.

Add a `GanttRowViewModel` to represent a single row in the Gantt chart, tracking parsed start/end dates and missing date state. Add `GanttViewModel` to observe `WorkspaceViewModel.Nodes` and transform them into `GanttRowViewModel` instances, injecting `PhaseDataService` to load dates. Add focused unit tests for projection and date parsing.

## Inputs

- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/ViewModels/ViewModelBase.cs`
- `src/Hymnal.Core/Models/PhaseData.cs`
- `src/Hymnal.Core/Services/PhaseDataService.cs`

## Expected Output

- `src/Hymnal/ViewModels/GanttRowViewModel.cs`
- `src/Hymnal/ViewModels/GanttViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs`

## Verification

dotnet test Hymnal.sln --filter GanttViewModelTests
