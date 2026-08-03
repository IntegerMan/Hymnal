# S04: Runtime Stabilization and Override Alignment — UAT

**Milestone:** M002
**Written:** 2026-05-31T05:52:06.221Z

## UAT Type
Desktop smoke / integration

## Preconditions
- Hymnal builds successfully
- A workspace exists with at least one chapter that has a status and optional target configured

## Steps
1. Launch the app and open a workspace.
2. Press F3 to open the Chapter Info pane.
3. Select a chapter that has a target and some word count progress.
4. Verify the pane shows the chapter's status, phase dates, word count, target display, HasTarget state, and a non-zero proximity fill when appropriate.
5. Switch to a different chapter and confirm the pane updates to the newly active chapter without crashing.
6. Remove the target from a chapter and confirm the proximity fill drops to zero and HasTarget becomes false.
7. Reopen the app and verify startup still succeeds with the same workspace.

## Expected Outcomes
- App launch does not throw a NullReferenceException from ChapterInfoViewModel.
- F3 opens the Chapter Info pane successfully.
- Target and proximity state reflect the live ChapterViewModel values rather than a stubbed or display-string-derived fallback.
- Switching chapters updates the pane immediately and safely.
- A chapter with no target reports HasTarget = false and ProximityFill = 0.0.

## Edge Cases
- Rapidly switch between chapters while the pane is open; the pane should remain stable.
- Open a workspace with no active chapter; the pane should not crash and should remain in its reset state.
- Open a chapter that has never had target data; the pane should still render and report no target cleanly.

## Pass/Fail Criteria
- Pass if startup succeeds, F3 works, and the Chapter Info pane tracks live chapter data without crashes or stale stubbed values.
- Fail if the app throws on launch, the pane shows static/stubbed proximity data, or target state does not follow the active chapter.
