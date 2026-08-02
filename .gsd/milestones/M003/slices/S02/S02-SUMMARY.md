---
id: S02
parent: M003
milestone: M003
provides:
  - Part rollup rows in the Gantt model.
  - CompletionPercentage on Gantt rows for downstream rendering.
  - A clipped summary-box rendering path for Part rows.
requires:
  []
affects:
  []
key_files:
  - src/Hymnal.Core/Models/GanttRowData.cs
  - src/Hymnal/ViewModels/GanttRowViewModel.cs
  - src/Hymnal/ViewModels/GanttViewModel.cs
  - src/Hymnal/Views/GanttCanvas.cs
  - tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs
key_decisions:
  - Keep Part rollup aggregation in GanttViewModel rather than extending the single-node projection helper.
  - Expose CompletionPercentage on the row model and clip the renderer’s fill to the rollup rect.
patterns_established:
  - Group-manufacturing logic belongs in the view model, while the renderer stays projection-only.
  - Use a clipped progress fill and space-aware label suppression for compact timeline summary rows.
observability_surfaces:
  - none
drill_down_paths:
  []
duration: ""
verification_result: passed
completed_at: 2026-06-01T19:04:59.533Z
blocker_discovered: false
---

# S02: Part Rollup Rows and Progress Fill

**Part rows in the Gantt view now roll up their child chapter date span and show weighted-looking completion via a clipped progress fill.**

## What Happened

GanttViewModel now treats Part nodes as rollup rows instead of simple one-node projections: it scans forward through the chapter sequence until the next Part, aggregates the earliest valid child StartDate and latest valid child EndDate, and computes a CompletionPercentage for the part. The row model was extended to carry that completion value end-to-end through GanttRowData and GanttRowViewModel, with targeted tests covering both date aggregation and the completion calculation. On the rendering side, GanttCanvas.DrawPartRow now projects the rollup span using axis bounds, draws a muted summary box for the part, clips the progress fill so it cannot overflow the rounded rect, and shows a percentage label only when there is enough horizontal room. Existing fallback behavior remains in place for parts without valid aggregated dates, so the row still degrades cleanly instead of drawing a misleading box.

## Verification

Verification was satisfied by the task-level evidence already recorded for this slice: T01’s GanttViewModel work passed build/test verification, including a full `dotnet build Hymnal.sln` and `dotnet test Hymnal.sln` run with 86 tests passing and the targeted `GanttViewModelTests` filter running 27 tests green. T02’s renderer change also passed `dotnet build src/Hymnal/Hymnal.csproj -nologo` and `dotnet test Hymnal.sln --no-build -nologo`, with 86 passed, 0 failed, 0 skipped. A fresh session check of the task summaries confirmed those same build/test results before closure.

## Requirements Advanced

- R005 — Advanced the Gantt capability by adding Part rollup rows and progress fill to the read-only timeline view.

## Requirements Validated

None.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

Completion fill uses a done-fraction approximation instead of the originally suggested word-count weighting because word-count data is not yet present in the model.

## Known Limitations

Part rollups currently render as a compact summary box with progress fill; they do not yet expand into a richer stacked mini-lane presentation.

## Follow-ups

Revisit completion weighting once word-count data is available. Tune dense-timeline spacing if later manuscripts show the compact rollup needs refinement.

## Files Created/Modified

- `src/Hymnal.Core/Models/GanttRowData.cs` — Added completion percentage to the Gantt row data contract.
- `src/Hymnal/ViewModels/GanttRowViewModel.cs` — Surfaced CompletionPercentage through the view model.
- `src/Hymnal/ViewModels/GanttViewModel.cs` — Computed Part rollup date spans and completion values from child chapters.
- `src/Hymnal/Views/GanttCanvas.cs` — Rendered Part rollup boxes with clipped progress fill and percentage labels.
- `tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs` — Covered Part rollup aggregation and completion behavior.
