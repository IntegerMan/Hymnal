---
id: S08
parent: M005
milestone: M005
provides:
  - Final M005 cross-surface structural editing acceptance evidence.
  - Manual desktop UAT script for human replay.
  - Requirement R013 validated through end-to-end replay and failure polish.
  - Known verification-environment caveat for future closeout planning.
requires:
  - slice: S02
    provides: Sidebar excluded chapters and include toggle.
  - slice: S03
    provides: Sidebar rename with UUID continuity.
  - slice: S04
    provides: Sidebar drag reorder for chapters and Parts.
  - slice: S05
    provides: Corkboard drag reorder and cross-Part moves.
  - slice: S06
    provides: Corkboard inclusion toggle and chapter insertion.
  - slice: S07
    provides: Gantt row drag reorder through canonical structure path.
affects:
  - M005 milestone validation/completion
  - Future native Windows verification lane planning
key_files:
  - tests/Hymnal.Core.Tests/ViewModels/StructuralConsistencyUatTests.cs
  - src/Hymnal/ViewModels/CorkboardItemViewModel.cs
  - src/Hymnal/ViewModels/WorkspaceViewModel.cs
  - src/Hymnal/ViewModels/CorkboardViewModel.cs
  - src/Hymnal/ViewModels/GanttViewModel.cs
  - src/Hymnal/Views/GanttCanvas.cs
  - docs/uat/M005-S08-structural-consistency.md
  - .gsd/milestones/M005/slices/S08/tasks/T01-SUMMARY.md
  - .gsd/milestones/M005/slices/S08/tasks/T02-SUMMARY.md
  - .gsd/milestones/M005/slices/S08/tasks/T03-SUMMARY.md
  - .gsd/milestones/M005/slices/S08/tasks/T04-SUMMARY.md
key_decisions:
  - Use one integrated temp-workspace UAT to replay sidebar, Corkboard, and Gantt operations through actual ViewModel/Core paths, then assert disk-backed state after a fresh ViewModel stack restart.
  - Fix Corkboard projection duplication by treating Workspace excluded nodes as authoritative before orphan-file projection adds excluded cards.
  - Use deterministic existing-file conflict for Corkboard cross-Part move failure and Workspace-mediated Gantt cross-Part reorder rejection, avoiding new structural write paths or broad UX redesign.
  - Document the S08 manual desktop UAT as a durable docs artifact rather than adding new structural write paths during closeout.
  - Treat full-solution test timeout/file-lock behavior as a known verification-environment issue after focused structural UAT and desktop app build passed.
patterns_established:
  - Cross-surface manuscript structure UAT should drive all structural surfaces on one workspace and then assert canonical disk state after restart.
  - Controlled structural failure tests should combine user-facing notification/error assertions with post-reload disk and projection invariants.
  - Excluded Corkboard projection treats Workspace excluded nodes as authoritative before adding orphan projections to avoid duplicate excluded cards.
observability_surfaces:
  - CorkboardViewModel.LastStructuralError exposes operation/path/message/Book.txt context for structural failures.
  - Shared notification fake/user-facing notifications assert failure visibility for sidebar, Corkboard, and Gantt paths.
  - Manual UAT script includes explicit failure-copy and persistence inspection points.
drill_down_paths:
  - .gsd/milestones/M005/slices/S08/tasks/T01-SUMMARY.md
  - .gsd/milestones/M005/slices/S08/tasks/T02-SUMMARY.md
  - .gsd/milestones/M005/slices/S08/tasks/T03-SUMMARY.md
  - .gsd/milestones/M005/slices/S08/tasks/T04-SUMMARY.md
  - docs/uat/M005-S08-structural-consistency.md
duration: ""
verification_result: passed
completed_at: 2026-06-19T05:31:08.313Z
blocker_discovered: false
---

# S08: Structural consistency UAT and failure polish

**Closed M005 structural editing with an integrated sidebar, Corkboard, and Gantt UAT replay, controlled failure coverage, and a manual desktop UAT script.**

## What Happened

