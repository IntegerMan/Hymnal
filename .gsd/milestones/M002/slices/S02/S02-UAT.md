# S02: Word Count Targets and Rollup — UAT

**Milestone:** M002
**Written:** 2026-05-31T03:09:27.809Z

## UAT Type
Manual UI + persistence verification

## Preconditions
- Workspace opens successfully with at least one chapter and one Part in `Book.txt`
- At least one chapter has no saved word-count history yet, so it initially displays `—`
- App is running with the sidebar visible

## Steps
1. Open a chapter and type 50 words.
2. Wait at least 300 ms after the final keystroke.
3. Save the chapter with Ctrl+S.
4. Observe the chapter row, Part row, and CHAPTERS header in the sidebar.
5. Right-click a chapter row and choose `Set Target…`.
6. Enter `minWords = 1800` and `maxWords = 4000`, then confirm.
7. Reopen the popup and clear the target.

## Expected Outcomes
- After the debounce interval, the chapter row updates from `—` to a formatted word count.
- After saving, the active chapter’s persisted count is updated and the word-count history gains a new entry for today.
- Part totals and the book total roll up from chapter counts and refresh reactively.
- When a target is set, the chapter row shows a proximity bar with partial fill proportional to the target.
- Clearing the target removes the proximity bar and target state.

## Edge Cases
- Chapters with no saved count remain `—` until background recalculation or a save provides a count.
- Markua directive lines beginning with `{` are excluded from word counts.
- Empty or whitespace-only content counts as 0 words.
- If a chapter has only a `minWords` target, the proximity bar still uses that value as the fill basis.

## Acceptance Notes
- No UI freeze during typing or workspace load.
- No manual editing of `.hymnal-data` files is required.
- The target popup dismisses on Set, Clear, Cancel, and light-dismiss.
