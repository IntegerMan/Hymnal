---
id: S03
parent: M005
milestone: M005
provides:
  - A verified canonical rename path for sidebar chapter and Part operations with UUID continuity guarantees.
  - A user-facing sidebar rename affordance that downstream reorder/Corkboard/Gantt slices can follow without introducing new structural write paths.
  - Prepared manual rename UAT scenarios for the integrated S08 cross-surface verification pass.
requires:
  - slice: S01
    provides: Canonical structure operations, rollback patterns, exclusion cleanup, and registry continuity foundations used by rename flows.
  - slice: S02
    provides: Sidebar projection rules and included-versus-excluded node semantics needed to show rename only on eligible rows.
affects:
  - S04
  - S05
  - S06
  - S07
  - S08
key_files:
  - src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs
  - src/Hymnal.Core/Services/BookTxtStructureService.cs
  - src/Hymnal/ViewModels/WorkspaceViewModel.cs
  - src/Hymnal/Views/SidebarView.axaml
  - src/Hymnal/Views/SidebarView.axaml.cs
  - tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs
  - tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarRenameTests.cs
  - tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs
key_decisions:
  - Kept `RenameEntryAsync(bookTxtPath, existingPath, replacementPath)` as the single canonical rename mutation surface rather than adding sidebar-specific file I/O paths.
  - Treated the preloaded `R010` ownership mapping as out of scope for S03 and advanced `R013` instead, matching the approved rename-only sketch.
  - Used `Hymnal.slnx` plus an explicit Windows `dotnet.exe` path through PowerShell for verification because the WSL-backed `gsd_exec` environment does not reliably expose `dotnet`.
patterns_established:
  - Sidebar code-behind may gather rename input, but all structural mutation, validation, rollback, and reload orchestration stay in WorkspaceViewModel and Hymnal.Core services.
  - Book.txt path changes that can affect UUID-backed metadata must route through the rollback-aware canonical structure service and reload the workspace afterward.
  - Visibility rules for structural sidebar affordances should be centralized in testable predicates so excluded or missing nodes never expose invalid actions.
observability_surfaces:
  - Result.Fail messages from `BookTxtStructureService` naming rename phase, source path, target path, and rollback status.
  - WorkspaceViewModel notification errors that surface invalid rename input and Core structural failures to the user.
  - Automated test coverage across Core, ViewModel, and view smoke layers for rename success and failure behavior.
drill_down_paths:
  - .gsd/milestones/M005/slices/S03/tasks/T01-SUMMARY.md
  - .gsd/milestones/M005/slices/S03/tasks/T02-SUMMARY.md
  - .gsd/milestones/M005/slices/S03/tasks/T03-SUMMARY.md
  - .gsd/milestones/M005/slices/S03/tasks/T04-SUMMARY.md
duration: ""
verification_result: passed
completed_at: 2026-06-18T12:19:56.029Z
blocker_discovered: false
---

# S03: Sidebar rename with UUID continuity

**Closed the sidebar rename slice by verifying the existing Core, ViewModel, and sidebar wiring preserves UUID-backed metadata across chapter and Part renames while keeping all structural mutation inside BookTxtStructureService.**

## What Happened

S03 closed as a verification-heavy slice because the planned implementation was already landed across all four tasks. T01 confirmed `IBookTxtStructureService.RenameEntryAsync` remains the sole canonical rename surface and that `BookTxtStructureService` already performs rollback-aware chapter and Part renames, Book.txt rewrites, heading updates, registry reconciliation, and UUID continuity protection. T02 verified `WorkspaceViewModel` exposes sidebar rename commands that reject invalid or excluded targets, build deterministic replacement paths, suppress watchers during structural edits, delegate only to the structure service, reload the workspace, and reselect the renamed node so notes, phases, targets, and history remain attached to the same UUID. T03 verified the sidebar view exposes a `Rename…` affordance only for included present chapter and Part rows, prompts for the new title in code-behind, and keeps all structural logic in the existing ViewModel/Core path. T04 audited the full rename call chain and verification boundary, documented that the real solution file is `Hymnal.slnx`, and confirmed rename mutations stay inside `BookTxtStructureService` rather than the view or view model. During closeout I refreshed slice verification with `gsd_exec` using an explicit Windows `dotnet.exe` path through PowerShell because the WSL-backed sandbox does not reliably expose `dotnet`. The fresh closeout run completed successfully against `Hymnal.slnx`, matching the task-level evidence that all rename-focused suites and the full solution tests are green.

