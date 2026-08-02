---
id: T03
parent: S01
milestone: M003
provides:
  - GanttCanvas missing-date rendering that keeps chapter rows visible
key_files:
  - src/Hymnal/Views/GanttCanvas.cs
  - tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs
key_decisions:
  - When every chapter lacks valid dates, the canvas falls back to a sensible axis and still renders muted rows instead of collapsing to an empty placeholder.
patterns_established:
  - Read-only timeline views should keep incomplete data visible so authors can still orient themselves.
observability_surfaces:
  - none
duration: 40m
verification_result: passed
completed_at: 2026-06-01
blocker_discovered: false
---

# T03: Fix GanttCanvas missing-date rendering

**GanttCanvas now keeps missing-date chapters visible instead of collapsing to an empty placeholder.**

## What Happened

Updated `GanttCanvas.Render` so the view no longer bails out when no valid phase dates exist. Instead, it uses a fallback axis range and still renders each chapter row as a muted placeholder band. The true empty-state path remains only for the case where no rows exist at all.

To pin the behavior down, the Gantt view-model test file now includes a missing-dates coverage test that constructs a workspace with a chapter whose phase data has no valid dates and verifies the projected row remains present.

## Verification

- `dotnet test Hymnal.sln --filter GanttViewModelTests -nologo` — passed.
- `dotnet test Hymnal.sln -nologo` — passed, 82 total tests / 0 failures.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test Hymnal.sln --filter GanttViewModelTests -nologo` | 0 | ✅ pass | 533ms |
| 2 | `dotnet test Hymnal.sln -nologo` | 0 | ✅ pass — 82/82 passed | 576ms |

## Diagnostics

Review `src/Hymnal/Views/GanttCanvas.cs` to confirm the fallback-axis render path and the removal of the no-dates early return.

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal/Views/GanttCanvas.cs` — fallback-axis render path for all-missing-date workspaces.
- `tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs` — missing-date row coverage.
