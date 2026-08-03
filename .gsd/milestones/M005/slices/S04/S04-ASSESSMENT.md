---
sliceId: S04
uatType: browser-executable
verdict: PASS
date: 2026-06-18T13:24:00.000Z
---

# UAT Result — S04

## Checks

| Check | Mode | Result | Notes |
|-------|------|--------|-------|
| Preconditions: workspace has at least two `{class: part}` dividers, multiple included chapters under each Part, and at least one excluded/missing projection state. | artifact | PASS | Test-backed workspaces in `WorkspaceSidebarReorderTests` and `SidebarViewSmokeTests` cover multi-Part fixtures plus excluded and missing rows. Evidence: `.gsd/exec/fe8d8125-0bd8-4e3e-b5cb-66d950499654.stdout`, `.gsd/exec/ab15745a-061b-429f-9f61-cf1ae8361638.stdout`. |
| Drag an included chapter row to another position within the same Part section. Expected: legal same-Part target, `Book.txt` order changes, sidebar reloads, no duplicates. | runtime | PASS | `ReorderChapterCommand_ReordersIncludedChapterWithinSamePartUsingActiveBookTxtIndexAndReloadsWithoutDuplicates` verifies the reorder call, watcher suppression, persisted `Book.txt` order, reload order, and distinct visible nodes. Core `ReorderEntryAsync_MovesEntryWithinPart` verifies canonical Book.txt mutation. Prior task-session native Windows dotnet evidence passed. |
| Restart or reload the workspace after same-Part chapter reorder. Expected: order persists from `Book.txt`. | runtime | PASS | The same WorkspaceViewModel test reopens the workspace after mutation and asserts visible order matches the reordered Book.txt, including excluded projection, with no duplicate paths. |
| Drag an included Part divider relative to another Part divider. Expected: Part divider and child entries move as a single block; `Book.txt` reflects moved block. | runtime | PASS | `ReorderChapterCommand_ReordersIncludedPartAsWholeBlockWhenDroppedOnPartDivider`, `ReorderEntryAsync_MovesPartBlockBeforeAnotherPartBlock`, and `ReorderEntryAsync_MovesPartBlockAfterAnotherPartBlockAndPreservesBlankLines` verify Part-block movement and child preservation. |
| Restart or reload after Part-block reorder. Expected: block order persists and sidebar has one node per active Book.txt entry plus expected projections, with no duplicates. | runtime | PASS | Workspace test reopens after Part-block reorder and asserts visible order equals `Book.txt`, and Core test `ReorderEntryAsync_PartBlockPreservesAllEntriesWithoutDuplicatesAfterReload` asserts normalized reload contains all entries exactly once. |
| Attempt to drag an excluded or missing row. Expected: no drag starts or drop rejected; no Book.txt mutation. | runtime | PASS | `CanDragFromSidebar_AllowsOnlyIncludedPresentChaptersAndParts` and `CanDragFromSidebar_RejectsBlankPaths` reject invalid sources at the view predicate. `ReorderChapterCommand_RejectsExcludedSourceBeforeCallingCore` and `ReorderChapterCommand_RejectsMissingSourceBeforeCallingCore` assert no Core reorder calls, user-visible errors, and unchanged Book.txt. |
| Attempt to drop a chapter onto a target in a different Part. Expected: UI rejects before Core mutation and surfaces user-visible error. | runtime | PASS | `CanDropOnSidebar_AllowsChapterWithinCurrentPartOnly` rejects cross-Part chapter targets in the view predicate; `ReorderChapterCommand_RejectsCrossPartChapterMovementWithUserVisibleNotificationBeforeCallingCore` verifies no Core call, notification text, and unchanged Book.txt. |
| Attempt to drop a Part onto a chapter, or a chapter onto a Part divider. Expected: mismatched-kind drop rejected; Book.txt unchanged. | runtime | PASS | `CanDropOnSidebar_AllowsPartOnPartButNotChapterTargets` and `CanDropOnSidebar_AllowsChapterWithinCurrentPartOnly` cover view-level rejection. `ReorderChapterCommand_RejectsPartDropOntoChapterBeforeCallingCore` verifies no Core call, user-visible error, and unchanged Book.txt. The WorkspaceViewModel implementation also rejects chapter-on-Part with an explicit error. |
| Simulate or induce downstream Core reorder failure after a legal drag intent. Expected: user notified with Core failure context, watcher suppression released, Book.txt unchanged, sidebar unchanged without duplicates. | runtime | PASS | `ReorderChapterCommand_CoreFailureShowsErrorAndLeavesVisibleOrderUnchanged` injects `Result.Fail`, asserts watcher suppression was active for the attempted call, verifies notification includes Core failure context, and asserts unchanged Book.txt/sidebar order with distinct visible nodes. |
| Edge cases: self-drops ignored/rejected, blank/inactive paths invalid, excluded projections ignored for active index calculation, Part moves after invalid final target rejected before mutation. | artifact | PASS | `CanDropOnSidebar_RejectsSelfDropAndInactiveRows`, `CanDragFromSidebar_RejectsBlankPaths`, same-Part active-index reorder test with an excluded projection, and `CanDropOnSidebar_RejectsPartDropAfterLastPartBecauseViewModelRejectsIt` cover these edge cases. |
| Acceptance: automated tests remain green. | runtime | PASS | Current `gsd_exec` cannot run dotnet in this lane (`dotnet: command not found` from `.gsd/exec/170877f9-80aa-455c-87e2-f5309f495254.stdout`; known environment limitation). Prior native Windows dotnet task-session evidence records focused S04 suites passing: BookTxtStructureServiceTests, WorkspaceSidebarReorderTests, and SidebarViewSmokeTests reported 73 passed, 0 failed in T04 summary; T01/T02/T03 summaries also record targeted pass evidence. |

## Overall Verdict

PASS — All automatable S04 UAT checks are covered by executable Core/ViewModel/View predicate tests and prior native dotnet evidence; manual desktop UAT remains intentionally deferred to S08 integrated structural consistency UAT.

## Notes

- No browser target exists for this Avalonia desktop slice despite the detected `browser-executable` mode, so evidence was gathered through artifact/runtime test coverage rather than browser automation.
- A fresh `gsd_exec` dotnet attempt was made and could not execute because this lane exposes `/mnt/c/Dev/Hymnal` without `dotnet` on PATH; this matches the project’s known closeout verification limitation. The UAT verdict therefore uses current source/test inspection plus existing task-session native Windows dotnet evidence.
- Evidence artifacts created during this UAT run: `.gsd/exec/86235968-3e68-4ab4-bd1c-60619b700c89.stdout`, `.gsd/exec/fe8d8125-0bd8-4e3e-b5cb-66d950499654.stdout`, `.gsd/exec/f3cc713f-aadb-4b53-a7a1-df67af79b677.stdout`, and `.gsd/exec/ab15745a-061b-429f-9f61-cf1ae8361638.stdout`.
