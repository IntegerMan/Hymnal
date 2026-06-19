# M005 S08 Structural Consistency Desktop UAT

## Purpose

Prove the finished M005 structural-editing surfaces behave as one coherent manuscript-structure system when an author edits the same workspace through the sidebar, Corkboard, and Gantt, then restarts Hymnal.

This manual script mirrors the automated `StructuralConsistencyUatTests` fixture and should be used as the desktop reviewer script for final S08 acceptance.

## Verification environment assumptions

- Open and verify the project with `Hymnal.slnx`.
- Prefer native Windows .NET from PowerShell, for example:
  - `& 'C:\Program Files\dotnet\dotnet.exe' test Hymnal.slnx --nologo --verbosity minimal`
  - `& 'C:\Program Files\dotnet\dotnet.exe' build src/Hymnal/Hymnal.csproj --nologo --verbosity minimal`
- Do not rely on WSL-hosted `gsd_exec` for .NET restore/build verification when it reproduces the known MEM008 failure (`Value cannot be null (Parameter 'path1')`). That is an environment issue with Windows `dotnet.exe` resolving package paths through WSL mounts, not a product failure.
- Use a throwaway local workspace. No secrets or PII are required.

## Starting workspace

Create a temporary workspace folder with this layout:

```text
Book.txt
front/
  part.md
  alpha.md
  beta.md
middle/
  part.md
  act-one/
    part.md
    gamma.md
    delta.md
    orphan.md
back/
  part.md
  omega.md
```

Use this `Book.txt` content:

```text
front/part.md
front/alpha.md
front/beta.md
middle/part.md
middle/act-one/part.md
middle/act-one/gamma.md
middle/act-one/delta.md
back/part.md
back/omega.md
```

Use part files with `{class: part}` in the first line, for example:

```markdown
{class: part}
# Front Matter
```

Use simple chapter bodies, for example:

```markdown
# Alpha

Original alpha body.
```

Before opening Hymnal, create or simulate existing metadata for the chapter files if your test harness supports it. At minimum, inspect after the run that `.hymnal-data/registry.json`, `.hymnal-data/exclusions.json`, notes, phase data, target data, and word-count history keep chapter identity by UUID rather than creating duplicate chapter identities.

## Script A: cross-surface structural replay

### 1. Open and inspect the workspace

1. Launch Hymnal.
2. Open the temporary workspace.
3. Confirm the sidebar order is:
   1. `front/part.md`
   2. `front/alpha.md`
   3. `front/beta.md`
   4. `middle/part.md`
   5. `middle/act-one/part.md`
   6. `middle/act-one/gamma.md`
   7. `middle/act-one/delta.md`
   8. `back/part.md`
   9. `back/omega.md`
4. Confirm Part nodes are visible but not selectable as editable chapter documents.
5. Confirm `middle/act-one/orphan.md` is not in `Book.txt` and appears as an excluded/orphan item only where the UI is intended to show excluded files.

Author-facing observations:

- Part rows should be visually distinct from chapter rows.
- Excluded/orphan chapter styling should clearly communicate “not currently in Book.txt.”
- Menu labels for remove/include/rename/reorder actions should use author-facing language, not internal model names.

### 2. Sidebar operations

Perform these operations from the sidebar:

1. Remove `front/beta.md` from the book.
2. Confirm `front/beta.md` is not lost from disk and is shown as excluded.
3. Include `front/beta.md` back into the book.
4. Rename `front/alpha.md` to `Renamed Alpha`.
5. Reorder `front/beta.md` after `front/renamed-alpha.md`.
6. Move `back/part.md` before `middle/part.md`.

Expected sidebar order after this stage:

```text
front/part.md
front/renamed-alpha.md
front/beta.md
back/part.md
back/omega.md
middle/part.md
middle/act-one/part.md
middle/act-one/gamma.md
middle/act-one/delta.md
```

Author-facing observations:

- Drag/drop affordances should make valid reorder targets apparent.
- Part-section movement should make it clear that the whole Part section moves together.
- Rename feedback should not expose raw exceptions.

### 3. Corkboard operations

Switch to Corkboard/Plan view and perform these operations:

1. Drag `front/renamed-alpha.md` after `front/beta.md` within the Front section.
2. Drag `front/renamed-alpha.md` into the Back section after `back/omega.md`.
3. Remove `middle/act-one/delta.md` from the book.
4. Confirm `middle/act-one/delta.md` appears as an excluded card.
5. Confirm `middle/act-one/orphan.md` also appears as an excluded card and is not duplicated.
6. Include `middle/act-one/orphan.md` after `middle/act-one/gamma.md`.
7. Inline-create a chapter named `Inserted Scene` after `middle/act-one/orphan.md` in the Act One section.

Expected Corkboard observations:

- Part dividers remain visually distinct from chapter cards.
- Excluded cards are styled differently from included cards.
- Include/remove menu labels should explain whether the file remains on disk.
- Valid drop targets should be discoverable while dragging.
- No duplicate card should appear for `middle/act-one/orphan.md` or any excluded file.
- No user-visible structural error should remain after the successful operations.

### 4. Gantt operations

Switch to Gantt view and perform these operations:

