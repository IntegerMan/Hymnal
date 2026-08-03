---
estimated_steps: 7
estimated_files: 4
skills_used: []
---

# T01: Compute Part rollups in GanttViewModel

1. Update `GanttProjection.Project` to support Part rollups. Wait, `GanttProjection.Project` maps a single node. Part rollups depend on child chapters. 
Actually, `GanttViewModel.RebuildRows()` handles the list logic. Update `GanttViewModel.RebuildRows()` to compute aggregated start and end dates, and completion state, for each Part node based on the subsequent Chapter nodes until the next Part.
2. In `GanttViewModel.cs`, when iterating `_workspace.Nodes`, if the node is a Part, instead of just projecting it directly, look ahead to find all its child chapters (chapters appearing after it and before the next Part).
3. Compute the `Min` of valid `StartDate` and `Max` of valid `EndDate` from those child chapters.
4. Calculate a `CompletionPercentage` (0.0 to 1.0) based on child chapter statuses (or word count, but currently we only have `ChapterStatus` readily available on `PhaseData`. The sketch mentions "weighted by total word count" but word count is not yet in the `ChapterNode` or `PhaseData` model. For this slice, implement a simple percentage of completed chapters (`Status == ChapterStatus.Done`), and add a TODO for word count weighting. Or add a `CompletionPercentage` to `GanttRowData`).
5. Update `GanttRowData` record and `GanttRowViewModel` to include `double CompletionPercentage` (default 0).
6. Update `GanttViewModelTests` to verify Part row aggregation (dates and completion).

## Inputs

- `src/Hymnal.Core/Models/GanttRowData.cs`
- `src/Hymnal/ViewModels/GanttRowViewModel.cs`
- `src/Hymnal/ViewModels/GanttViewModel.cs`
- `src/Hymnal.Core/Services/GanttProjection.cs`

## Expected Output

- `src/Hymnal.Core/Models/GanttRowData.cs`
- `src/Hymnal/ViewModels/GanttRowViewModel.cs`
- `src/Hymnal/ViewModels/GanttViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs`

## Verification

dotnet test Hymnal.sln --filter GanttViewModelTests -nologo
