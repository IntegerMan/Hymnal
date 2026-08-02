---
id: T02
parent: S01
milestone: M003
key_files:
  - src/Hymnal/Views/GanttCanvas.cs
  - src/Hymnal/Views/GanttView.axaml
  - src/Hymnal/Views/GanttView.axaml.cs
  - src/Hymnal/ViewModels/MainWindowViewModel.cs
  - src/Hymnal/App.axaml.cs
  - src/Hymnal/Views/MainWindow.axaml
key_decisions:
  - GanttCanvas receives rows via StyledProperty and subscribes to INotifyCollectionChanged for live invalidation
  - Phase boxes drawn with DrawRectangle(brush, pen, Rect, radiusX, radiusY) — Avalonia 12 has no RoundedRect overload on DrawingContext
  - IsVisible toggle uses Border wrappers in AXAML to avoid compiled-binding resolving against child DataContext instead of inherited MainWindowViewModel
  - GanttViewModel registered as singleton before MainWindowViewModel in DI (consistent with EditorViewModel/WorkspaceViewModel ordering rule)
duration: 
verification_result: passed
completed_at: 2026-06-01T18:25:01.316Z
blocker_discovered: false
---

# T02: GanttCanvas custom control, GanttView AXAML shell, and F5 Gantt toggle wired into MainWindow with 80/80 tests passing

**GanttCanvas custom control, GanttView AXAML shell, and F5 Gantt toggle wired into MainWindow with 80/80 tests passing**

## What Happened

Implemented three output files and wired the Gantt view into the main shell.

**GanttCanvas.cs** (`src/Hymnal/Views/GanttCanvas.cs`): Custom `Control` subclass overriding `Render(DrawingContext)`. Defines a StyledProperty `Rows` of type `IReadOnlyList<GanttRowViewModel>?`. Subscribes to `INotifyCollectionChanged` on the rows collection so any item add/remove/reset triggers `InvalidateVisual()`. Render path: computes date range from chapter rows with valid StartDate/EndDate, draws a month-labeled time axis with vertical grid lines, per-row labels in a fixed 180px column, and phase boxes using a synthwave-compatible palette (one color per ChapterStatus). Part rows render as muted section dividers. Missing-date chapter rows render a muted placeholder band. Empty-state message shown when no rows or no date data. All exceptions swallowed silently — view can never crash the host. Fixed a build error: Avalonia 12 `DrawingContext` does not accept `RoundedRect` overloads; replaced with `DrawRectangle(brush, pen, Rect, radiusX, radiusY)`.

**GanttView.axaml + .cs**: UserControl with a 48px header bar (GANTT label + status legend) and a `ScrollViewer` wrapping the `GanttCanvas`. Binds `Canvas.Rows` to `{Binding Rows}` from `GanttViewModel`. Code-behind is minimal (`InitializeComponent` only).

**DI wiring (App.axaml.cs)**: Registered `GanttViewModel` as `AddSingleton` before `MainWindowViewModel`. Passed it as a new constructor parameter.

**MainWindowViewModel**: Added `GanttViewModel` property, `IsGanttVisible` / `IsEditorVisible` pair, and `ToggleGanttCommand` (toggles the bool + raises `IsEditorVisible` change notification).

**MainWindow.axaml**: Added F5 KeyBinding, 'Toggle Gantt View' menu item under View. Replaced the bare `<views:EditorView Grid.Column="2" .../>` with a `<Panel>` containing two `<Border IsVisible="...">` children — one for the EditorView and one for the GanttView. Using Border wrappers rather than inline `IsVisible` on the views themselves avoids the Avalonia compiled-binding error where `IsVisible` would be resolved against the child's rebound DataContext instead of the inherited `MainWindowViewModel`.

## Verification

Build: `dotnet build src/Hymnal/Hymnal.csproj -nologo` → 0 errors, 0 warnings (besides the .NET 10 preview notice). Tests: `dotnet test Hymnal.sln --no-build -nologo` → 80 passed, 0 failed (up from 31 baseline; T01's 21 Gantt tests included in the 80).

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build src/Hymnal/Hymnal.csproj -nologo` | 0 | ✅ pass | 6940ms |
| 2 | `dotnet test Hymnal.sln --no-build -nologo` | 0 | ✅ pass — 80/80 passed | 3200ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal/Views/GanttCanvas.cs`
- `src/Hymnal/Views/GanttView.axaml`
- `src/Hymnal/Views/GanttView.axaml.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal/App.axaml.cs`
- `src/Hymnal/Views/MainWindow.axaml`
