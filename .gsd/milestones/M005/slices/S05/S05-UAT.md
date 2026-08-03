# S05: Corkboard drag reorder and cross-Part moves — UAT

**Milestone:** M005
**Written:** 2026-06-18T18:51:18.359Z

# S05 UAT — Corkboard drag reorder and cross-Part moves

## UAT Type
Manual desktop UAT with real pointer drag/drop in the Avalonia app.

## Preconditions
- Build of Hymnal containing S05 is installed or runnable locally.
- Test workspace contains a `Book.txt` with at least:
  - one root-level chapter,
  - one Part containing at least one chapter,
  - one second Part or nested Part target,
  - one empty Part region for empty-drop validation.
- `.hymnal-data/registry.json` and any chapter notes/phase/target metadata already exist for moved chapters so continuity can be checked.

## Steps
1. Open the workspace in Hymnal and navigate to the Corkboard view.
2. Drag a chapter card within its current Part to a new position between sibling cards.
3. Confirm the Corkboard redraws with the moved card in the new order.
4. Close and reopen the workspace.
5. Confirm the same-Part order still matches the drop result and `Book.txt` reflects the new ordering.
6. Drag a chapter card from one Part into a different Part.
7. Drop once into a populated Part between two cards.
8. Confirm the Corkboard redraws with the chapter in the target Part.
9. Inspect the workspace on disk and confirm the chapter file moved into the target Part folder.
10. Reopen the workspace and confirm the moved chapter remains in the target Part and still opens correctly.
11. Verify existing notes/phase/target data for the moved chapter still appear under the same chapter after the move.
12. Drag a chapter into an empty Part region and confirm it lands immediately after that Part divider.
13. Attempt an invalid or conflicting drop scenario if available in the fixture (for example, drop onto itself or create a target-path conflict).
14. Confirm the visible card layout does not falsely change and an error is surfaced to the user.

## Expected Outcomes
- Same-Part drag reorder updates the visual Corkboard order immediately after reload/projection.
- `Book.txt` persists the new order after workspace reopen.
- Cross-Part drag uses the canonical structure path and moves the actual chapter file into the destination Part folder.
- Nested Part destinations preserve the full target folder path.
- Chapter UUID-backed metadata continuity is preserved: the moved chapter retains notes and related sidecar data.
- Empty-Part drops place the chapter into the first slot inside that Part.
- Invalid, unsupported, or conflicting drops surface a user-visible error and do not leave the corkboard in a misleading stale-success state.

## Edge Cases
- Drop a card onto its current location or otherwise perform a no-op gesture; verify no incorrect reorder occurs.
- Repeat a cross-Part move after collapse/expand state changes in the Corkboard; verify target mapping still works.
- If a failure occurs after a committed move, verify the board reloads to the truthful on-disk state while still surfacing the failure message.

