---
id: S06
parent: M005
milestone: M005
provides:
  - A complete Corkboard include/exclude path using the canonical BookTxtStructureService APIs.
  - Inline chapter insertion with Part-aware ordering and reload persistence.
  - Immediate excluded-card projection after reload without duplicate orphan rows.
  - Fresh slice verification evidence advancing R013 ahead of S08 cross-surface UAT.
requires:
  - slice: S01
    provides: Canonical BookTxtStructureService operations and atomic structure-write boundary for include/exclude/create semantics.
  - slice: S02
    provides: Excluded-node manifest semantics and sidebar/workspace reload behavior reused by Corkboard excluded cards.
  - slice: S05
    provides: Existing Corkboard drag/reload patterns and integration harness reused for structural persistence verification.
affects:
  - S08
key_files:
  - src/Hymnal/ViewModels/CorkboardViewModel.cs
  - src/Hymnal/ViewModels/CorkboardItemViewModel.cs
  - src/Hymnal/Views/CorkboardView.axaml
  - src/Hymnal/Views/CorkboardView.axaml.cs
  - tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs
  - tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelIntegrationTests.cs
  - tests/Hymnal.Core.Tests/ViewModels/CorkboardProjectionTests.cs
  - tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs
key_decisions:
  - Keep Corkboard include/exclude and chapter creation as thin consumers of IBookTxtStructureService rather than adding a Corkboard-only Book.txt write path.
  - Derive inline-create placement and include insertion indexes from canonical all-entry Book.txt ordering, including Part boundaries, instead of chapter-only projection order.
  - Project manifest-excluded workspace chapter nodes directly into Corkboard excluded cards and de-duplicate orphan discovery by path so reloads show excluded cards immediately without duplicate rows.
  - Use native Windows dotnet.exe plus serialized/no-build reruns for verification when executor-side gsd_exec/bash cannot run dotnet or parallel builds lock Avalonia obj outputs.
patterns_established:
  - Corkboard routing helpers should translate board, part, chapter, and excluded-card actions into canonical structure-service calls plus workspace reload, keeping view code-behind deterministic and thin.
  - Excluded Corkboard cards are projection items sourced first from workspace excluded nodes, with orphan discovery only supplementing missing manifest-backed exclusions.
  - Structural verification in this repo should record native Windows dotnet evidence in task summaries and let closeout consume that evidence when gsd_exec cannot run dotnet directly.
observability_surfaces:
  - CorkboardViewModel.LastStructuralError for failed include/exclude/create operations.
  - INotificationService.ShowError for visible UI failure reporting during structural operations.
  - Inspection via Book.txt, .hymnal-data/exclusions.json, chapter files, and focused integration/projection tests.
drill_down_paths:
  - .gsd/milestones/M005/slices/S06/tasks/T01-SUMMARY.md
  - .gsd/milestones/M005/slices/S06/tasks/T02-SUMMARY.md
  - .gsd/milestones/M005/slices/S06/tasks/T03-SUMMARY.md
  - .gsd/milestones/M005/slices/S06/tasks/T04-SUMMARY.md
  - .gsd/milestones/M005/slices/S06/tasks/T05-SUMMARY.md
duration: ""
verification_result: passed
completed_at: 2026-06-18T23:53:09.835Z
blocker_discovered: false
---

# S06: Corkboard inclusion toggle and chapter insertion

**Completed Corkboard include/exclude and inline chapter insertion through the canonical structure service with reload-persistent excluded-card projection and Book.txt ordering.**

## What Happened