## Verification

Fresh closeout verification passed with `gsd_exec` run `0eaa82f5-069a-4c8d-9559-f5ed2ab48b60` using `powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test Hymnal.slnx --nologo --verbosity minimal"` from the repo root; the run exited 0. This refreshed the slice-level proof required by S03 after task completion. Task-level evidence remains consistent with that closeout result: T01 passed the focused `BookTxtStructureServiceTests` suite covering chapter rename, Part folder rename, rollback, and UUID continuity; T02 passed `WorkspaceSidebarRenameTests` covering command integration, notification failures, reload, reselection, and metadata continuity; T03 passed `SidebarViewSmokeTests` covering the `Rename…` affordance and visibility rules; and T04 recorded a full serial verification pass including `dotnet build src/Hymnal/Hymnal.csproj --nologo --verbosity minimal` and `dotnet test Hymnal.slnx --nologo --verbosity minimal --no-build --no-restore` with 324/324 tests green. Observability and failure visibility remain the planned Result/notification surfaces: conflict and invalid-path failures are reported with phase-aware error details rather than silent partial state.

## Requirements Advanced

- R013 — Advanced and validated the sidebar rename portion of the manuscript structural-editing requirement by proving chapter and Part renames flow through the canonical BookTxtStructureService path and preserve UUID-backed continuity across reload.

## Requirements Validated

- R013 — Updated requirement R013 to validated after fresh slice closeout verification and existing task evidence confirmed canonical rename behavior, rollback-aware conflict handling, workspace reload continuity, and UUID-backed metadata preservation.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

Used the real solution file `Hymnal.slnx` for closeout verification because the repository does not contain `Hymnal.sln`. Also used `gsd_exec` through PowerShell with an explicit Windows `dotnet.exe` path because the WSL-backed sandbox does not reliably expose `dotnet`.

## Known Limitations

Human desktop execution of the rename scenarios remains deferred to S08; S03 closes on automated proof plus prepared manual UAT rather than a live interactive desktop pass. An existing non-blocking build warning (`CS4014` in `src/Hymnal/ViewModels/WorkspaceViewModel.cs`) was reported in task-level verification and remains outside this slice’s rename-specific scope.

## Follow-ups

S04-S07 should continue consuming the same canonical `BookTxtStructureService` path for reorder and other structural edits. S08 should execute the prepared desktop rename scenarios together with cross-surface reorder and reload UAT to confirm integrated author experience and failure messaging.

## Files Created/Modified

- `src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs` — Holds the canonical rename contract verified for sidebar chapter and Part operations.
- `src/Hymnal.Core/Services/BookTxtStructureService.cs` — Owns rollback-aware rename mutation, Book.txt rewrites, heading updates, registry reconciliation, and failure reporting verified by this slice.
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs` — Exposes watcher-suppressed sidebar rename commands and reload/reselection behavior verified in integration tests.
- `src/Hymnal/Views/SidebarView.axaml` — Declares the `Rename…` sidebar affordance for eligible rows.
- `src/Hymnal/Views/SidebarView.axaml.cs` — Prompts for rename input and routes commands without performing structural file mutation.
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs` — Covers chapter rename, Part folder rename, conflicts, rollback, and UUID continuity.
- `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarRenameTests.cs` — Covers rename command integration, reload continuity, failure notifications, and metadata preservation.
- `tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs` — Covers rename menu declaration and visibility rules for eligible versus excluded/missing sidebar rows.
