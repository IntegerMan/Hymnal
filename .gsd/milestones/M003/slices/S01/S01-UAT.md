# S01: Gantt Canvas Renderer Foundation — UAT

**Milestone:** M003
**Written:** 2026-06-01T18:51:57.531Z

# S01: Gantt Canvas Renderer Foundation — UAT

**Milestone:** M003
**Written:** 2026-06-01

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: This slice is a read-only renderer and projection change with strong unit and build coverage; the correctness signal comes from test/build evidence plus the verified slice artifacts.

## Preconditions

- The workspace opens successfully and loads manuscript chapter metadata.
- The solution builds and the Gantt-focused test suite passes.
- The Gantt tab/view is present in the main shell.

## Smoke Test

- Build the app project and run the Gantt view-model tests. Confirm the Gantt tab can render a chapter timeline without requiring edits or extra setup.

## Test Cases

### 1. Read-only chapter timeline renders

1. Open a workspace with saved phase metadata.
2. Switch to the Gantt tab.
3. **Expected:** A time axis and per-chapter phase bars render in chapter order.

### 2. Missing or invalid dates stay visible

1. Open a workspace where one or more chapters have missing or invalid phase dates.
2. Switch to the Gantt tab.
3. **Expected:** The affected chapter rows still appear as muted/gap-like placeholders instead of disappearing or crashing the view.

### 3. In-session phase changes refresh the projection

1. Change a chapter's phase data in-session through the existing workspace model path.
2. Return to the Gantt tab or observe the projection refresh.
3. **Expected:** The corresponding Gantt row updates without needing a workspace reload.

## Edge Cases

### All chapters lack valid dates

1. Load a workspace where no chapter has a valid phase range.
2. Open the Gantt tab.
3. **Expected:** The renderer falls back to a sensible axis and still shows the rows in muted form.

## Failure Signals

- The Gantt tab is missing from the shell.
- The Gantt surface renders empty when rows exist.
- The app crashes or throws on missing dates.
- Phase changes do not appear until the workspace is reloaded.

## Not Proven By This UAT

- Inline date editing.
- Part rollup rows and aggregate summaries.
- Progress fill inside bars.
- Visual polish or advanced responsive scaling of the axis.

## Notes for Tester

This slice is intentionally read-only. The main thing to check is that the timeline projection remains legible and stable even when date data is incomplete, and that the projection updates when phase metadata changes in-session.
