# S03: Chapter Info Pane and Validation Margin — UAT

**Milestone:** M002
**Written:** 2026-05-31T03:43:44.499Z

## UAT Type
Desktop smoke + behavior verification

## Preconditions
- Workspace is open with a chapter selected.
- The chapter has known status and optional target data.
- The editor contains a Markua document that can exercise the advisory patterns.

## Steps
1. Press `F3` to open the Chapter Info pane.
2. Confirm the pane shows the current chapter title, status, phase start/end date fields, live word count, and target controls.
3. Change the status to `Editing` and confirm the phase start date auto-fills to today when prefill is enabled.
4. Toggle the right rail so both Chapter Info and Notes are visible and confirm the row splitter is present and resizes the panes.
5. Open a Markua file that contains a blank line immediately before a `{sample: true}` heading and confirm an advisory dot appears in the editor gutter.
6. Add or remove the right-rail panes and confirm notes behavior still works as before.

## Expected Outcomes
- `F3` opens Chapter Info without disrupting the workspace.
- Chapter Info reflects the active chapter's persisted state and live word count.
- Status changes persist and phase date prefill behaves as configured.
- The right rail supports both panes together, with the splitter visible only when both are expanded.
- Advisory gutter markers appear for the two supported patterns and never block editing or crash the editor.

## Edge Cases
- Switching chapters while the pane is open should resync state without leaking old chapter subscriptions.
- A document with no advisory patterns should not render gutter dots.
- Any editor or margin exception must be swallowed silently.

## Pass Criteria
- The pane opens on F3 and displays the active chapter correctly.
- Status/date/target interactions work end to end.
- The validation margin appears for the supported advisory patterns and the app remains stable.
