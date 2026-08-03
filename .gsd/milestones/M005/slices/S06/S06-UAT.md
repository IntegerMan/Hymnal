# S06: Corkboard inclusion toggle and chapter insertion — UAT

**Milestone:** M005
**Written:** 2026-06-18T23:53:09.836Z

# UAT Type
Targeted desktop workflow replay on a local sample manuscript workspace.

# Preconditions
- Launch Hymnal against a writable workspace containing `Book.txt`, at least one Part section, and multiple chapter files.
- Ensure one chapter is currently included and visible on the Corkboard.
- If available, keep a second markdown file present in the workspace that can be excluded and re-included.

# Steps
1. Open the Corkboard view and locate an included chapter card inside a Part.
2. Exclude that chapter from the card action/menu.
3. Confirm the card remains visible on the Corkboard as an excluded card with muted styling, badge/helper text, and no drag/open affordance.
4. Re-include the excluded card using the card action/menu.
5. Verify the chapter returns as an included card in the expected structural position.
6. Insert a new chapter between two existing chapter cards using the inline/new-chapter affordance.
7. Enter a unique chapter title and confirm creation.
8. Verify the new chapter appears in the expected slot on the Corkboard and that a new markdown file is created in the correct Part folder (or workspace root when outside a Part).
9. Insert another new chapter immediately after a Part divider or into an empty Part if that affordance is available.
10. Close and reopen the workspace, or restart Hymnal and reopen the same workspace.
11. Revisit the Corkboard and sidebar.

# Expected Outcomes
- Excluding a chapter updates the Corkboard without silently removing the card; the item is clearly marked excluded and is not draggable/openable.
- Re-including the chapter restores it through the canonical structure path and places it at the expected Book.txt order position.
- Creating a new chapter inserts it exactly where requested relative to neighboring chapter cards or Part boundaries.
- `Book.txt` order matches the visual Corkboard order after reload/restart.
- `.hymnal-data/exclusions.json` reflects the excluded state when applicable and no duplicate excluded cards appear after reload.
- Duplicate or invalid create/include attempts surface an error notification and do not leave partial files or partial Book.txt changes behind.

# Edge Cases
- Insert at the very beginning of the book.
- Insert immediately after an empty Part divider.
- Insert within nested Part folders.
- Attempt to create a chapter whose generated file path would collide with an existing file.
- Exclude and re-include the same chapter across a workspace reload.

# Pass Criteria
The user can exclude, re-include, and insert chapters from the Corkboard with the same persisted structure visible after reload/restart, and invalid operations fail visibly without duplicate files, duplicate excluded cards, or Book.txt divergence.
