# S01: Chapter Registry and Status Lifecycle — UAT

**Milestone:** M002
**Written:** 2026-05-31T00:55:39.568Z

## UAT Type
Manual functional regression + persistence verification

## Preconditions
- Workspace contains a `Book.txt` with at least one chapter file and one Part node.
- App starts with a clean `.hymnal-data/` folder or a known pre-existing registry/phases file.

## Steps
1. Open the workspace.
2. Confirm every chapter row in the sidebar shows a status dot and every Part row does not.
3. Click a chapter status dot and choose `Drafting`.
4. Observe that the dot changes colour immediately and the chapter state persists after relaunch.
5. Rename a chapter file and update `Book.txt` to match the new path.
6. Relaunch the app and confirm the chapter keeps the same status/UUID-backed history after rename.
7. Open a missing chapter entry and confirm the dot appears dimmed and non-interactive.
8. Change status on two different chapters in quick succession and confirm both persist after reload.

## Expected Outcomes
- Status dots are visible for chapter rows and absent for Part rows.
- Selecting a new status updates the dot colour immediately.
- Closing and reopening the app preserves chapter status.
- Renames preserve UUID-backed chapter identity and do not reset phase data.
- Missing-file chapters are dimmed, do not open the flyout, and do not look interactive.
- Concurrent or rapid status updates do not clobber each other.

## Edge Cases
- Existing workspaces with legacy `phases.json` entries using old status values still load without breaking the workspace.
- Reopening a workspace after a rename plus an orphaned entry does not assign a new UUID when the rename can be matched by chapter title.
- A workspace with no `.hymnal-data/` files still opens and seeds new registry/phase entries on first interaction.
