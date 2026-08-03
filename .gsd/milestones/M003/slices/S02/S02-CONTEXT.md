---
id: S02
milestone: M003
status: ready
---

# S02: Part Rollup Rows and Progress Fill — Context

## Goal

Show Part rows in the Gantt view as compact rollups that span their child chapters' date range and visualize overall completion with a weighted progress fill.

## Why this Slice

This slice turns the chapter timeline into a real project-management view by letting the author see section-level structure and completion at a glance without waiting for inline editing or later Gantt interactions.

## Scope

### In Scope

- Part rows appear in the Gantt view alongside chapter rows.
- A Part row spans from the earliest relevant phase start to the latest relevant phase end among its child chapters.
- Part rows display a progress fill based on total completion percentage for the part, weighted by total word count.
- When phase bars overlap within a Part, the layout uses stacked mini-lanes so the bars remain readable.
- The row should stay compact, with bars sized small enough to remain legible in dense timelines.

### Out of Scope

- Inline date editing.
- Dragging or resizing bars.
- Reordering chapters or parts.
- Any other Gantt interaction beyond display.
- Final pixel-perfect visual polish if it requires later experimentation beyond the basic stacked-lane approach.

## Constraints

- Must build on the read-only renderer delivered by S01.
- Must respect the existing manuscript order and phase metadata as the source of truth.
- Overlapping phase bars must remain understandable rather than collapsing into a single unreadable strip.
- The UI should communicate structure clearly even when different phases overlap in time.

## Integration Points

### Consumes

- `GanttViewModel` and `GanttRowViewModel` from S01 — provides the baseline chapter timeline rows and rendering surface.
- Manuscript ordering and chapter/part structure from the existing manuscript model — determines which rows are grouped together.
- Saved phase data for each chapter — drives date span and completion calculations.
- Word-count totals for parts — used to weight completion fill.

### Produces

- Part rollup rows in the Gantt view — summary rows that aggregate child chapter timing.
- Weighted progress fill rendering — a visible completion cue for section-level progress.
- Stacked mini-lane rendering behavior for overlapping phase bars within a Part.

## Open Questions

- Exactly how many mini-lanes should the renderer allocate before it starts compressing or truncating overlaps? — current thinking is to keep the implementation flexible and tune the limit after seeing real manuscript density.
- Should the completion fill use a simple word-count weighting across child chapters or incorporate phase-state weighting as well? — current thinking is to start with weighted word count only, matching the user’s mental model.
- How much axis-density tuning is needed for compact Part rows? — current thinking is to keep the initial pass readable first and refine spacing later if the rendered result feels crowded.