1. Confirm rows include the book row, Part rows, and chapter rows in the same current manuscript order.
2. Reorder `middle/act-one/gamma.md` after `middle/act-one/inserted-scene.md` using the available Gantt row movement interaction.
3. Exercise both supported row-movement affordances if present in the build under review:
   - keyboard movement commands/buttons for selected rows; and
   - drag/drop row movement if exposed.

Expected Gantt observations:

- Keyboard and/or drag affordances should only allow valid reorder moves.
- Part rows should not feel like normal chapter edit rows.
- A successful same-Part chapter move should update the sidebar and Corkboard projections without requiring a manual reload.

### 5. Expected persisted final state

After the successful replay, inspect `Book.txt`. It should contain exactly:

```text
front/part.md
front/beta.md
back/part.md
back/omega.md
back/renamed-alpha.md
middle/part.md
middle/act-one/part.md
middle/act-one/orphan.md
middle/act-one/inserted-scene.md
middle/act-one/gamma.md
```

Also verify:

- `back/renamed-alpha.md` exists.
- `front/alpha.md` and `front/renamed-alpha.md` do not exist.
- `middle/act-one/delta.md` still exists on disk but is excluded from `Book.txt`.
- `middle/act-one/inserted-scene.md` exists and contains a heading/body scaffold for `Inserted Scene`.
- `.hymnal-data/exclusions.json` contains `middle/act-one/delta.md` only.
- `.hymnal-data/registry.json` maps the original Alpha UUID to `back/renamed-alpha.md` and preserves UUIDs for Beta, Gamma, Delta, and Orphan.
- Metadata sidecars keyed by UUID, including notes, phase data, targets, and word-count history, remain readable and are not duplicated under path-derived identities.

### 6. Restart persistence

1. Quit Hymnal completely.
2. Relaunch Hymnal.
3. Reopen the same workspace.
4. Confirm the sidebar, Corkboard, and Gantt all show one consistent manuscript state matching the final `Book.txt` order.
5. Confirm excluded `middle/act-one/delta.md` appears once where excluded files are shown and does not re-enter `Book.txt`.
6. Confirm no duplicate paths appear in any surface.

## Script B: controlled structural failure visibility

Use a second throwaway workspace or reset the first one to this smaller fixture:

```text
Book.txt
front/part.md
front/alpha.md
front/beta.md
back/part.md
back/omega.md

front/part.md
front/alpha.md
front/beta.md
back/part.md
back/omega.md
back/beta.md
```

The important extra file is `back/beta.md`, which already exists on disk but is not in `Book.txt`. It forces a deterministic cross-Part move conflict if `front/beta.md` is moved into Back.

### 1. Successful setup edits

1. Open the workspace.
2. Reorder `front/beta.md` before `front/alpha.md` from the sidebar.
3. In Corkboard, move `front/alpha.md` before `front/beta.md`.
4. Confirm the stable order is back to:

```text
front/part.md
front/alpha.md
front/beta.md
back/part.md
back/omega.md
```

### 2. Corkboard controlled failure

1. In Corkboard, attempt to move `front/beta.md` into the Back section after `back/omega.md`.
2. Expected result: the operation fails because `back/beta.md` already exists.
3. Confirm the visible failure text includes:
   - operation context such as `Drop card`;
   - source path `front/beta.md`;
   - target path `back/beta.md`;
   - `Book.txt` context; and
   - a clear target-file conflict message.
4. Confirm the Corkboard diagnostic surface, if inspected in debug/test tooling, has `LastStructuralError` populated with operation, path, message, and `Book.txt` path.
5. Confirm `Book.txt`, `front/beta.md`, and `back/beta.md` are unchanged.

Author-facing observation: the error should tell the author what failed and why without suggesting that manuscript content was deleted.

### 3. Gantt controlled failure

1. In Gantt, attempt to move `front/alpha.md` after `back/omega.md`.
2. Expected result: the operation is rejected because Gantt row reorder does not move chapters across Part sections.
3. Confirm the visible failure text includes:
   - `Reorder failed` or equivalent operation context;
   - `front/alpha.md`;
   - `Book.txt` context;
   - a message that the chapter cannot be moved across Part sections; and
   - guidance to move chapters between Parts from the Corkboard.
4. Confirm `Book.txt`, sidebar order, Corkboard order, and Gantt order remain unchanged.
5. Restart Hymnal and confirm the stable order and UUID-backed metadata are still intact.

## Pass criteria

This UAT passes when:

- Sidebar, Corkboard, and Gantt all operate on the same persisted structure.
- Restart/reload shows the same manuscript order in all three surfaces.
- UUID-backed metadata continues across rename, move, exclude/include, and inline create operations.
- Watcher/reload behavior does not duplicate rows/cards or reintroduce excluded files.
- Controlled Corkboard and Gantt failures are user-visible, non-destructive, and include operation/path/message/`Book.txt` context where available.
- Author-facing polish is acceptable for excluded styling, menu labels, drag/drop affordances, Gantt keyboard/drag behavior, restart persistence, and failure message copy.

## Follow-up capture rule

If this script reveals a desirable feature outside the S08 sketch, record it as a follow-up issue or later milestone candidate. Do not expand S08 with new structural write paths during closeout.
