# S07: Gantt row drag reorder — UAT

**Milestone:** M005
**Written:** 2026-06-19T04:27:18.327Z

# UAT Type
Focused desktop functional UAT for the Gantt surface, with persistence/reload confirmation.

# Preconditions
- Use a workspace whose `Book.txt` contains at least one Part with three included chapter files in order, for example `part-one/chapter-one.md`, `part-one/chapter-two.md`, `part-one/chapter-three.md`.
- Include a second Part with at least one chapter to confirm cross-Part behavior.
- Open the workspace in Hymnal and switch to the Gantt view.

# Steps
1. Select the Gantt row for `chapter-two` inside the first Part.
2. Press `Alt+Up`.
3. Observe the row order in the Gantt grid/canvas.
4. Close and reopen the workspace, or restart the app and reopen the same workspace.
5. Confirm the reordered chapter still appears above `chapter-one` in the same Part.
6. Drag `chapter-three` and drop it before `chapter-two` within the same Part.
7. Observe the row order immediately after drop.
8. Reopen the workspace again and confirm the same order persists.
9. Attempt to drag a chapter row from Part One onto a chapter row in Part Two.
10. Attempt to drag a Part rollup row or book rollup row.

# Expected Outcomes
- After step 2, the selected chapter moves up exactly one legal position within its Part and no duplicate/missing rows appear.
- After steps 4-5, the Gantt order matches the saved `Book.txt` order after reload/restart.
- After steps 6-8, the drag reorder persists and the same ordering is visible after reopening the workspace.
- After step 9, no reorder occurs across Parts; the existing workspace error notification path remains user-visible and `Book.txt` order is unchanged.
- After step 10, rollup rows are not reorderable and no structural mutation occurs.

# Edge Cases
- Dropping a chapter onto itself or an immediately adjacent no-op position should not change order.
- Excluded or missing chapter rows must not become reorder sources or targets.
- Reorder actions must not create a direct Gantt-specific write path; persistence should still flow through the canonical Book.txt structure path.

# Evidence to Capture
- Screenshot of Gantt before reorder.
- Screenshot after keyboard reorder.
- Screenshot after drag reorder.
- `Book.txt` line order before and after one legal move.
- Visible notification text for one rejected cross-Part move.
