---
id: S03
milestone: M003
status: ready
---

# S03: Inline Date Editing — Context

## Goal

Let the author edit chapter phase dates directly in the Gantt view with a simple inline interaction that commits immediately.

## Why this Slice

This is the interaction slice that makes the Gantt view useful for real planning work, because it removes the need to jump back to the chapter info pane whenever the timeline needs a correction.

## Scope

### In Scope

- Inline editing on chapter rows in the Gantt view.
- Click-to-edit entry into the inline date editor.
- Immediate commit when the author confirms a valid value.
- Blank dates are allowed as a deliberate clear action.
- Invalid or incomplete values stay in the editing flow until corrected.
- The editing experience should feel direct and low-friction rather than modal.

### Out of Scope

- Editing Part rows directly.
- Drag-to-resize or drag-based editing.
- Batch editing multiple dates at once.
- Any new scheduling rules beyond plain date entry.
- Visual polish that is not necessary to make the inline edit flow understandable.

## Constraints

- Must build on the read-only Gantt surface and rollup rows established by S01 and S02.
- Chapter rows only are editable in this slice.
- The edit interaction should be lightweight enough to use repeatedly without leaving the Gantt view.
- Changes should commit as soon as the user confirms them, so the view stays in sync with the underlying metadata.

## Integration Points

### Consumes

- `GanttRowViewModel` and the canvas renderer from S01 — provides the row surface that users click on.
- Part rollup layout from S02 — remains visible but not directly editable in this slice.
- Existing chapter phase metadata — the source of truth for the displayed dates and saved updates.

### Produces

- Click-to-edit inline date behavior for chapter rows.
- Immediate persistence of valid date edits back into the existing chapter metadata path.
- Blank-date clearing behavior for removing a date value deliberately.

## Open Questions

- Should the inline editor allow editing both start and end dates in the same control, or should it edit one field at a time? — current thinking is to keep the first pass simple and edit one date field per interaction.
- How should the editor present a blank date once cleared so the author knows it was intentional? — current thinking is to keep the field visibly empty and rely on the row’s visual state to show the gap.
- If the author clicks away mid-edit, should the last valid value remain or should the edit be canceled entirely? — current thinking is to treat blur as commit once the value is valid, matching the immediate-commit model.
