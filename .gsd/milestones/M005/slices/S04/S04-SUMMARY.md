---
id: S04
parent: M005
milestone: M005
provides:
  - Canonical Part-block reorder behavior in BookTxtStructureService for downstream Corkboard and Gantt consumers.
  - Sidebar drag predicate and ViewModel validation examples for future structural surfaces.
  - Reload-persistence and duplicate-node regression coverage for reorder operations.
requires:
  - slice: S01
    provides: Atomic Book.txt structure service, exclusion manifest, and consistency boundaries.
  - slice: S02
    provides: Excluded/missing sidebar projections and include/exclude behavior.
  - slice: S03
    provides: Sidebar structural command patterns, rename continuity, and watcher-suppressed reload behavior.
affects:
  - S05
  - S07
  - S08
key_files:
  - src/Hymnal.Core/Services/BookTxtStructureService.cs
  - src/Hymnal/ViewModels/WorkspaceViewModel.cs
  - src/Hymnal/Views/SidebarView.axaml.cs
  - tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs
  - tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarReorderTests.cs
  - tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs
key_decisions:
  - Kept the existing ReorderEntryAsync API and dispatched Part divider sources to a Part-block move based on `{class: part}` validation.
  - Kept the existing ReorderCardRequest command surface for sidebar reorder while resolving through active Book.txt validation before mutation.
  - Reloaded the workspace from canonical Book.txt after successful sidebar reorder rather than relying on lightweight visible-node merge.
  - Mirrored WorkspaceViewModel reorder legality in SidebarView predicates so illegal gestures do not show move affordances or invoke mutation commands.
patterns_established:
  - Structural UI surfaces should validate legal drag intent before Core mutation and then call the canonical BookTxtStructureService path.
  - Successful sidebar structural writes should suppress watchers, mutate Book.txt through Core, and reload workspace state from disk.
  - Part reorder means moving the divider and its child entries as an indivisible contiguous Book.txt block.
observability_surfaces:
  - User-visible notifications for illegal sidebar drop attempts and downstream Core reorder failures.
  - Automated tests asserting unchanged Book.txt/sidebar state on rejected or failed reorder.
  - No new telemetry or health endpoint; this is a desktop-only UI/Core slice.
drill_down_paths:
  - .gsd/milestones/M005/slices/S04/tasks/T01-SUMMARY.md
  - .gsd/milestones/M005/slices/S04/tasks/T02-SUMMARY.md
  - .gsd/milestones/M005/slices/S04/tasks/T03-SUMMARY.md
  - .gsd/milestones/M005/slices/S04/tasks/T04-SUMMARY.md
duration: ""
verification_result: passed
completed_at: 2026-06-18T13:15:49.881Z
blocker_discovered: false
---

# S04: Sidebar drag reorder for chapters and Parts

**Sidebar drag reorder now supports included chapter same-Part moves and included Part block moves through the canonical Book.txt structure path, with illegal drops rejected visibly and reload persistence covered by tests.**

## What Happened

S04 extended the existing Book.txt structure path and sidebar reorder orchestration to support drag reorder for both chapters and Part dividers. Core now detects `{class: part}` source entries in `BookTxtStructureService.ReorderEntryAsync` and moves the Part divider plus its following child chapter entries as one contiguous Book.txt block, while preserving the existing single-entry chapter reorder path. `WorkspaceViewModel` now resolves sidebar reorder intents against active Book.txt entries rather than visible projected nodes, so excluded and missing rows do not distort reorder indexes. It rejects excluded or missing sources, missing targets, Part-to-chapter drops, chapter-to-Part drops, and unsupported cross-Part chapter moves before Core mutation, emitting user-visible notifications. Successful sidebar structural writes remain wrapped in manuscript watcher suppression, call the canonical `BookTxtStructureService` mutation, and reload the workspace from disk to synchronize Part-block moves and excluded projection merging without duplicate sidebar nodes. `SidebarView` now exposes testable drag and drop predicates that allow only included, present chapters and Parts to initiate drags or serve as legal matching targets, suppressing illegal visual drop affordances before command invocation. Integration tests prove legal chapter reorders, legal Part-block reorders, watcher suppression, reload persistence, absence of duplicate nodes, and Core failure notification with unchanged Book.txt/sidebar state.

