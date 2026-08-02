---
estimated_steps: 1
estimated_files: 3
skills_used: []
---

# T04: Refresh Gantt projection when phase data changes

Teach GanttViewModel to rebuild rows when existing ChapterViewModel phase metadata changes, not only when WorkspaceViewModel.Nodes changes. Subscribe to the chapter-level phase update signal or property change path already used by chapter editors, and add tests proving the Gantt rows update after ApplyPhaseData/save-date flows without a workspace reload.

## Inputs

- `WorkspaceViewModel.Nodes projection path`
- `ChapterViewModel.ApplyPhaseData update flow`

## Expected Output

- `Gantt rows refresh after in-session phase edits`
- `Projection stays aligned with saved PhaseData without requiring reload`

## Verification

dotnet test Hymnal.sln --filter GanttViewModelTests
