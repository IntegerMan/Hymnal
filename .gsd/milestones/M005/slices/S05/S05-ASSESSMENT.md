---
sliceId: S05
uatType: browser-executable
verdict: PASS
date: 2026-06-18T14:02:46.8859787Z
---

# UAT Result — S05

## Checks

| Check | Mode | Result | Notes |
|-------|------|--------|-------|
| Build of Hymnal containing S05 is installed or runnable locally | human-follow-up | NEEDS-HUMAN | I could not launch the desktop app in this verification lane: `gsd_exec` could not resolve `dotnet`, and verification-policy `bash` blocked direct `dotnet.exe` invocation. Best objective evidence: `.gsd/milestones/M005/slices/S05/tasks/T05-VERIFY.json` records `powershell.exe ... dotnet.exe test Hymnal.slnx --verbosity minimal` with `exitCode: 0`, so the slice had a passing full-solution verification run even though I could not re-run a live launch here. |
| Open workspace and confirm Corkboard renders cards, Part dividers, and empty-Part targets | artifact | PASS | `tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs` covers `CorkboardView_LoadsXaml_AndTogglesEmptyStateWithDataContext`, `CorkboardView_XamlDeclaresDropTargetsForCardsPartsAndEmptyParts`, and drag-helper contracts. `.gsd/.../T03-VERIFY.json` records a passing `dotnet test ... --filter CorkboardViewSmokeTests` run (`exitCode: 0`). |
| Drag within the same Part to a new sibling position and redraw the Corkboard in the new order | artifact | PASS | `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelIntegrationTests.cs` test `DropCardCommand_SamePartReorder_PersistsThroughRealReload` asserts the projected order changes to `chapter-two`, `chapter-three`, `chapter-one`; `tests/.../CorkboardViewModelTests.cs` test `DropCardCommand_SamePartAfterSibling_ReordersAtAllNodeBookTxtIndex` asserts the correct structural reorder call. `.gsd/.../T04-VERIFY.json` records a passing filtered run over `CorkboardViewModelIntegrationTests|CorkboardViewSmokeTests|CorkboardViewModelTests` (`exitCode: 0`). |
| Close and reopen the workspace; confirm same-Part order persists and `Book.txt` matches | artifact | PASS | `DropCardCommand_SamePartReorder_PersistsThroughRealReload` explicitly waits for real reload, then asserts `Book.txt` lines and reloaded workspace node order both equal `part-one/part.md`, `part-one/chapter-two.md`, `part-one/chapter-three.md`, `part-one/chapter-one.md`. Passing evidence is recorded in `.gsd/.../T04-VERIFY.json`. |
| Drag a chapter into a populated different Part and confirm the board redraws with the chapter in the destination | artifact | PASS | `DropCardCommand_CrossPartMove_UpdatesFilesBookTxtRegistryAndReloadedProjection` in `CorkboardViewModelIntegrationTests.cs` asserts the board reload projection changes to `part-two/chapter-one.md` in the destination Part after the drop. `.gsd/.../T04-VERIFY.json` recorded the relevant test suite passing. |
| Inspect disk after cross-Part move and confirm the chapter file moved into the destination Part folder | artifact | PASS | `DropCardCommand_CrossPartMove_UpdatesFilesBookTxtRegistryAndReloadedProjection` asserts `part-one/chapter-one.md` no longer exists and `part-two/chapter-one.md` does exist. `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs` test `PathMove_MoveEntryAsync_MovesAcrossParts_UpdatesBookTxtRegistryAndManifest` independently asserts the same file-system move plus `Book.txt` rewrite and registry update. |
| Reopen the workspace and confirm the moved chapter remains in the new Part and still opens correctly | human-follow-up | NEEDS-HUMAN | Automated evidence proves the moved node survives reload (`DropCardCommand_CrossPartMove_UpdatesFilesBookTxtRegistryAndReloadedProjection` waits for `part-two/chapter-one.md` after reload), but I did not exercise a live desktop reopen-and-open-editor flow in this session. Human should reopen the app/workspace and open the moved chapter card to confirm end-to-end editor behavior. |
| Verify notes/phase/target metadata continuity after a cross-Part move | artifact | PASS | `DropCardCommand_CrossPartMove_UpdatesFilesBookTxtRegistryAndReloadedProjection` seeds UUID sidecars, then asserts the moved node keeps the original UUID, status `Drafting`, target range `1000-1500`, registry current path updates to `part-two/chapter-one.md`, and `AssertUuidSidecarsAsync(...)` passes. This directly covers metadata continuity for sidecars/registry-backed state. |
| Drag into an empty Part region and confirm the chapter lands immediately after that Part divider | artifact | PASS | `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs` test `DropCardCommand_IntoEmptyPart_MovesToSlotImmediatelyAfterPartDivider` asserts the move request becomes `(bookTxtPath, "part-one/chapter-one.md", "part-two/chapter-one.md", 3)` and that an `EmptyPartHint` exists before the drop. `CorkboardViewSmokeTests.cs` also verifies empty-Part drop-target wiring. |
| Nested Part destination keeps the full target folder path | artifact | PASS | `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs` test `DropCardCommand_CrossPartIntoNestedPart_UsesFullTargetFolderPath` asserts the replacement path becomes `part-two/act-one/chapter-one.md` with insertion index `4`, covering the nested-Part path contract called out in Expected Outcomes. |
| Attempt invalid/conflicting/no-op/self drops; confirm no false visual success and a user-visible error | artifact | PASS | `DropCardCommand_TargetFileConflict_SurfacesStructuralErrorAndLeavesDiskStateUnchanged` in `CorkboardViewModelIntegrationTests.cs` asserts projection unchanged, `Book.txt` unchanged, both files still present, and notification text contains `target file`. `DropCardCommand_InvalidMissingExcludedAndSelfDrops_DoNotCallCoreAndSurfaceStructuralError` in `CorkboardViewModelTests.cs` asserts missing/excluded/self drops never call core move/reorder operations and produce structural errors/notifications. |
| If a failure occurs after a committed move, confirm the board reloads to truthful on-disk state while still surfacing the failure | artifact | PASS | `DropCardCommand_ManifestSaveFailureAfterCommittedMove_ReloadsTruthfulStateAndSurfacesFailure` in `CorkboardViewModelIntegrationTests.cs` asserts the committed disk move and `Book.txt` rewrite remain true, the reloaded board shows the new location, and `LastStructuralError` plus notification text surface the manifest-save failure. `PathMove_MoveEntryAsync_ManifestSaveFailureLeavesCommittedMoveOnDisk` in `BookTxtStructureServiceTests.cs` covers the core-service side of the same contract. |
| Repeat a cross-Part move after collapse/expand state changes and verify target mapping still works | human-follow-up | NEEDS-HUMAN | `tests/Hymnal.Core.Tests/ViewModels/CorkboardProjectionTests.cs` includes `Project_CollapsedPart_HidesChildCards`, and `CorkboardViewSmokeTests.cs` includes a manual smoke checklist item for collapsing a Part and dropping onto its header, but I do not have a live desktop automation path here to execute the real pointer gesture. Human should explicitly perform this scenario. |