S06 finished the remaining Corkboard structural capabilities planned after S05. T01 hardened the include/exclude command contracts so CorkboardViewModel stays a thin consumer of IBookTxtStructureService and clears stale selection state after successful exclusion. T02 completed inline chapter insertion semantics by deriving placement from canonical Book.txt node order, covering inserts between chapter cards, adjacent to Part dividers, at index 0, and within nested Part folder prefixes while surfacing duplicate-path or invalid-create failures through LastStructuralError instead of pre-writing files. T03 wired the Corkboard view and code-behind so included, excluded, part-level, and board-level menu actions route through deterministic helpers that use canonical all-entry insertion indexes and preserve excluded-card affordances and non-draggable behavior. T04 closed the remaining reload gap by projecting manifest-excluded workspace nodes directly into Corkboard excluded cards and de-duplicating orphan discovery, so excluded cards appear immediately after reload without duplicate rows while the existing real-workspace integration tests continue to prove include/exclude and inline-create persistence. T05 then re-verified the slice with focused Corkboard suites, full-solution tests, and an app build, documenting the Windows verification-environment limitation where gsd_exec/bash cannot reliably invoke dotnet and concurrent native test runs can lock Avalonia obj outputs. Together these changes keep Corkboard inclusion toggle and new chapter insertion on the same canonical BookTxtStructureService path as other structure edits, preserve reload/restart persistence, and advance R013 while leaving the planned cross-surface desktop replay for S08.

## Verification

Closeout verification used Context Mode evidence plus a coverage check because executor-side gsd_exec/bash cannot reliably run dotnet in this Windows repo. A fresh gsd_exec node audit (`C:/Dev/Hymnal/.gsd/exec/386122db-797a-45a6-a523-c4e31adc47e8.stdout`) confirmed every slice-plan verification command is covered by passing task-summary evidence: `dotnet test ... --filter CorkboardViewModelTests` in T01/T02/T05, `dotnet test ... --filter CorkboardViewSmokeTests` in T03/T05, `dotnet test ... --filter CorkboardViewModelIntegrationTests` in T05, `dotnet test Hymnal.slnx --nologo --verbosity minimal` in T05, and `dotnet build src/Hymnal/Hymnal.csproj --nologo` in T05. The fresh task evidence records 39 passing CorkboardViewModelTests, 7 passing CorkboardViewSmokeTests, 10 passing CorkboardViewModelIntegrationTests, 386/386 passing full-solution tests, and a successful app build. T04 also added focused CorkboardProjectionTests to prove immediate excluded-card projection and orphan de-duplication after reload. No failing product regressions remained; only documented verification-environment quirks required native Windows dotnet.exe and serialized/no-build reruns.

## Requirements Advanced

- R013 — Completed the remaining Corkboard inclusion-toggle and inline chapter insertion capabilities through the canonical structure service with reload-persistent excluded-card projection and fresh focused/full-suite verification evidence.

## Requirements Validated

None.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

Verification closure relied on fresh task-summary evidence and a gsd_exec coverage audit because gsd_exec/bash could not reliably invoke dotnet in this Windows environment; focused task runs used native Windows dotnet.exe and serialized/no-build reruns to avoid Avalonia obj file-lock contention.

## Known Limitations

S08 still remains for end-to-end desktop cross-surface replay and final user-facing structure-edit polish. Verification environment still has the known gsd_exec/bash dotnet limitation and intermittent concurrent-test file locking in Avalonia obj outputs.

## Follow-ups

Execute S08 cross-surface UAT to replay sidebar, Corkboard, and future Gantt structure edits together after S07 lands.

## Files Created/Modified

- `src/Hymnal/ViewModels/CorkboardViewModel.cs` — Hardened include/exclude command contracts and inline chapter insertion ordering against canonical Book.txt node order.
- `src/Hymnal/ViewModels/CorkboardItemViewModel.cs` — Projected manifest-excluded workspace nodes directly and de-duplicated orphan discovery for excluded cards after reload.
- `src/Hymnal/Views/CorkboardView.axaml` — Added excluded-card styling and ensured include/exclude/new-chapter affordances are exposed in the Corkboard UI.
- `src/Hymnal/Views/CorkboardView.axaml.cs` — Added deterministic routing helpers for board, part, chapter, and excluded-card include/create actions.
- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs` — Expanded focused view-model coverage for include/exclude behavior, inline create placement, duplicate handling, and error surfacing.
- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelIntegrationTests.cs` — Served as the real-workspace persistence proof for include/exclude and inline-create flows during slice verification.
- `tests/Hymnal.Core.Tests/ViewModels/CorkboardProjectionTests.cs` — Added focused coverage for immediate excluded-card projection and orphan de-duplication after reload.
- `tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs` — Verified Corkboard XAML affordances, excluded styling text, drag-source guards, and menu-routing helper outputs.
