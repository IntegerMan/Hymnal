# S04: Chapter Notes Panel — UAT

**Milestone:** M001
**Written:** 2026-05-29T20:19:42.213Z

# S04: Chapter Notes Panel — UAT

**Milestone:** M001
**Written:** 2026-05-29

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: The slice is primarily shell integration plus persistence behavior, and the automated build/test evidence verifies the controller and storage paths without requiring a live UI session.

## Preconditions

- A Hymnal workspace is open with at least one chapter selected.
- The app has access to the workspace root so it can resolve `.hymnal-data/notes/`.

## Smoke Test

1. Open a chapter in the editor.
2. Toggle the Notes panel.
3. **Expected:** The panel appears and shows the selected chapter's note content, or an empty editor for a new chapter.

## Test Cases

### 1. Load notes for an existing chapter

1. Open a chapter that already has a saved notes file.
2. Open the Notes panel.
3. **Expected:** The saved note text loads into the panel for that chapter.

### 2. Save edits through auto-save

1. Open the Notes panel for a chapter.
2. Type or modify note text.
3. Pause typing long enough for the debounce window to flush.
4. **Expected:** The note is persisted to `.hymnal-data/notes/` using the existing atomic metadata write path.

## Edge Cases

### Chapter switch cancels stale edits

1. Edit notes for Chapter A.
2. Before the debounce delay elapses, switch to Chapter B.
3. **Expected:** Chapter A's pending auto-save does not overwrite Chapter B's notes, and Chapter B loads its own content.

## Failure Signals

- Notes panel does not appear when toggled.
- Switching chapters shows stale note content from the previous chapter.
- Edits are lost after the debounce delay or the notes file is not created.
- Build or test verification fails.

## Not Proven By This UAT

- Full live UI behavior of Avalonia layout resizing and keyboard interaction.
- Long-running reliability under repeated rapid chapter switching.
- Cross-platform file permission edge cases.

## Notes for Tester

The automated evidence proves the implementation and build are healthy; a manual run should focus on the feel of the panel toggle, chapter switching, and whether auto-save latency is acceptable.
