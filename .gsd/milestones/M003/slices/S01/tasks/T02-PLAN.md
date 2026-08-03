---
estimated_steps: 1
estimated_files: 6
skills_used: []
---

# T02: GanttCanvas custom control, GanttView AXAML shell, and F5 Gantt toggle wired into MainWindow with 80/80 tests passing

Create a custom Avalonia control `GanttCanvas` deriving from `Control` that overrides `Render(DrawingContext)`. Draw a simple time axis and chapter boxes based on `GanttRowViewModel` properties. Register dependency in `App.axaml.cs` and wire `GanttViewModel` to `MainWindowViewModel`. Add the UI elements to `MainWindow.axaml` (e.g., a new tab or toggleable view).

## Inputs

- `src/Hymnal/ViewModels/GanttRowViewModel.cs`
- `src/Hymnal/ViewModels/GanttViewModel.cs`
- `src/Hymnal/Views/Editor/ValidationMargin.cs`

## Expected Output

- `src/Hymnal/Views/GanttCanvas.cs`
- `src/Hymnal/Views/GanttView.axaml`
- `src/Hymnal/Views/GanttView.axaml.cs`

## Verification

dotnet build src/Hymnal/Hymnal.csproj -nologo
