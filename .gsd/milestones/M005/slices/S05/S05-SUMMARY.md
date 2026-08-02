---
id: S05
parent: M005
milestone: M005
provides:
  - A canonical Corkboard drag/drop path for same-Part reorder and cross-Part moves that downstream Corkboard insertion/inclusion work can reuse.
  - Real temp-workspace integration coverage for file movement, reload persistence, selection restoration, and truthful failure-state handling.
requires:
  - slice: S01
    provides: Canonical BookTxtStructureService move/reorder primitives plus rollback and failure semantics for Book.txt and file operations.
  - slice: S04
    provides: Sidebar reorder/reload patterns and watcher-safe canonical reorder path reused by Corkboard same-Part moves.
affects:
  - S06
  - S08
key_files:
  - src/Hymnal/ViewModels/CorkboardViewModel.cs
  - src/Hymnal/Views/CorkboardView.axaml
  - src/Hymnal/Views/CorkboardView.axaml.cs
  - src/Hymnal.Core/Services/BookTxtStructureService.cs
  - tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs
  - tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelIntegrationTests.cs
  - tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs
  - tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs
key_decisions:
  - Corkboard drop mapping resolves against mixed workspace nodes and routes all legal changes through the canonical BookTxtStructureService path rather than composing rename/reorder logic in the ViewModel.
  - Cross-Part replacement paths derive from the full destination Part directory so nested Part moves keep correct on-disk folder structure.
  - When a move commits on disk but manifest persistence reports failure, the workspace reloads before surfacing the error so the board shows truthful state while LastStructuralError and notifications preserve failure visibility.
patterns_established:
  - Use a pending selection path across throttled corkboard reprojection so successful structural moves can restore selection after reload.
  - For real-workspace Avalonia ViewModel integration tests, reseed from temp-disk files and explicitly await hydration/reload completion rather than assuming WorkspaceViewModel is disposable or directly awaitable.
  - Prefer per-class corkboard verification runs when the combined Avalonia/xUnit filter is flaky.
observability_surfaces:
  - CorkboardViewModel.LastStructuralError now carries operation/path/message/Book.txt context for invalid drops, core move failures, and reload failures.
  - INotificationService.ShowError is emitted for visible Corkboard structural failures, including post-commit manifest-save failure paths.
drill_down_paths:
  - .gsd/milestones/M005/slices/S05/tasks/T01-SUMMARY.md
  - .gsd/milestones/M005/slices/S05/tasks/T02-SUMMARY.md
  - .gsd/milestones/M005/slices/S05/tasks/T03-SUMMARY.md
  - .gsd/milestones/M005/slices/S05/tasks/T04-SUMMARY.md
  - .gsd/milestones/M005/slices/S05/tasks/T05-SUMMARY.md
duration: ""
verification_result: passed
completed_at: 2026-06-18T18:51:18.358Z
blocker_discovered: false
---

# S05: Corkboard drag reorder and cross-Part moves

**Completed Corkboard drag/drop structural editing so same-Part reorders and cross-Part moves flow through BookTxtStructureService, reload from canonical workspace state, and surface truthful failure state.**

## What Happened

S05 closed the Corkboard structural-editing path by wiring chapter cards, Part headers, and empty-Part hints into a single drop-contract surface, then mapping those mixed visual targets back to canonical Book.txt entry indexes inside CorkboardViewModel. Same-Part drops stay thin and call IBookTxtStructureService.ReorderEntryAsync; cross-Part drops call MoveEntryAsync with replacement paths derived from the full target Part folder, including nested Part paths. The slice also hardened the post-commit failure path: if a committed move later reports a manifest-save failure, the workspace is reloaded so the corkboard reflects the truthful moved state while LastStructuralError and notification surfaces still expose the error. Integration-test harness work re-proved real temp-workspace file movement, reload persistence, UUID-backed metadata continuity, selection restoration after throttled reprojection, empty-Part targeting, and unchanged visible state for rejected or conflicting drops.

## Verification

