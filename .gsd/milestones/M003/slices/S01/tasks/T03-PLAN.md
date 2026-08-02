---
estimated_steps: 1
estimated_files: 2
skills_used: []
---

# T03: Fix GanttCanvas missing-date rendering

Update the custom GanttCanvas render path so chapters still appear as muted rows or gaps when no valid phase date range exists. Preserve the time axis and row layout; any informational empty-state text must be additive, not a substitute for rendering rows. Add tests or rendering assertions that cover a workspace where all chapters lack valid start/end dates.

## Inputs

- `Existing GanttCanvas rendering logic`
- `Current GanttRowViewModel missing-date flags`

## Expected Output

- `Muted chapter rows still render when no valid dates exist`
- `Missing-date workspaces no longer collapse to an empty placeholder`

## Verification

dotnet test Hymnal.sln --filter GanttViewModelTests