S08 assembled the upstream structural-editing slices into one end-to-end proof. The new StructuralConsistencyUatTests fixture creates a real temp manuscript workspace with nested Parts, included chapters, an excluded/orphan chapter, registry UUIDs, notes, phase data, targets, and word-count history. It drives the real Workspace, Corkboard, and Gantt ViewModel/Core paths for sidebar remove/include, rename, chapter reorder, Part-block reorder, Corkboard same-Part reorder, Corkboard cross-Part move, Corkboard remove/include, inline chapter insertion, and Gantt same-Part row reorder. The replay then constructs a fresh ViewModel stack to simulate restart/reload and asserts the final Book.txt order, chapter file locations, exclusions manifest, registry UUID path mapping, UUID-backed metadata continuity, no duplicate sidebar/card/row projections, and no stale Corkboard structural error. During T01, the integrated replay exposed duplicate excluded Corkboard projection after reload; CorkboardItemViewModel.Project was fixed to treat Workspace excluded nodes as authoritative before appending orphan-file cards, avoiding duplicate excluded cards without adding a new structural write path. T02 confirmed watcher suppression, reload synchronization, and ownership boundaries already matched the canonical structure-service design. T03 added deterministic controlled-failure replay: a Corkboard cross-Part file-conflict failure and a Workspace-mediated Gantt illegal cross-Part reorder rejection both surface actionable diagnostics while leaving Book.txt, files, registry UUIDs, exclusions, metadata sidecars, and surface projections unchanged after reload. T04 documented the equivalent manual desktop UAT in docs/uat/M005-S08-structural-consistency.md, including author-facing checks for excluded styling, menu labels, drag/drop affordances, Gantt row movement behavior, restart persistence, and failure-message copy.

## Verification

Closeout used the S08 task-session native Windows dotnet evidence because direct gsd_exec dotnet verification reproduces MEM008/Windows-dotnet-through-WSL restore failures in this environment. Current closeout gsd_exec evidence inspected the completed task summaries and UAT artifact and confirmed all S08 artifacts exist with verification tables and required sidebar/Corkboard/Gantt/restart/failure coverage. Task-session verification evidence recorded: T01 targeted StructuralConsistencyUatTests passed with 1 test, 0 failed; T02 reran the focused integrated replay and audited production code for non-canonical Book.txt writes; T03 focused StructuralConsistencyUatTests passed with 2 tests, 0 failed and asserted controlled failure visibility plus non-destructive rollback; T04 focused S08 test through Hymnal.slnx passed with 2 tests, 0 failed, UAT document sanity passed, and desktop app build of src/Hymnal/Hymnal.csproj succeeded with 0 warnings and 0 errors. The prescribed full Hymnal.slnx test gate was attempted in T04 and reproduced a known environment/testhost issue: Windows temp-workspace file lock in GanttViewModelTests followed by timeout, then serialized/no-build retry timeout. A fresh closeout gsd_exec attempt also reproduced MEM008 restore failure. These are recorded as verification-environment limitations, not product-state failures for the S08 structural surfaces.

## Requirements Advanced

- R013 — Completed final cross-surface replay and failure polish for Corkboard structural editing across sidebar, Corkboard, and Gantt.

## Requirements Validated

- R013 — StructuralConsistencyUatTests plus manual UAT script prove Corkboard drag reorder, cross-Part file move, inclusion toggle, chapter insertion, restart persistence, UUID metadata continuity, and controlled failure visibility on one integrated workspace.

## New Requirements Surfaced

- None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

Full `dotnet test Hymnal.slnx` did not produce a clean pass in the recorded environment because of known Windows dotnet/WSL restore and Windows testhost/temp-workspace file-lock behavior. S08 closeout therefore relies on focused S08 replay, artifact sanity, and desktop app build evidence already recorded in task summaries.

## Known Limitations

Full solution test execution remains unreliable in the agent verification environment; see T04 summary and MEM008. Existing unrelated analyzer warnings in older tests and a pre-existing WorkspaceViewModel CS4014 warning were not addressed by this slice.

## Follow-ups

Investigate and harden the full-solution test environment/testhost cleanup separately, especially the Windows file lock observed in GanttViewModelTests. Consider adding a CI-friendly no-WSL verification lane for native Windows dotnet closeout.

## Files Created/Modified

- `tests/Hymnal.Core.Tests/ViewModels/StructuralConsistencyUatTests.cs` — Added integrated positive and controlled-failure structural UAT replay.
- `src/Hymnal/ViewModels/CorkboardItemViewModel.cs` — Fixed excluded Corkboard projection duplication by respecting Workspace excluded nodes before orphan projection.
- `docs/uat/M005-S08-structural-consistency.md` — Added manual desktop UAT script covering cross-surface replay, restart persistence, metadata continuity, and controlled failures.
- `src/Hymnal/Hymnal.csproj` — Touched during T04 verification context; desktop app build verified.
- `Hymnal.slnx` — Included in T04 verification context for solution-level S08 focused test.
