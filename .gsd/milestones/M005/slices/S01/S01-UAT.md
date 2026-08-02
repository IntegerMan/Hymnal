# S01: Atomic structure core and exclusion manifest — UAT

**Milestone:** M005
**Written:** 2026-06-18T03:27:17.675Z

## UAT Type

Automated Core contract UAT. Desktop UI UAT is intentionally deferred to later M005 UI slices; this slice proves the Core operations that those surfaces will call.

## Preconditions

- A test workspace contains `Book.txt` with Part-based chapter entries and corresponding markdown files.
- `.hymnal-data/registry.json` contains UUID-backed chapter identity for at least one movable chapter.
- `.hymnal-data/exclusions.json` may be absent, present, stale, malformed, or contain existing intentional exclusions depending on the scenario.
- Verification is run in a normal developer shell for dotnet commands; the gsd_exec closure lane is known to fail this repo's dotnet restore with `Value cannot be null. (Parameter 'path1')`.

## Steps and Expected Outcomes

1. **Load missing and populated exclusion manifests**
   - Load a workspace with no manifest.
   - Expected: service returns an empty schema-versioned manifest without creating a file.
   - Save exclusions, reload, and inspect normalized paths.
   - Expected: `.hymnal-data/exclusions.json` is written atomically with schemaVersion and forward-slash relative paths.

2. **Handle stale and invalid manifest entries**
   - Add entries for files that no longer exist and paths with duplicate case variants.
   - Load passively.
   - Expected: missing files are omitted from the loaded model without rewriting the manifest.
   - Perform a user-driven include or exclude save.
   - Expected: stale entries are pruned on save and duplicate paths are de-duplicated case-insensitively.
   - Try absolute or traversal paths.
   - Expected: operation returns `Result.Fail` and no filesystem write occurs.

3. **Exclude an included chapter**
   - Call `ExcludeEntryAsync` for a chapter present in Book.txt.
   - Expected: Book.txt no longer contains the chapter entry, the markdown file remains on disk, and the normalized path is added to exclusions.json.
   - Force Book.txt write failure.
   - Expected: method fails during the Book.txt phase and the manifest remains untouched.
   - Force manifest save failure after Book.txt succeeds.
   - Expected: method returns a phase-aware failure that later UI can show.

4. **Include an intentionally excluded chapter**
   - Call `IncludeExistingEntryAsync` or `IncludeExistingEntryAfterPartAsync` for an excluded markdown file.
   - Expected: Book.txt receives the entry at the requested index or after the requested Part, and the path is removed from exclusions.json.
   - Try including a missing file, duplicate Book.txt entry, invalid index, missing Part, or outside-workspace path.
   - Expected: each returns `Result.Fail` without silently mutating unrelated resources.

5. **Move a chapter across Part folders**
   - Call `MoveEntryAsync` with a source chapter, target Part-relative markdown path, and final order index.
   - Expected: source file is moved, target file appears, Book.txt contains the target path in the requested order, old path disappears from Book.txt, stale target exclusions are removed, and registry UUID identity is preserved for the moved chapter.
   - Reload normalized Book.txt entries.
   - Expected: the persisted order and path are stable after reload.

6. **Exercise failure and rollback paths**
   - Attempt a move where the target file already exists.
   - Expected: operation fails before move/write, with no auto-suffix, overwrite, or merge.
   - Force a Book.txt write failure after file move.
   - Expected: service attempts to move the file back, returns an explicit failure, and indicates whether rollback restored state.
   - Force rollback failure.
   - Expected: failure message identifies source path, target path, phase, and unrecovered state.
   - Create ambiguous or conflicting registry identity before a path change.
   - Expected: service fails rather than silently creating or guessing a UUID.

## Acceptance Evidence

- ExclusionManifestService tests cover round-trip persistence, stale load tolerance, mutation-time pruning, malformed JSON, invalid paths, atomic save failure, include removal, and orphan-discovery independence.
- BookTxtStructureService tests cover IncludeExclude and PathMove behavior, including conflict, rollback, registry continuity, and integrated reload-style reads.
- Task-level shell verification passed the full Core suite with 301 tests and the desktop app build with 0 errors.

## Edge Cases Covered

- Missing manifest and malformed manifest JSON.
- Duplicate exclusions with case variants.
- Stale exclusions for externally deleted files.
- Absolute paths, traversal paths, outside-workspace paths, and missing markdown files.
- Duplicate Book.txt entries and invalid insertion positions.
- Cross-Part file move success.
- Target file conflict.
- Book.txt write failure after filesystem move.
- Rollback failure after move failure.
- Registry save failure and ambiguous UUID preservation.

## Deferred UAT

- Visual sidebar styling and include toggle behavior: S02.
- Sidebar rename and drag reorder: S03-S04.
- Corkboard drag/drop, inclusion toggle, and insertion UI: S05-S06.
- Gantt row drag reorder and full cross-surface restart UAT: S07-S08.
