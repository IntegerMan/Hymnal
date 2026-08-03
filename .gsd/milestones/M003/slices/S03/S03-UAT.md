# S03: Inline Date Editing — UAT

**Milestone:** M003
**Written:** 2026-06-01T19:28:27.907Z

# S03: Inline Date Editing — UAT

**Milestone:** M003
**Written:** 2026-06-01

## UAT Type

- UAT mode: mixed
- Why this mode is sufficient: The slice is a UI interaction with persistence, and the implemented test coverage plus task-level build/test evidence proves the wiring, while a short manual smoke confirms the author-facing flow.

## Preconditions

- The app opens a workspace that already has chapter phase dates in the registry.
- The Gantt view is available and shows chapter rows.
- At least one chapter row has editable phase dates.

## Smoke Test

- Click a chapter phase entry in the Gantt view.
- The date editor overlay appears with the current chapter title and date fields.
- Change one date and save.
- The Gantt view and underlying chapter metadata reflect the updated date.

## Test Cases

### 1. Edit a chapter date inline

1. Open the Gantt view.
2. Click a chapter row/phase entry.
3. Update the start or end date in the overlay.
4. Click Save.
5. **Expected:** The overlay closes and the updated date is persisted through the chapter metadata path.

### 2. Clear a date deliberately

1. Open the date editor for a chapter row.
2. Remove one of the dates so it is blank.
3. Click Save.
4. **Expected:** The blank value is accepted as an intentional clear action and the stored metadata no longer contains that date.

## Edge Cases

### Invalid or incomplete input

1. Open the date editor.
2. Enter an incomplete or invalid date value.
3. **Expected:** The editor stays in the editing flow until the value is corrected; no bad value is committed.

## Failure Signals

- Clicking a chapter row does nothing.
- The overlay opens but Save does not persist the new date.
- The editor crashes or leaves the UI in a broken state after cancel/save.
- Part rows become editable, which would indicate the edit routing is too broad.

## Not Proven By This UAT

- Precise pixel-anchored positioning of the editor near the clicked row.
- Drag-based editing or resize handles.
- Editing Part rows directly.
- End-to-end workflow validation beyond chapter date persistence.
- Performance under very large manuscripts.

## Notes for Tester

The implementation intentionally uses a fixed-position overlay instead of a row-anchored flyout. The chapter title in the overlay is the cue that the editor is tied to the clicked row.
