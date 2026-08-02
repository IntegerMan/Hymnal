---
estimated_steps: 4
estimated_files: 2
skills_used: []
---

# T02: Add GanttCanvas row drag intent events

Task plan frontmatter: estimated_steps: 8; estimated_files: 2; skills_used: [tdd, verify-before-complete]

Why: The sketch specifically calls out GanttCanvas drag handling. The canvas should only translate pointer gestures into row-reorder intent; mutation remains in `GanttViewModel` and `WorkspaceViewModel`.

Do: Extend `GanttCanvas` with a row reorder event args type that identifies the dragged source row, target row, and whether the drop is before or after the target based on the target row midpoint. Reuse the existing row geometry (`HeaderHeight`, `RowHeight`) and row list rather than duplicating layout math. Keep the drag scope narrow: only chapter rows that are editable can be drag sources and targets; book and Part rollup rows are not draggable or droppable for S07. Ignore pointer activity in the header or outside row bounds. Avoid Book.txt, workspace, filesystem, or service references in the canvas. Add focused `GanttCanvasTests` that exercise hit testing/drag intent without needing a desktop automation harness: source row detection, target midpoint before/after calculation, ignored book/Part rows, and no event for no-op drops.

Done when: `GanttCanvas` raises a deterministic row-reorder intent for legal chapter-row drags and remains a UI event source only; existing cell edit and tooltip behavior remain intact.

## Inputs

- `src/Hymnal/Views/GanttCanvas.cs`
- `src/Hymnal.Core/Models/GanttRowData.cs`
- `src/Hymnal/ViewModels/GanttRowViewModel.cs`
- `tests/Hymnal.Core.Tests/Views/GanttCanvasTests.cs`

## Expected Output

- `src/Hymnal/Views/GanttCanvas.cs`
- `tests/Hymnal.Core.Tests/Views/GanttCanvasTests.cs`

## Verification

powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter GanttCanvasTests --verbosity minimal"

## Observability Impact

No telemetry added; drag intent is inspectable through deterministic event args and focused smoke tests.
