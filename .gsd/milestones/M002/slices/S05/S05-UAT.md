# S05: Desktop UAT and Operational Verification — UAT

**Milestone:** M002
**Written:** 2026-05-31T20:19:48.597Z

# UAT: M002 Desktop UAT and Operational Verification

## UAT Type
Desktop acceptance verification with a mix of automated evidence review and human-observable UI checks.

## Preconditions
- Hymnal builds successfully.
- `Hymnal.sln` test suite passes.
- A real workspace is available with persisted `.hymnal-data/` state.
- For the rename scenario, use an isolated copy of the workspace, not the original.
- For the ValidationMargin check, be prepared to undo the temporary markup before saving.

## Scenario 1: Status persistence across restart
1. Open Hymnal and load a workspace with at least one chapter.
2. Open Chapter Info with F3.
3. Change chapter status from Outlining to Drafting.
4. Confirm the sidebar dot updates immediately and PhaseStartDate is pre-filled.
5. Quit and relaunch Hymnal.
6. Confirm the same workspace opens and the status plus phase-start date are still present.

**Expected:** status change persists to `phases.json`, the sidebar dot updates immediately, and restart restores the saved state.

## Scenario 2: Rename UUID survival
1. Copy the workspace to a temporary directory.
2. Open the copy in Hymnal and confirm the target chapter has the expected status.
3. Quit Hymnal.
4. Rename the chapter `.md` file on disk.
5. Update `Book.txt` to reference the renamed file.
6. Reopen Hymnal with the copy workspace.
7. Confirm the chapter still has its prior status and phase data.

**Expected:** the renamed chapter retains its UUID-backed identity and does not lose status/history data.

## Scenario 3: Live word count, save side-effects, and history
1. Open a populated chapter.
2. Type approximately 100 words.
3. Pause typing and observe the count update in the sidebar / Chapter Info pane.
4. Press Ctrl+S.
5. Confirm the saved count is reflected and a new history entry appears for today.

**Expected:** the live count updates within the debounce window, save completes atomically, and history appends from the Saved signal.

## Scenario 4: Target and proximity fill
1. Open Chapter Info on a chapter with a target.
2. Set or confirm a target of 4,000 words on a chapter with about 2,000 words.
3. Observe the target/proximity display.

**Expected:** proximity fill is about 50% and reflects the authoritative `ChapterViewModel` calculation.

## Scenario 5: ValidationMargin advisory dot
1. Open a chapter in the editor.
2. Temporarily insert a blank line before a `{sample: true}` attribute block, or add an unknown attribute key such as `{boguskey: true}`.
3. Confirm the advisory dot appears in the gutter.
4. Undo the temporary change before saving.

**Expected:** the advisory dot appears for the targeted pattern and disappears once the text is corrected.

## Edge Cases
- If the workspace already contains unexpected orphans, record them before judging restart behavior.
- If the chapter rename test would touch the original workspace, stop and retry only in the isolated copy.
- If the ValidationMargin indicator appears after a non-targeted edit, confirm the exact markup pattern before treating it as a failure.
- If cold-start timing is above the milestone target, record the actual value and environment rather than inventing a pass.

## Exit Criteria
- Desktop scenarios are observed and recorded in the S05 summary.
- Automated build/test evidence remains green.
- No temporary validation text is left in the manuscript after UAT.
