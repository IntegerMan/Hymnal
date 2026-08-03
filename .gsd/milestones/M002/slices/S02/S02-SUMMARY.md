---
id: S02
parent: M002
milestone: M002
provides:
  - Live chapter counts and saved-count history for downstream features
  - Target storage and display state for the Chapter Info pane in S03
requires:
  []
affects:
  []
key_files:
  - src/Hymnal/ViewModels/EditorViewModel.cs
  - src/Hymnal/ViewModels/ChapterViewModel.cs
  - src/Hymnal/ViewModels/WorkspaceViewModel.cs
  - src/Hymnal/App.axaml.cs
  - src/Hymnal/Views/SidebarView.axaml
  - src/Hymnal/Views/Converters/NodeKindConverters.cs
  - src/Hymnal/Views/Converters/StatusToBrushConverter.cs
key_decisions:
  - AvaloniaScheduler.Instance used for debounced LiveWordCount ObserveOn instead of RxApp.MainThreadScheduler
  - Popup used for target entry because Avalonia FlyoutBase.IsOpen is not suitable for TwoWay binding
patterns_established:
  - Background per-chapter word-count recalculation runs on Task.Run and marshals updates to the UI thread with generation guards
  - Nullable integer text fields in the sidebar popup use a dedicated converter for clean two-way binding
observability_surfaces:
  - none
drill_down_paths:
  []
duration: ""
verification_result: passed
completed_at: 2026-05-31T03:09:27.808Z
blocker_discovered: false
---

# S02: Word Count Targets and Rollup

**Implemented debounced live word counts, saved-count persistence, reactive chapter/part/book rollups, and per-chapter targets with sidebar proximity indicators.**

## What Happened

S02 completed the word-count and target pipeline across Core, ViewModels, and the sidebar UI. In the ViewModel layer, EditorViewModel now computes a 300ms-debounced LiveWordCount from Text, exposes a Saved observable that fires after successful save, and uses the project’s Avalonia scheduler conventions. ChapterViewModel now carries persisted and live word-count state, target data, display-friendly word-count and part-total properties, proximity fill calculation, and commands for set/confirm/clear target actions with error handling. WorkspaceViewModel was extended to load targets at workspace open, subscribe to EditorViewModel.Saved so each save updates the active chapter count and appends history, recalculate uncached chapter counts in background Task.Run work guarded by generation checks, and maintain reactive total word count rollups. App.axaml.cs was updated to register the new Core services and wire them into the existing DI factories.

On the UI side, SidebarView.axaml was updated to show a book total in the CHAPTERS header, right-aligned chapter and part word counts, a proximity fill bar when targets exist, and a right-click Set Target popup with min/max inputs plus Set, Clear, and Cancel actions. Supporting converters were added for part-node detection, nullable integer text binding, and opacity handling where resources were missing. During implementation, a few Avalonia-specific constraints were resolved: Flyout was replaced with Popup because IsOpen TwoWay binding is not viable on this version, CharacterSpacing and StackPanel.Padding were removed because they are not supported as used, and scheduler usage was aligned to AvaloniaScheduler.Instance.

The slice remained within the planned scope: Core logic stayed Avalonia-free, metadata writes use atomic persistence paths, and word-count history is appended on save without blocking the UI. The end result is that word counts are now visible and reactive in the sidebar, totals roll up across the workspace, and targets can be created and cleared from the chapter context menu.

## Verification

Verified with the slice’s required command set: `dotnet build src/Hymnal/Hymnal.csproj -nologo` succeeded, `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter WordCount -nologo` passed (10/10), and `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter Targets -nologo` passed (6/6). The task summaries also show a full solution pass with 57/57 tests passing and no regressions. Milestone status check confirmed S02 had all tasks complete before closure.

## Requirements Advanced

- R004 — Implemented live word count, target storage, sidebar rollups, and history persistence for chapter progress signals.

## Requirements Validated

- R004 — Build and targeted WordCount/Targets tests passed, and the slice summaries confirm sidebar rollup, live count debounce, and target persistence wiring are complete.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

Sidebar target entry uses Popup instead of Flyout because FlyoutBase.IsOpen is not TwoWay-bindable in this Avalonia version; this preserves the intended UX without changing behavior.

## Known Limitations

Background recalculation failures remain silent by design for now, leaving uncached chapters displayed as `—` until a later successful save or recalculation.

## Follow-ups

None.

## Files Created/Modified

- `src/Hymnal/ViewModels/EditorViewModel.cs` — Added debounced LiveWordCount and Saved observable wiring.
- `src/Hymnal/ViewModels/ChapterViewModel.cs` — Added word-count state, target state, proximity fill, and target commands.
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs` — Loaded targets, subscribed to Saved, updated totals, and ran background word-count recalculation.
- `src/Hymnal/App.axaml.cs` — Registered and injected WordCountService, TargetsService, and WordCountHistoryService.
- `src/Hymnal/Views/SidebarView.axaml` — Rendered counts, totals, target proximity bar, and target popup UI.
- `src/Hymnal/Views/Converters/NodeKindConverters.cs` — Added part-node detection converter.
- `src/Hymnal/Views/Converters/StatusToBrushConverter.cs` — Added nullable int string converter for target entry fields.