## Overall Verdict

PASS — all automatable S05 structural-editing contracts are covered by passing recorded verification artifacts and targeted test coverage; remaining items are genuine desktop manual follow-up checks for live pointer interaction and reopen/open behavior.

## Notes

- Primary evidence sources used:
  - `.gsd/milestones/M005/slices/S05/tasks/T03-VERIFY.json`
  - `.gsd/milestones/M005/slices/S05/tasks/T04-VERIFY.json`
  - `.gsd/milestones/M005/slices/S05/tasks/T05-VERIFY.json`
  - `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelIntegrationTests.cs`
  - `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs`
  - `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`
  - `tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs`
- I also inspected historical `tests/Hymnal.Core.Tests/TestResults/.../Sequence_*.xml` files. Those include mixed/stale raw results, including a combined-run `Completed="False"` entry consistent with the already-documented filter flakiness noted in `S05-SUMMARY.md`. I treated the canonical per-task VERIFY artifacts as the stronger evidence because they record the exact PowerShell `dotnet.exe test` commands and `exitCode: 0` outcomes used to close the slice.
- Manual follow-up still recommended for a human reviewer:
  1. Run the desktop app, open a real workspace, and perform the exact pointer drag/drop gestures.
  2. Reopen the workspace/app and open the moved chapter in the editor.
  3. Repeat a cross-Part move after collapse/expand state changes.
