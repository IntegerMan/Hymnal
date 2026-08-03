---
id: S03
parent: M002
milestone: M002
provides:
  - (none)
requires:
  []
affects:
  []
key_files: []
key_decisions:
  - Use the AvaloniaEdit IBackgroundRenderer fallback for advisory gutter markers because AbstractMargin is absent in AvaloniaEdit 12.0.0.
  - Keep row-height control in MainWindow.axaml.cs subscriptions because Avalonia RowDefinition did not reliably inherit DataContext in the shared right rail.
  - Preserve existing ViewModel disposal style by using CompositeDisposable.Add rather than DisposeWith.
  - Route target writes through ChapterViewModel.SetTargetCommand to preserve lock ownership in TargetsService.
patterns_established:
  - F3 Chapter Info pane now mirrors the Notes pane lifecycle pattern for chapter switching and visibility.
  - Advisory validation in the editor is non-blocking and silent by design.
  - Right-rail layout is coordinated from shared visibility streams instead of binding RowDefinition heights directly in AXAML.
observability_surfaces:
  - none
drill_down_paths:
  - .gsd/milestones/M002/slices/S03/tasks/T01-SUMMARY.md
  - .gsd/milestones/M002/slices/S03/tasks/T02-SUMMARY.md
  - .gsd/milestones/M002/slices/S03/tasks/T03-SUMMARY.md
duration: ""
verification_result: passed
completed_at: 2026-05-31T03:43:44.498Z
blocker_discovered: false
---

# S03: Chapter Info Pane and Validation Margin

**Completed the F3 Chapter Info pane, refactored the right rail to host Chapter Info and Notes together, and added a silent advisory validation gutter for Markua markers.**

## What Happened

Implemented the full S03 presentation layer for M002. The ViewModel backbone now includes ChapterInfoViewModel with chapter-switch lifecycle handling, status/date/target persistence, and phase-date prefill support, plus MainWindowViewModel coordination for independently toggleable right-rail panes. The UI layer now exposes the Chapter Info pane on F3, renders a shared right-rail host with a collapsible Chapter Info / Notes stack and splitter behavior, and wires ChapterInfoView bindings and code-behind to the VM contract. ValidationMargin was added in the editor using the AvaloniaEdit fallback extension point available in this codebase, scanning for the two advisory Markua patterns and rendering non-blocking gutter dots while swallowing all exceptions. Task-level summaries captured the notable implementation decisions, including the AvaloniaEdit AbstractMargin fallback and the row-height subscription workaround for Avalonia right-rail layout.

## Verification

Verified in the Windows-backed shell session: `dotnet build src/Hymnal/Hymnal.csproj -nologo` succeeded with 0 warnings and 0 errors, and `dotnet test Hymnal.sln -nologo` passed with 57 tests, 0 failed, 0 skipped. Build and test output confirmed the slice integrates cleanly with the existing solution and did not regress the core test suite.

## Requirements Advanced

- R003 — Surfaced and persisted chapter status/date editing through the new Chapter Info pane, including optional phase-date prefill behavior.
- R004 — Surfaced live word count and editable target controls in the Chapter Info pane, completing the user-facing count surface.

## Requirements Validated

None.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

ValidationMargin uses IBackgroundRenderer + BackgroundRenderers.Add() instead of AbstractMargin because AvaloniaEdit 12.0.0 in this codebase does not expose AbstractMargin. RowDefinition height behavior is driven from MainWindow.axaml.cs subscriptions because Avalonia RowDefinition did not reliably inherit DataContext in the right-rail refactor.

## Known Limitations

ChapterInfoViewModel.ProximityFill remains a stubbed 0.0 value; the pane is wired and functional, but the fill display is not yet using live chapter proximity data.

## Follow-ups

Consider wiring ChapterInfoViewModel/ChapterInfoView to the live ChapterViewModel.ProximityFill value in a later slice if the proximity bar becomes a user-visible acceptance target.

## Files Created/Modified

- `src/Hymnal/ViewModels/ChapterInfoViewModel.cs` — Added the F3 pane ViewModel with lifecycle handling, status/date/target persistence, and prefill support.
- `src/Hymnal/ViewModels/ChapterViewModel.cs` — Added ApplyPhaseData mutator for syncing phase state back into the chapter VM.
- `src/Hymnal/ViewModels/MainWindowViewModel.cs` — Added ChapterInfoViewModel plus right-rail visibility coordination properties.
- `src/Hymnal/App.axaml.cs` — Registered ChapterInfoViewModel in DI and wired it into MainWindowViewModel construction.
- `src/Hymnal/Views/ChapterInfoView.axaml` — Created the Chapter Info pane UI with status/date/word-count/target controls.
- `src/Hymnal/Views/ChapterInfoView.axaml.cs` — Added minimal code-behind for status selection change handling.
- `src/Hymnal/Views/MainWindow.axaml` — Refactored the right rail into a shared host for Chapter Info and Notes with F3/F4 bindings and splitter behavior.
- `src/Hymnal/Views/MainWindow.axaml.cs` — Driven right-rail row sizing from subscriptions to support the shared host layout.
- `src/Hymnal/Views/Converters/NodeKindConverters.cs` — Added the BoolToGridLength converter used by the right-rail visibility logic.
- `src/Hymnal/Views/Editor/ValidationMargin.cs` — Created the advisory gutter renderer for the two Markua validation patterns.
- `src/Hymnal/Views/EditorView.axaml.cs` — Registered and refreshed the validation margin from the editor lifecycle.