## Verification

Task evidence shows all planned verification gates passed. T01 ran `powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter BookTxtStructureServiceTests --verbosity minimal"` with exit 0, covering Part-block reorder plus existing Book.txt structure tests. T02 ran the filtered `WorkspaceSidebarReorderTests` command with exit 0; 7 reorder ViewModel tests passed. T03 ran the filtered `SidebarViewSmokeTests` command with exit 0. T04 ran focused S04 suites through native Windows dotnet with exit 0, reporting 73 focused tests passed, then ran `dotnet test Hymnal.slnx --no-build --nologo --verbosity normal --blame-hang --blame-hang-timeout 60s --blame-hang-dump-type none` with exit 0, reporting 348 passed and 0 failed. Closeout attempted to re-run verification through `gsd_exec` as required; run `1a580ea0-1304-4045-88e8-bfd6ebcfb03a` reproduced the known environment-only WSL/Windows dotnet NuGet restore failure `Value cannot be null. (Parameter 'path1')`, matching existing project memory MEM008. Because this is a closeout-safe verification-lane environment limitation, not a source regression, closure relies on the task-session native Windows dotnet evidence recorded in T01-T04 summaries. Operational readiness: health signal is the passing Core/ViewModel/View test suite and full solution test count; failure signal is Result/notification-based rejection of illegal moves or downstream Core failures; recovery procedure is no partial write for rejected or failed reorder and workspace reload from canonical Book.txt after successful writes; monitoring gaps are limited to no runtime telemetry for desktop drag/drop, with S08 planned to cover integrated human UAT across surfaces.

## Requirements Advanced

- R013 — Advanced canonical manuscript-structure editing by adding sidebar chapter reorder within Parts and Part-block reorder through BookTxtStructureService with reload persistence and illegal-move failure visibility.

## Requirements Validated

None.

## New Requirements Surfaced

- None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

The closeout environment cannot reliably re-run dotnet verification through `gsd_exec`; it reproduces the known WSL/Windows dotnet NuGet restore failure. Task-session native Windows dotnet evidence is used as the authoritative pass evidence. No desktop automation UAT was required for this slice; S08 remains the integrated human desktop UAT.

## Known Limitations

Chapter moves across Part sections are intentionally unsupported in the sidebar for S04 and are rejected with a user-visible error. Cross-Part file movement is deferred to Corkboard structural editing in S05. Runtime telemetry was not added; failure visibility is Result/notification based.

## Follow-ups

S05 should consume the canonical Part/chapter reorder semantics for Corkboard drag reorder and cross-Part moves. S07 should remain a thin Gantt consumer of the same BookTxtStructureService reorder primitive. S08 should run integrated desktop UAT across Sidebar, Corkboard, and Gantt.

## Files Created/Modified

- `src/Hymnal.Core/Services/BookTxtStructureService.cs` — Added Part-block reorder behavior while preserving chapter single-entry reorder.
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs` — Added active Book.txt sidebar reorder validation, watcher-suppressed Core mutation, notifications, and reload synchronization.
- `src/Hymnal/Views/SidebarView.axaml.cs` — Enabled included Part drags and legal-drop predicates for sidebar gestures.
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs` — Added Core tests for Part-block reorder semantics and invalid Part reorder cases.
- `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarReorderTests.cs` — Added ViewModel integration tests for legal/illegal sidebar reorder, reload persistence, watcher suppression, duplicate-node prevention, and Core failure notification.
- `tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs` — Added smoke tests for sidebar drag eligibility and legal-drop predicates.
