---
estimated_steps: 4
estimated_files: 3
skills_used: []
---

# T03: Wire Gantt view drag and keyboard moves

Task plan frontmatter: estimated_steps: 9; estimated_files: 3; skills_used: [tdd, verify-before-complete]

Why: The slice demo requires the actual Gantt surface to initiate the reorder, and the sketch permits an equivalent accessible move command. Wiring must connect both the canvas gesture and keyboard move affordance to the same `GanttViewModel` command path.

Do: In `GanttView.axaml`, add accessible key bindings or row-level key handling for moving the selected editable chapter row up/down within its Part, for example Alt+Up and Alt+Down or Ctrl+Alt+Up/Ctrl+Alt+Down if existing shortcuts conflict. In `GanttView.axaml.cs`, route `GanttCanvas` row-reorder events from `TimelineCanvas` into the `GanttViewModel` reorder method added in T01. Ensure keyboard handling does not fire while an editing control is focused, following the existing `ChapterGrid_KeyDown` pattern for copy/paste. Keep DataGrid row selection synchronized enough that a successful keyboard move can preserve/reselect the moved row after rebuild. Add or extend a Gantt view smoke test that proves the XAML/code-behind exposes the expected handlers and that key routing ignores non-editable rows. If direct pointer simulation is fragile in Avalonia tests, assert the handler method can be invoked with constructed event args and that it calls the VM method rather than doing any filesystem work.

Done when: Both keyboard and GanttCanvas drag affordances call the same GanttViewModel reorder API; no Gantt view code calls `BookTxtStructureService`, writes files, or mutates `Book.txt` directly.

## Inputs

- `src/Hymnal/Views/GanttView.axaml`
- `src/Hymnal/Views/GanttView.axaml.cs`
- `src/Hymnal/Views/GanttCanvas.cs`
- `src/Hymnal/ViewModels/GanttViewModel.cs`
- `tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs`

## Expected Output

- `src/Hymnal/Views/GanttView.axaml`
- `src/Hymnal/Views/GanttView.axaml.cs`
- `tests/Hymnal.Core.Tests/Views/GanttViewSmokeTests.cs`

## Verification

powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter GanttViewSmokeTests --verbosity minimal"

## Observability Impact

Maintains failure visibility through the shared WorkspaceViewModel notification path; the view layer remains inspectable by smoke tests and contains no direct persistence path.
