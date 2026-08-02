---
id: S01
parent: M003
milestone: M003
provides:
  - A working read-only Gantt tab in the main shell.
  - A reusable row projection for chapter timeline rendering.
  - A canvas renderer that can be extended with rollups and editing in later slices.
requires:
  []
affects:
  - S02
  - S03
key_files:
  - src/Hymnal/ViewModels/GanttViewModel.cs
  - src/Hymnal/ViewModels/GanttRowViewModel.cs
  - src/Hymnal/Views/GanttCanvas.cs
  - src/Hymnal/Views/GanttView.axaml
  - src/Hymnal/Views/GanttView.axaml.cs
  - src/Hymnal/ViewModels/MainWindowViewModel.cs
  - src/Hymnal/Views/MainWindow.axaml
  - src/Hymnal/App.axaml.cs
  - tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs
key_decisions:
  - Keep incomplete timeline data visible by rendering fallback axes and muted rows rather than collapsing the canvas.
  - Subscribe the projection view-model to ChapterViewModel.PhaseData so in-session phase changes refresh the Gantt rows immediately.
  - Use a custom canvas-based renderer for the first Gantt surface to keep the view read-only and predictable.
patterns_established:
  - Read-only timeline projections should preserve incomplete data instead of hiding it.
  - Projection view-models should subscribe to the exact data they display, not just workspace reload events.
  - Canvas renderers should prefer stable fallback geometry over empty-state collapse when input data is partial.
observability_surfaces:
  - none
drill_down_paths:
  - .gsd/milestones/M003/slices/S01/tasks/T01-SUMMARY.md
  - .gsd/milestones/M003/slices/S01/tasks/T02-SUMMARY.md
  - .gsd/milestones/M003/slices/S01/tasks/T03-SUMMARY.md
  - .gsd/milestones/M003/slices/S01/tasks/T04-SUMMARY.md
duration: ""
verification_result: passed
completed_at: 2026-06-01T18:51:57.529Z
blocker_discovered: false
---

# S01: Gantt Canvas Renderer Foundation

**Read-only Gantt tab with a time axis, chapter phase bars, fallback rendering for missing dates, and live refresh when phase data changes.**

## What Happened

Slice S01 established the first usable Gantt surface for M003. The projection layer now turns the loaded manuscript workspace into Gantt rows, the custom Avalonia canvas renders a read-only time axis plus per-chapter phase bars, and the shell exposes a new Gantt tab/view from the main window.

The initial implementation handled the happy path, then follow-on fixes hardened the renderer and projection behavior. When chapters have no valid phase dates, the canvas now keeps their rows visible by falling back to a sensible axis range and muted placeholder bands instead of collapsing to an empty state. The projection view-model also listens for in-session PhaseData changes on ChapterViewModel and rebuilds rows immediately, so date edits propagated through the existing workspace model are reflected without a reload.

Task-level verification stayed green throughout the slice. The Gantt-focused test suite passed, the full solution test run passed with 82/82 tests, and the Hymnal app project built successfully. Together these checks prove the slice delivers a stable, read-only timeline projection wired into the shell and resilient to incomplete phase metadata.

## Verification

Verified by task summaries and solution-level evidence: `dotnet test Hymnal.sln --filter GanttViewModelTests -nologo` passed; `dotnet test Hymnal.sln -nologo` passed with 82/82 tests; `dotnet build src/Hymnal/Hymnal.csproj -nologo` passed. The rendered slice now satisfies the completion contract: S01 is marked complete in the roadmap, and both the summary and UAT artifacts are written by the canonical slice-closeout path.

## Requirements Advanced

- R005 — Advanced the per-chapter phase timeline requirement by delivering the first read-only Gantt tab, time axis, and chapter phase bars from saved phase metadata.

## Requirements Validated

- R005 — Artifact and test evidence show the Gantt view renders chapter rows from saved phase data, preserves rows with missing dates, and refreshes when PhaseData changes in-session.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

None.

## Known Limitations

Inline editing, rollups, and progress fill are deferred to later slices.

## Follow-ups

None.

## Files Created/Modified

- `src/Hymnal/ViewModels/GanttRowViewModel.cs` — Added row projection model for chapter timeline rendering.
- `src/Hymnal/ViewModels/GanttViewModel.cs` — Projected workspace chapters into Gantt rows and refreshed on phase-data changes.
- `src/Hymnal/Views/GanttCanvas.cs` — Rendered the read-only time axis and phase bars, including fallback display for missing dates.
- `src/Hymnal/Views/GanttView.axaml` — Added the Gantt view shell.
- `src/Hymnal/Views/GanttView.axaml.cs` — Bound the view to the Gantt projection.
- `src/Hymnal/ViewModels/MainWindowViewModel.cs` — Exposed the Gantt tab state in the main shell.
- `src/Hymnal/Views/MainWindow.axaml` — Added the Gantt tab/view to the main window.
- `src/Hymnal/App.axaml.cs` — Registered Gantt-related dependencies in DI.
- `tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs` — Added projection, missing-date, and live-refresh coverage.
