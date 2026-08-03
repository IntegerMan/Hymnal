---
id: M003
title: "Gantt View and Project Management"
status: complete
completed_at: 2026-06-03T23:49:01.441Z
key_decisions:
  - Use explicit shell modes rather than a binary Gantt toggle.
  - Keep Gantt interactions routed through chapter-row commands while part rows remain non-editable.
  - Use a fixed-position overlay for inline date editing rather than tying the editor to the canvas layout.
  - Sanitize non-finite measure values in the Gantt canvas before returning a Size.
key_files:
  - src/Hymnal/ViewModels/MainWindowViewModel.cs
  - src/Hymnal/ViewModels/ShellMode.cs
  - src/Hymnal/Views/MainWindow.axaml
  - src/Hymnal/Views/MainWindow.axaml.cs
  - src/Hymnal/Views/GanttCanvas.cs
  - src/Hymnal/Views/Converters/ShellModeConverters.cs
  - tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs
  - tests/Hymnal.Core.Tests/Views/GanttCanvasTests.cs
  - tests/Hymnal.Core.Tests/Views/ShellModeConvertersTests.cs
lessons_learned:
  - Avalonia ScrollContentPresenter can pass non-finite sizes into Measure, so custom controls must sanitize both width and height before returning one.
  - Shell visibility is easier to reason about when an explicit mode enum drives the main window instead of a boolean toggle.
  - Adding date columns to the Gantt surface is simplest when the canvas owns the layout and the view model stays focused on projection data.
---

# M003: Gantt View and Project Management

**Delivered the Gantt-based project management surface, inline date editing, and shell navigation needed to switch between writing and management modes.**

## What Happened

M003 completed the project-management surface for Hymnal. S01 established the Gantt projection and read-only timeline rendering, S02 added part rollup behavior and summary/progress rendering, and S03 delivered clickable chapter-row date editing with save/cancel persistence back to chapter metadata. The shell was refined so Write remains the authoring surface while Manage hosts the Gantt view, and the Gantt canvas was hardened against non-finite measure inputs while gaining start/end columns for the displayed dates.

## Success Criteria Results

- The Gantt surface renders chapter and part rows with timeline positioning and part rollups.
- Chapter rows support inline date editing with persistence back to chapter metadata.
- Manage mode now hosts the Gantt surface, and Write remains the primary writing surface.
- The Gantt canvas measure path handles non-finite input sizes without throwing.
- Start/end date columns are visible in the Gantt view for each phase row.

## Definition of Done Results

- All slices in M003 are complete: S01, S02, and S03.
- Slice-level verification evidence was recorded for the implemented tasks, including test runs and UAT for S03.
- The milestone’s functional goals were validated through the existing task summaries and the saved UAT assessment.
- The Gantt screen was personally validated by the user, and the milestone validation gate now records pass.

## Requirement Outcomes

- The milestone advanced the project-management and Gantt-view requirements by delivering the timeline surface, date editing, and view switching behavior.
- S03’s UAT and task summaries validated the chapter date-edit flow and persistence path.
- The later shell and Gantt refinements keep the same requirement intent: Gantt is now the Manage surface, and the timeline includes explicit start/end columns.

## Deviations

None.

## Follow-ups

None.
