---
id: S01
milestone: M003
status: ready
---

# S01: Gantt Canvas Renderer Foundation — Context

## Goal

Render a read-only Gantt tab that shows a time axis and per-chapter phase boxes from saved phase metadata.

## Why this Slice

This slice establishes the first user-visible timeline surface for M003 and proves the manuscript metadata can be turned into a useful schedule view before adding rollups, progress fill, or inline editing.

## Scope

### In Scope

- A new Gantt view/tab available in the main shell.
- Read-only rendering of chapter phase boxes against a visible time axis.
- Use saved `PhaseData` as the source of truth for what appears on the timeline.
- Chapters with missing or invalid phase dates remain visible but render as gaps or muted/incomplete rows rather than disappearing.
- The view should feel stable and legible first; polish can come later.

### Out of Scope

- Editing dates directly in the Gantt view.
- Part rollup rows and aggregate summaries.
- Progress fill inside phase bars.
- Drag/reorder behavior.
- Any timeline logic that depends on future slice features.

## Constraints

- This slice must stay read-only.
- It must build on the existing manuscript/phase metadata model rather than inventing a parallel source of truth.
- The first version should prioritize clarity and correctness over visual novelty.
- Incomplete date data should not block the rest of the timeline from rendering.

## Integration Points

### Consumes

- `.hymnal-data` phase metadata via the existing phase model/service path — source of saved chapter date ranges.
- Manuscript chapter ordering from the loaded workspace model — determines row order in the timeline.
- Main shell navigation — where the new Gantt surface will appear.

### Produces

- A Gantt view entry in the app shell.
- A reusable row model for chapter timeline rendering.
- A custom canvas-based renderer that draws the time axis and chapter bars.

## Open Questions

- Should missing or partial dates show as faint placeholders, gaps, or warning-styled rows? — current thinking is to keep them visible but visually muted so the view remains usable.
- How dense should the initial time axis be at different window sizes? — current thinking is to keep it simple and readable rather than highly adaptive.
- Should the first release show only chapter rows, or also surface any lightweight summary labels in the header? — current thinking is to keep summaries for the next slice.
