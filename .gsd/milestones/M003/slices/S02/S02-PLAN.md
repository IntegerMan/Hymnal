# S02: Part Rollup Rows and Progress Fill

**Goal:** Show Part rows in the Gantt view as compact rollups that span their child chapters' date range and visualize overall completion with a weighted progress fill.
**Demo:** The Gantt view now includes Part nodes, showing summary phase boxes that span the min/max dates of their child chapters, along with progress fill.

## Must-Haves

- Part rows display a combined Start/EndDate from their valid child chapters and render with a completion fill. Stacked mini-lanes for overlapping child bars aren't strictly required in the model but rendering must not collapse overlaps into unreadable blobs.

## Proof Level

- This slice proves: contract

## Integration Closure

The `GanttCanvas` fully integrates Part nodes from the `ManuscriptModel` into the custom read-only timeline view with proper visual cues.

## Verification

- No new significant operational observability needed since all Gantt rendering failures are silently swallowed as per D015, and data loading is unchanged.

## Tasks

- [x] **T01: Compute Part rollups in GanttViewModel** `est:45m`
  1. Update `GanttProjection.Project` to support Part rollups. Wait, `GanttProjection.Project` maps a single node. Part rollups depend on child chapters. 
  Actually, `GanttViewModel.RebuildRows()` handles the list logic. Update `GanttViewModel.RebuildRows()` to compute aggregated start and end dates, and completion state, for each Part node based on the subsequent Chapter nodes until the next Part.
  2. In `GanttViewModel.cs`, when iterating `_workspace.Nodes`, if the node is a Part, instead of just projecting it directly, look ahead to find all its child chapters (chapters appearing after it and before the next Part).
  3. Compute the `Min` of valid `StartDate` and `Max` of valid `EndDate` from those child chapters.
  4. Calculate a `CompletionPercentage` (0.0 to 1.0) based on child chapter statuses (or word count, but currently we only have `ChapterStatus` readily available on `PhaseData`. The sketch mentions "weighted by total word count" but word count is not yet in the `ChapterNode` or `PhaseData` model. For this slice, implement a simple percentage of completed chapters (`Status == ChapterStatus.Done`), and add a TODO for word count weighting. Or add a `CompletionPercentage` to `GanttRowData`).
  5. Update `GanttRowData` record and `GanttRowViewModel` to include `double CompletionPercentage` (default 0).
  6. Update `GanttViewModelTests` to verify Part row aggregation (dates and completion).
  - Files: `src/Hymnal.Core/Models/GanttRowData.cs`, `src/Hymnal/ViewModels/GanttRowViewModel.cs`, `src/Hymnal/ViewModels/GanttViewModel.cs`, `tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs`
  - Verify: dotnet test Hymnal.sln --filter GanttViewModelTests -nologo

- [x] **T02: Render Part rollup boxes and progress fill in GanttCanvas** `est:45m`
  1. Update `GanttCanvas.cs` to render the aggregated dates and progress fill for Part rows.
  2. In `DrawPartRow`, if the part row has valid dates (start and end), draw a summary phase box matching those dates using the `EmptyStateBrush` or similar muted color.
  3. Draw the progress fill inside that box based on `row.CompletionPercentage` using a brighter color (e.g., `#34D399` for Done).
  4. Do not render stacked mini-lanes yet since we just need the rollup box and fill. The context mentions "When phase bars overlap within a Part, the layout uses stacked mini-lanes", but that implies rendering child phase bars inside the Part row. Let's instead draw a single rollup box with a progress fill for now to keep it compact, or draw mini bars. The sketch says: "Part rows display a progress fill based on total completion percentage... When phase bars overlap within a Part, the layout uses stacked mini-lanes so the bars remain readable".
  5. Add logic in `DrawPartRow` to draw the mini-lanes if we want to show child phases, but it's simpler to just draw the rollup span and progress fill first.
  6. The test for this is visual, but we can verify it compiles and runs.
  - Files: `src/Hymnal/Views/GanttCanvas.cs`
  - Verify: dotnet build src/Hymnal/Hymnal.csproj -nologo

## Files Likely Touched

- src/Hymnal.Core/Models/GanttRowData.cs
- src/Hymnal/ViewModels/GanttRowViewModel.cs
- src/Hymnal/ViewModels/GanttViewModel.cs
- tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs
- src/Hymnal/Views/GanttCanvas.cs
