---
id: T04
parent: S01
milestone: M003
provides:
  - GanttViewModel phase-change refresh so in-session edits rebuild the timeline rows
key_files:
  - src/Hymnal/ViewModels/GanttViewModel.cs
  - src/Hymnal/ViewModels/ChapterViewModel.cs
  - tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs
key_decisions:
  - Subscribe to ChapterViewModel.PropertyChanged for PhaseData and rebuild rows when it changes so ApplyPhaseData flows refresh the Gantt view immediately.
patterns_established:
  - Projection view-models should subscribe to the data they display, not rely solely on workspace reloads.
observability_surfaces:
  - none
duration: 45m
verification_result: passed
completed_at: 2026-06-01
blocker_discovered: false
---

# T04: Refresh Gantt projection when phase data changes

**Gantt rows now refresh when chapter phase metadata changes in-session.**

## What Happened

Updated `GanttViewModel` so it no longer only reacts to workspace node list changes. It now listens for `ChapterViewModel.PropertyChanged` on the `PhaseData` property and rebuilds the projected row list whenever a chapter's phase metadata changes.

The test harness now includes a phase-refresh test that constructs a chapter view-model, creates the Gantt projection, applies new phase data, and waits for the projected row to update. The same test file also bootstraps ReactiveUI before constructing the chapter view-models used in the test.

## Verification

- `dotnet test Hymnal.sln --filter GanttViewModelTests -nologo` — passed.
- `dotnet test Hymnal.sln -nologo` — passed, 82 total tests / 0 failures.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test Hymnal.sln --filter GanttViewModelTests -nologo` | 0 | ✅ pass | 533ms |
| 2 | `dotnet test Hymnal.sln -nologo` | 0 | ✅ pass — 82/82 passed | 576ms |

## Diagnostics

Review `src/Hymnal/ViewModels/GanttViewModel.cs` to see the `PropertyChanged` subscription that triggers a row rebuild on `PhaseData` updates.

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal/ViewModels/GanttViewModel.cs` — phase-change subscriptions and row rebuild logic.
- `tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs` — live-refresh coverage.
