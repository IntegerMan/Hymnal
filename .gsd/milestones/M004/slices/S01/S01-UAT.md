# S01: Plan Mode Corkboard Cards — UAT

**Milestone:** M004
**Written:** 2026-06-04T04:12:25.945Z

**UAT Type:** Manual desktop smoke + functional verification

**Preconditions**
- A workspace with a valid `Book.txt` and at least one Part plus multiple chapters.
- The app is launched normally and the workspace is already open.
- At least one chapter has status, word count, and one or more phase/target fields populated so the card can display live metadata.

**Steps**
1. Click the **Plan** tab.
2. Verify the shell switches to a full-board corkboard layout with left/right chrome collapsed like Manage mode.
3. Confirm the board shows Part dividers in Book.txt order and a card for each chapter.
4. Inspect one card and verify it shows chapter title, status, word count, target/progress area or `No target`, and phase dates when present.
5. Click a card once and confirm it becomes the selected card.
6. Press **Enter** or double-click the selected card.
7. Verify the editor opens that chapter and the shell returns to **Write** mode.
8. Use a structural action from the card context menu, such as rename, new chapter, include existing, remove from book, or delete with confirmation.
9. Verify the change is reflected in Book.txt order and the board refreshes afterward.

**Expected Results**
- Plan mode is selectable and visible as a dedicated corkboard surface.
- Part dividers render even when a Part is empty.
- Card metadata reflects the current live manuscript state.
- Selection is visually obvious and keyboard open works.
- Opening a card routes through the existing chapter-open path and returns to Write mode.
- Structural edits are applied atomically and the corkboard stays in sync after the workspace refresh.

**Edge Cases**
- Empty workspace / no chapters: the board should show an explicit empty state rather than a blank panel.
- Chapters without targets: the card should show `No target` rather than dropping the target row.
- Part with no chapters: the Part divider should still appear with a subtle empty hint.
- Delete action: must require confirmation before any destructive file removal occurs.
- Invalid reorder target: the board should ignore the invalid drop rather than corrupting order.
