# S02: Part Rollup Rows and Progress Fill — UAT

**Milestone:** M003
**Written:** 2026-06-01T19:04:59.534Z

# S02: Part Rollup Rows and Progress Fill — UAT

**Milestone:** M003
**Written:** 2026-06-01

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: This slice is a pure data-projection and custom-rendering change, so the recorded build/test evidence and targeted model tests are sufficient to prove the contract without a live desktop walkthrough.

## Preconditions

- S01 Gantt canvas foundation is present.
- The workspace/manuscript model contains at least one Part node with child chapters that have phase dates.
- S02 model and renderer changes are merged and the relevant test suites can be executed.

## Smoke Test

- Run the slice’s recorded verification path and confirm the Gantt part rollup tests and build checks pass.

## Test Cases

### 1. Part rollup aggregation

1. Execute the GanttViewModel tests that cover Part rows.
2. Confirm a Part row spans from the earliest valid child StartDate to the latest valid child EndDate.
3. Confirm the row exposes a non-zero CompletionPercentage when some child chapters are done.
4. **Expected:** The rollup row reflects the child chapter range and completion state rather than the Part’s own node values.

### 2. Part rollup rendering

1. Execute the renderer build/test verification for GanttCanvas.
2. Inspect the recorded behavior for Part rows with valid dates.
3. Confirm the rollup box renders with a clipped progress fill and percentage label when space allows.
4. **Expected:** The fill stays inside the rollup rect and narrow rows suppress the label rather than overflowing.

## Edge Cases

### Missing or invalid child dates

1. Use a Part whose child chapters have no valid aggregated dates.
2. **Expected:** The row falls back to the prior divider-style appearance instead of drawing a misleading span box.

### Very narrow part span

1. Use a Part whose span is too narrow for a readable label.
2. **Expected:** The progress label is omitted while the box and fill remain bounded and legible.

## Failure Signals

- `GanttViewModelTests` fails or the solution build/test evidence no longer passes.
- Part rows stop aggregating child date ranges correctly.
- Progress fill spills outside the part box or labels overlap the span.

## Not Proven By This UAT

- Word-count-weighted completion fill; the shipped implementation uses a simpler done-fraction approach.
- Inline date editing, dragging, and any other interactive Gantt behavior from S03.
- Final pixel-perfect layout tuning for dense manuscripts.

## Notes for Tester

- The current slice proves the Part rollup contract and renderer integration, not the later editable Gantt interactions.
- If visual review is needed later, focus on whether the box and fill remain readable in dense timelines rather than on exact spacing.

## New Requirements Surfaced

- None.

## Deviations

- Completion fill uses a done-fraction approximation instead of the originally suggested word-count weighting because word-count data is not yet present in the model.

## Known Limitations

- Part rollups currently render as a compact summary box with progress fill; they do not yet expand into a richer stacked mini-lane presentation.

## Follow-ups

- Revisit completion weighting once word-count data is available.
- Tune dense-timeline spacing if later manuscripts show the compact rollup needs refinement.

