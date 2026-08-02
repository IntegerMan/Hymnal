# S03: Markua Editor with Save — UAT

**Milestone:** M001
**Written:** 2026-05-29T16:07:07.402Z

# S03: Markua Editor with Save — UAT

**Milestone:** M001
**Written:** 2026-05-29

## UAT Type

- UAT mode: mixed
- Why this mode is sufficient: The slice is a UI/editor workflow with explicit save behavior and local file persistence, so a combination of artifact-driven checks and hands-on interaction is enough to validate the contract.

## Preconditions

- The Hymnal app launches successfully.
- A real manuscript workspace has already been opened from S02.
- The workspace contains at least one chapter and one Part node.
- The active chapter file is writable on disk.

## Smoke Test

1. Click a chapter in the sidebar.
2. Confirm the editor opens the chapter text.
3. Type a small change.
4. Confirm the title shows an unsaved indicator.
5. Press Ctrl+S.

**Expected:** The chapter saves without losing content, the dirty indicator clears, and the file on disk reflects the new text.

## Test Cases

### 1) Open chapter from sidebar
- **Steps:** Select a chapter node.
- **Expected:** The editor shows that chapter's text and the save affordance is available.

### 2) Markua highlighting is active
- **Steps:** Open a chapter containing heading text, bold, italic, code block, and attribute-list markup.
- **Expected:** The editor applies Markua-aware highlighting to those token groups.

### 3) Dirty-state indicator appears
- **Steps:** Type any character into the active chapter.
- **Expected:** The title bar shows the modified-state indicator and the Save command becomes available.

### 4) Explicit save works
- **Steps:** Press Ctrl+S or use File > Save.
- **Expected:** The file is saved atomically and the dirty indicator disappears.

### 5) Save-on-switch works
- **Steps:** Edit a chapter, then click a different chapter in the sidebar.
- **Expected:** The current chapter saves first, then the new chapter opens.

### 6) Save failure blocks switching
- **Steps:** Make the active chapter read-only or otherwise force a save error, then click another chapter.
- **Expected:** The switch is blocked, the original buffer stays open, and an error banner remains visible.

### 7) External change while clean
- **Steps:** Open a chapter, leave it clean, then modify the file externally.
- **Expected:** The editor reloads from disk when the change is detected.

### 8) External change while dirty
- **Steps:** Open a chapter, edit it without saving, then modify the file externally.
- **Expected:** The app prompts the user to keep the Hymnal buffer or reload from disk, without silently merging.

### 9) Restore last edited chapter
- **Steps:** Save a chapter, close the app, relaunch, and reopen the workspace.
- **Expected:** The previously edited chapter is restored as the active editor target.

## Edge Cases

- Clicking a Part node should not open the editor on a non-chapter selection.
- Missing chapter entries from the tree should remain non-editable.
- Save should remain atomic even if the process is interrupted during write.
- A failed save must not discard the user's unsaved buffer.

## Pass Criteria

- Chapter selection opens the editor.
- Markua highlighting is visible.
- Dirty-state and save affordances appear and clear correctly.
- Save is atomic and reliable.
- Save-on-switch and conflict handling behave as specified.
