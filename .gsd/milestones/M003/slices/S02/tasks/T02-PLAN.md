---
estimated_steps: 6
estimated_files: 1
skills_used: []
---

# T02: Render Part rollup boxes and progress fill in GanttCanvas

1. Update `GanttCanvas.cs` to render the aggregated dates and progress fill for Part rows.
2. In `DrawPartRow`, if the part row has valid dates (start and end), draw a summary phase box matching those dates using the `EmptyStateBrush` or similar muted color.
3. Draw the progress fill inside that box based on `row.CompletionPercentage` using a brighter color (e.g., `#34D399` for Done).
4. Do not render stacked mini-lanes yet since we just need the rollup box and fill. The context mentions "When phase bars overlap within a Part, the layout uses stacked mini-lanes", but that implies rendering child phase bars inside the Part row. Let's instead draw a single rollup box with a progress fill for now to keep it compact, or draw mini bars. The sketch says: "Part rows display a progress fill based on total completion percentage... When phase bars overlap within a Part, the layout uses stacked mini-lanes so the bars remain readable".
5. Add logic in `DrawPartRow` to draw the mini-lanes if we want to show child phases, but it's simpler to just draw the rollup span and progress fill first.
6. The test for this is visual, but we can verify it compiles and runs.

## Inputs

- `src/Hymnal/Views/GanttCanvas.cs`
- `src/Hymnal/ViewModels/GanttRowViewModel.cs`

## Expected Output

- `src/Hymnal/Views/GanttCanvas.cs`

## Verification

dotnet build src/Hymnal/Hymnal.csproj -nologo
