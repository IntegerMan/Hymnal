---
id: S01
parent: M004
milestone: M004
provides:
  - Plan-mode corkboard shell surface
  - Live card projection over manuscript state
  - Atomic structural edit entry points for Book.txt
  - UI wiring and smoke coverage for downstream Plan-aware slices
requires:
  []
affects:
  []
key_files:
  - src/Hymnal/ViewModels/CardViewModel.cs
  - src/Hymnal/ViewModels/CorkboardItemViewModel.cs
  - src/Hymnal/ViewModels/CorkboardViewModel.cs
  - src/Hymnal/ViewModels/WorkspaceViewModel.cs
  - src/Hymnal/ViewModels/MainWindowViewModel.cs
  - src/Hymnal/Views/CorkboardView.axaml
  - src/Hymnal/Views/CorkboardView.axaml.cs
  - src/Hymnal/Views/MainWindow.axaml
  - src/Hymnal/Views/MainWindow.axaml.cs
  - src/Hymnal/App.axaml.cs
  - tests/Hymnal.Core.Tests/ViewModels/CorkboardProjectionTests.cs
  - tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs
  - tests/Hymnal.Core.Tests/ViewModels/MainWindowPlanModeTests.cs
  - tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs
  - tests/Hymnal.Core.Tests/Views/ShellModeConvertersTests.cs
  - tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs
key_decisions:
  - Project corkboard cards directly from ChapterViewModel and represent structural rows as a mixed item hierarchy instead of creating a second manuscript model.
  - Route structural edits through IBookTxtStructureService and refresh through a workspace reload seam instead of mutating editor/file state directly from the corkboard.
  - Make Plan a first-class shell mode with its own visibility flag and dedicated view rather than reusing editor or gantt surfaces.
patterns_established:
  - Live corkboard projections should stay reactive to chapter metadata updates without duplicating persistence state.
  - Atomic Book.txt edits should be followed by a workspace refresh seam so view-model state stays synchronized.
  - Destructive structural actions should use explicit confirmation while non-destructive actions stay immediate.
observability_surfaces:
  - none
drill_down_paths:
  []
duration: ""
verification_result: passed
completed_at: 2026-06-04T04:12:25.944Z
blocker_discovered: false
---

# S01: Plan Mode Corkboard Cards

**Delivered Plan mode as a live corkboard that projects Book.txt order into compact part dividers and chapter cards, supports open-to-Write, and routes structural edits through atomic Book.txt operations.**

## What Happened

S01 is now complete. The corkboard projects directly from the existing ChapterViewModel / WorkspaceViewModel manuscript data instead of introducing a second manuscript model, and mixed board rows are represented as a discriminated item hierarchy so part dividers, empty-part hints, and chapter cards can coexist cleanly. Card projections now surface title, status, word count, target/progress, phase dates, selection state, and live refresh behavior. The board can open a selected chapter back into Write mode through the existing shell navigation path, and the shell now exposes Plan as a first-class center-panel mode with Manage-style hidden chrome.

Structural author actions are wired through IBookTxtStructureService and refresh the workspace through a reload seam rather than manipulating editor/file state directly from the board. The corkboard view adds compact layout, selection styling, keyboard open behavior, drag-reorder, and context actions for rename/new/include/remove/delete with confirmation on destructive paths. Verification evidence from the task summaries shows all five slice tasks passed their focused checks: BookTxtStructureServiceTests, CorkboardProjectionTests, CorkboardViewModelTests, MainWindowPlanModeTests, and CorkboardViewSmokeTests. The slice therefore satisfies the Plan-mode corkboard demo and the required structural editing contract for the current milestone.

## Verification

Verification evidence is present across all five completed task summaries: T01 BookTxtStructureServiceTests passed (15/15); T02 CorkboardProjectionTests passed (4/4) and validated live card projection / mixed item types; T03 CorkboardViewModelTests passed and validated selection, open requests, structural edits, and workspace reload behavior; T04 MainWindowPlanModeTests passed (3 tests) and confirmed Plan-mode shell wiring and open-to-Write routing; T05 CorkboardViewSmokeTests passed and confirmed the UI surface constructs and binds correctly. Current milestone status was also checked and shows S01 has 5/5 tasks done.

## Requirements Advanced

None.

## Requirements Validated

- R006 — The corkboard now renders chapter title, status, word count, phase dates, and click-to-open behavior, with plan-mode shell wiring and smoke tests validating the user-facing contract.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

Used a dedicated corkboard view with code-behind prompts/confirmations and drag handlers for the in-view structural actions, and added a small WorkspaceViewModel reload seam so structural edits can refresh cleanly after atomic Book.txt writes.

## Known Limitations

Drag-and-drop and dialog flows were validated by smoke coverage and summary evidence, but they remain the most brittle part of the UI and benefit from manual desktop confirmation on a real workspace.

## Follow-ups

S02 can now build supplemental docs beside the new Plan surface, and S03 can add the Git toolbar knowing Plan mode and the corkboard shell wiring are stable.

## Files Created/Modified

- `src/Hymnal/ViewModels/CardViewModel.cs` — Added live card projection over ChapterViewModel state.
- `src/Hymnal/ViewModels/CorkboardItemViewModel.cs` — Added mixed item hierarchy for part dividers, empty-part hints, and chapter cards.
- `src/Hymnal/ViewModels/CorkboardViewModel.cs` — Implemented selection, open requests, and structural edit workflow.
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs` — Added reload seam used after corkboard structural edits.
- `src/Hymnal/ViewModels/MainWindowViewModel.cs` — Added Plan-mode visibility and navigation wiring.
- `src/Hymnal/Views/CorkboardView.axaml` — Built the compact Plan-mode corkboard surface.
- `src/Hymnal/Views/CorkboardView.axaml.cs` — Wired interactions, drag handling, and context actions.
- `src/Hymnal/Views/MainWindow.axaml` — Enabled the Plan tab and hosted the corkboard view.
- `src/Hymnal/Views/MainWindow.axaml.cs` — Collapsed shell chrome for Plan and Manage modes.
- `src/Hymnal/App.axaml.cs` — Registered corkboard services and wiring in DI.
- `tests/Hymnal.Core.Tests/ViewModels/CorkboardProjectionTests.cs` — Verified card projection and live refresh behavior.
- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs` — Verified selection, open, reorder, and failure paths.
- `tests/Hymnal.Core.Tests/ViewModels/MainWindowPlanModeTests.cs` — Verified shell navigation and Plan-mode routing.
- `tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs` — Verified the corkboard UI surface constructs and binds.
- `tests/Hymnal.Core.Tests/Views/ShellModeConvertersTests.cs` — Extended Plan-mode converter coverage.
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs` — Validated atomic Book.txt structural operations.
