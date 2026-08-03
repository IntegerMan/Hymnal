# S02: Supplemental Docs Sidebar and Editor Path — UAT

**Milestone:** M004
**Written:** 2026-06-04T05:29:59.863Z

# UAT: Supplemental Docs Sidebar and Editor Path

**UAT Type:** Desktop UI smoke + persistence flow

## Preconditions
- A workspace exists with a valid `Book.txt`.
- Hymnal is launched with that workspace open.
- The DOCS sidebar section is visible.
- The workspace contains or can create `.hymnal-data/docs/`.

## Steps
1. Expand the DOCS section in the left sidebar.
2. Create a new folder, for example `research`.
3. Create a new doc file inside that folder, for example `notes.md`.
4. Select the new doc file.
5. Verify the editor opens the file in Write mode and the chapter selection remains unchanged.
6. Type content into the doc and save.
7. Switch to a chapter, then switch back to the doc.
8. Close Hymnal, reopen the same workspace, and return to DOCS.
9. Re-open the doc file and confirm the content is still present.

## Expected Outcomes
- New folders and files are created only under `.hymnal-data/docs/`.
- The new doc appears in the DOCS tree immediately after creation.
- Opening a doc routes through the existing editor path with `ActiveNode == null` and `ActiveFilePath` set.
- Editing a doc marks it dirty, and save writes through the shared atomic metadata-store path.
- Switching between chapters and docs preserves unsaved-change behavior and does not corrupt content.
- After reopening the workspace, the doc remains listed and the saved content is intact.

## Edge Cases
- Invalid folder or file names should surface a clear error notification and not create files.
- Attempting to open a folder node should not load the editor as a document.
- Reopening the workspace after saving should not duplicate the doc tree or lose selection state.
- Saving while a doc is externally modified should still surface the existing conflict/dirty behavior rather than silently overwriting.

## Acceptance Check
- DOCS tree creation, editor open/save, and reopen persistence all succeed in one continuous workflow.