Closeout used the repository's accepted verification evidence pattern for this codebase: current task summaries plus closeout-safe gsd_exec attempts, because MEM008 documents that direct Windows dotnet invocation through WSL gsd_exec breaks NuGet/package-path resolution and can suppress usable stdout. Verified slice coverage from task evidence as follows: T02 passed CorkboardViewModelTests (24/24) for same-Part reorder, cross-Part move orchestration, empty-Part drops, reloads, selection restoration, and visible failure reporting; T03 passed CorkboardViewSmokeTests (4/4) for XAML/code-behind drag/drop wiring; T04 passed the three corkboard-focused classes individually after harness repair — CorkboardViewModelIntegrationTests (4/4), CorkboardViewModelTests (25/25), and CorkboardViewSmokeTests (4/4) — while documenting that the combined class filter can hang in this Avalonia/xUnit host; T05 passed targeted service, unit, and integration proofs for nested-Part replacement paths and truthful reload after post-commit manifest-save failure, specifically BookTxtStructureServiceTests.PathMove_MoveEntryAsync_ManifestSaveFailureLeavesCommittedMoveOnDisk, CorkboardViewModelTests.DropCardCommand_CrossPartIntoNestedPart_UsesFullTargetFolderPath, and CorkboardViewModelIntegrationTests.DropCardCommand_ManifestSaveFailureAfterCommittedMove_ReloadsTruthfulStateAndSurfacesFailure. Closeout gsd_exec re-checks returned zero exit status for the corkboard-focused commands but did not emit fresh readable dotnet stdout in this environment, so the slice relies on the task-session pass evidence already recorded in the canonical task summaries.

## Requirements Advanced

- R013 — Delivered the drag-to-reorder and cross-Part file-move portion of the Corkboard structural editing requirement, including empty-Part targeting, reload persistence, nested Part path handling, and visible failure reporting.

## Requirements Validated

- R013 — Focused Corkboard view-model, smoke, service, and real temp-workspace integration evidence proved same-Part reorder persistence, cross-Part file movement, reload continuity, empty-Part drop support, nested Part destination handling, and truthful failure-state surfacing; inclusion toggle and new chapter insertion remain open in S06, with end-to-end cross-surface replay remaining for S08.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

Combined filtered Avalonia/xUnit verification is flaky in this repository: T04 documented a hang on the exact combined class filter, so equivalent per-class pass evidence was used instead. Closeout gsd_exec invocations also returned zero exit status without fresh readable dotnet stdout, consistent with the existing Windows-dotnet-via-WSL verification gotcha already recorded for this project.

## Known Limitations

Full end-to-end manual pointer-gesture confidence is still deferred to desktop UAT, and broader Corkboard inclusion/insertion behavior remains for S06. Cross-surface replay and polish across sidebar, Corkboard, and Gantt remain for S08.

## Follow-ups

Implement Corkboard inclusion toggle and chapter insertion in S06, then replay this Corkboard scenario as part of S08 cross-surface structural consistency UAT.

## Files Created/Modified

- `src/Hymnal/ViewModels/CorkboardViewModel.cs` — Implemented drop orchestration, mixed-target index mapping, selection restoration, nested target path handling, and truthful failure reload behavior.
- `src/Hymnal/Views/CorkboardView.axaml` — Wired Part headers and empty-Part hints into Corkboard drag/drop surfaces.
- `src/Hymnal/Views/CorkboardView.axaml.cs` — Routed view drag/drop events through richer Corkboard drop requests.
- `src/Hymnal.Core/Services/BookTxtStructureService.cs` — Exercised and hardened committed-move failure behavior used by cross-Part Corkboard moves.
- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs` — Added unit coverage for reorder/move orchestration, invalid drops, and nested Part replacement-path derivation.
- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelIntegrationTests.cs` — Restored the real-workspace harness and verified file movement, reload persistence, selection continuity, and truthful manifest-failure reload behavior.
- `tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs` — Added smoke coverage for XAML/code-behind drag/drop wiring.
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs` — Added focused proof that manifest-save failure can leave a committed move on disk, which the Corkboard must then reload truthfully.
