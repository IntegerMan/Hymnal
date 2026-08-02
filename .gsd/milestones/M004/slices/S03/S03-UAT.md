# S03: Git Toolbar Commit Workflow — UAT

**Milestone:** M004
**Written:** 2026-06-04T14:04:33.183Z

## UAT Type
Operational / integration

## Preconditions
- System Git is installed and available on PATH.
- The opened workspace is a Git repository for the positive path.
- The workspace contains at least one uncommitted file change.
- For push testing, a remote is configured and reachable.

## Steps
1. Open a Git-backed workspace in Hymnal.
2. Observe the toolbar area.
3. Verify the Git group is visible only when the workspace is inside a repository and Git is available.
4. Confirm the branch name and uncommitted-change count are shown.
5. Open the commit dialog.
6. Leave the message blank and choose Commit only.
7. Confirm the dialog uses the default `Hymnal: save progress <UTC ISO-8601>` message and that all changes are staged and committed.
8. Make another file change, reopen the commit dialog, and choose Commit & Push.
9. Confirm the commit is created and pushed to the configured remote.
10. Trigger a commit/push failure path and confirm the notification shows raw Git stderr when present.
11. Trigger an editor save and confirm the dirty count refreshes afterward.
12. Switch to a non-repository workspace or disable Git on PATH and confirm the Git toolbar hides/resets.

## Expected Outcomes
- The Git toolbar is hidden when Git is unavailable or the workspace is not a repository.
- The branch name and dirty count refresh after saves, workspace changes, watcher events, and Git operations.
- Commit only stages all changes and writes a commit using the default timestamped message when the user does not supply one.
- Commit & Push stages all changes, commits, and pushes to the current remote.
- Failures surface raw stderr through notifications instead of swallowing diagnostics.

## Edge Cases
- Atomic saves or Git metadata writes should not cause the status count to flicker or spam refreshes.
- Blank or whitespace-only commit messages must fall back to the default timestamped message.
- A failed push should still refresh status after the notification is shown.
- Switching workspace roots should dispose the old watcher and attach the new one without leaking events.
