# S03: Sidebar rename with UUID continuity — UAT

**Milestone:** M005
**Written:** 2026-06-18T12:19:56.030Z

# UAT Type
Manual desktop UAT script for downstream integrated verification; rename-specific slice UAT is prepared here and will be executed in the broader multi-surface pass in S08.

# Preconditions
- Build of the current repo succeeds and the app can launch from `src/Hymnal/Hymnal.csproj`.
- Use an isolated copy of a workspace containing `Book.txt`, at least one Part folder, at least two chapter markdown files, and existing `.hymnal-data` metadata for notes, phases, targets, and word-count history.
- The chosen chapter title is unique enough to make continuity easy to confirm after reload.
- The workspace has at least one included present chapter row and one Part row visible in the sidebar.

# Scenario 1 — Rename an included chapter from the sidebar
1. Launch Hymnal against the isolated workspace copy.
2. Select an included present chapter that already has UUID-backed metadata.
3. Open the sidebar context menu for that row and choose `Rename…`.
4. Enter a new title that changes the filename slug but keeps it in the same folder.
5. Confirm the rename and wait for the workspace to reload.
6. Reopen the renamed chapter and inspect any surfaces backed by the chapter UUID (notes, phase/state, targets, or history if visible).
7. Close and relaunch the app, then reopen the same workspace.

**Expected outcomes**
- The row offered `Rename…` before the operation.
- The markdown file is renamed on disk.
- `Book.txt` references the new relative path.
- The chapter heading/display title reflects the new name.
- The chapter is still represented by the same UUID-backed metadata after reload and after app relaunch.
- No duplicate old/new sidebar entries remain.

# Scenario 2 — Rename a Part from the sidebar
1. In the same workspace copy, open the context menu on a Part row and choose `Rename…`.
2. Enter a new Part title that changes the folder segment.
3. Confirm the rename and wait for the workspace to reload.
4. Inspect the sidebar children under the renamed Part.
5. Reopen one or more moved child chapters and confirm their metadata-backed state is still present.
6. Inspect `Book.txt` and the workspace folders on disk.

**Expected outcomes**
- The Part row offered `Rename…`.
- The Part folder is renamed on disk.
- All descendant `Book.txt` entries are rewritten under the new folder path.
- Child chapters reload under the renamed Part with no duplicate old-path nodes.
- Child UUID-backed metadata remains attached after the move.

# Scenario 3 — Conflict failure is visible and non-destructive
1. Attempt to rename a chapter or Part to a name whose target path already exists.
2. Confirm the action.

**Expected outcomes**
- The app shows a visible rename failure notification.
- The message identifies the rename as a structural failure rather than silently doing nothing.
- No file or folder is moved.
- `Book.txt` remains unchanged.
- Existing sidebar selection and metadata remain intact after the failed attempt.

# Scenario 4 — Invalid or ineligible rows do not expose rename
1. Inspect an excluded row and any missing/non-present row in the sidebar if available.
2. Open each row’s context menu.

**Expected outcomes**
- `Rename…` is not offered for excluded or missing nodes.
- Included present chapter rows and Part rows still offer `Rename…`.

# Edge cases to watch
- Case-only rename attempts should fail consistently rather than producing ambiguous path behavior.
- If reload fails after a rename attempt, the user should receive an explicit notification instead of silent partial state.
- After relaunch, verify there is no UUID churn such as missing notes/history or a newly created empty metadata state for the renamed entry.
- Confirm no stale old-path entry remains in `Book.txt`, the sidebar, or `.hymnal-data` continuity surfaces.
