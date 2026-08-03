# S01: Gantt Canvas Renderer Foundation

**Goal:** Render a read-only Gantt tab showing a time axis and per-chapter phase boxes from saved phase metadata.
**Demo:** A new 'Gantt' tab/view is available, rendering a read-only time axis and phase boxes for all chapters based on their saved `PhaseData`.

## Must-Haves

- A new Gantt view/tab is available in the main shell.\n- Chapter phase boxes render against a visible time axis based on saved `PhaseData`.\n- Missing or invalid phase dates do not block rendering and show as muted rows or gaps.

## Proof Level

- This slice proves: integration

## Integration Closure

The Gantt tab is wired into the main shell via MainWindow.axaml and MainWindowViewModel, projecting state from WorkspaceViewModel without touching write paths.

## Verification

- Minimal. The view is a read-only projection; errors in date parsing are handled gracefully by producing gaps or muted rows rather than crashing.

## Tasks

- [x] **T01: Added GanttProjection (Core), GanttRowData, GanttRowViewModel, and GanttViewModel with 21 passing tests covering projection and strict ISO 8601 date parsing.** `est:1h`
  Add a `GanttRowViewModel` to represent a single row in the Gantt chart, tracking parsed start/end dates and missing date state. Add `GanttViewModel` to observe `WorkspaceViewModel.Nodes` and transform them into `GanttRowViewModel` instances, injecting `PhaseDataService` to load dates. Add focused unit tests for projection and date parsing.
  - Files: `src/Hymnal/ViewModels/GanttRowViewModel.cs`, `src/Hymnal/ViewModels/GanttViewModel.cs`, `tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs`
  - Verify: dotnet test Hymnal.sln --filter GanttViewModelTests

- [x] **T02: GanttCanvas custom control, GanttView AXAML shell, and F5 Gantt toggle wired into MainWindow with 80/80 tests passing** `est:2h`
  Create a custom Avalonia control `GanttCanvas` deriving from `Control` that overrides `Render(DrawingContext)`. Draw a simple time axis and chapter boxes based on `GanttRowViewModel` properties. Register dependency in `App.axaml.cs` and wire `GanttViewModel` to `MainWindowViewModel`. Add the UI elements to `MainWindow.axaml` (e.g., a new tab or toggleable view).
  - Files: `src/Hymnal/Views/GanttCanvas.cs`, `src/Hymnal/Views/GanttView.axaml`, `src/Hymnal/Views/GanttView.axaml.cs`, `src/Hymnal/ViewModels/MainWindowViewModel.cs`, `src/Hymnal/Views/MainWindow.axaml`, `src/Hymnal/App.axaml.cs`
  - Verify: dotnet build src/Hymnal/Hymnal.csproj -nologo

- [x] **T03: Fix GanttCanvas missing-date rendering** `est:1h`
  Update the custom GanttCanvas render path so chapters still appear as muted rows or gaps when no valid phase date range exists. Preserve the time axis and row layout; any informational empty-state text must be additive, not a substitute for rendering rows. Add tests or rendering assertions that cover a workspace where all chapters lack valid start/end dates.
  - Files: `src/Hymnal/Views/GanttCanvas.cs`, `tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs`
  - Verify: dotnet test Hymnal.sln --filter GanttViewModelTests

- [x] **T04: Refresh Gantt projection when phase data changes** `est:2h`
  Teach GanttViewModel to rebuild rows when existing ChapterViewModel phase metadata changes, not only when WorkspaceViewModel.Nodes changes. Subscribe to the chapter-level phase update signal or property change path already used by chapter editors, and add tests proving the Gantt rows update after ApplyPhaseData/save-date flows without a workspace reload.
  - Files: `src/Hymnal/ViewModels/GanttViewModel.cs`, `src/Hymnal/ViewModels/ChapterInfoViewModel.cs`, `tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs`
  - Verify: dotnet test Hymnal.sln --filter GanttViewModelTests

## Files Likely Touched

- src/Hymnal/ViewModels/GanttRowViewModel.cs
- src/Hymnal/ViewModels/GanttViewModel.cs
- tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs
- src/Hymnal/Views/GanttCanvas.cs
- src/Hymnal/Views/GanttView.axaml
- src/Hymnal/Views/GanttView.axaml.cs
- src/Hymnal/ViewModels/MainWindowViewModel.cs
- src/Hymnal/Views/MainWindow.axaml
- src/Hymnal/App.axaml.cs
- src/Hymnal/ViewModels/ChapterInfoViewModel.cs
