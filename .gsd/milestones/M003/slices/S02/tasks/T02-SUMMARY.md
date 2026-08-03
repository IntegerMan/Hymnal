---
id: T02
parent: S02
milestone: M003
key_files:
  - src/Hymnal/Views/GanttCanvas.cs
key_decisions:
  - DrawPartRow updated signature to accept axisStart/axisEnd for date-to-pixel projection, consistent with how chapter rows are positioned
  - Progress fill is rendered using ctx.PushClip(trackRect) to guarantee the fill never overflows the rounded-corner span box
  - Percentage label is suppressed when boxW ≤ 36px to avoid text overflow in narrow spans
  - Part rows without valid aggregated dates fall back to the prior divider-only appearance (no rollup box drawn)
duration: 
verification_result: passed
completed_at: 2026-06-01T19:03:28.590Z
blocker_discovered: false
---

# T02: GanttCanvas.DrawPartRow now renders a date-span rollup box with a clipped green progress fill and percentage label for Part rows

**GanttCanvas.DrawPartRow now renders a date-span rollup box with a clipped green progress fill and percentage label for Part rows**

## What Happened

Updated `GanttCanvas.cs` to fully render Part rollup rows in the Gantt chart. Three changes were made:

1. **New brush/pen constants** — added `PartRollupTrackBrush` (muted semi-transparent slate, rgba 60% opacity), `PartRollupFillBrush` (`#34D399`, the Done green matching the phase palette), and `PartRollupStrokePen` (green outline at 63% opacity) as static fields alongside the existing brush declarations.

2. **DrawRow call-site** — the call to `DrawPartRow` was updated to pass `axisStart` and `axisEnd` so the rollup box can be positioned correctly on the time axis (matching how chapter rows compute their box positions via `DateToX`).

3. **DrawPartRow redesign** — the method signature gained `DateOnly axisStart, DateOnly axisEnd` parameters. The body now:
   - Retains the muted full-width background band and the horizontal separator at the top.
   - Retains the uppercase title label in the left label column.
   - Conditionally draws a rollup span box when `!row.IsMissingDates && row.StartDate.HasValue && row.EndDate.HasValue`: a rounded-rectangle track spanning the child date range, a `PushClip`-bounded progress fill scaled by `row.CompletionPercentage`, and a centered percentage text label (rendered only when the box is wider than 36px to avoid overflow).

The `CompletionPercentage` value (Done-fraction of child chapters) was computed in T01. When a Part has no valid child dates the fallback renders identically to the previous divider-only style so no regression occurs.

## Verification

Build: `dotnet build src/Hymnal/Hymnal.csproj -nologo` — succeeded, 0 warnings, 0 errors.
Tests: `dotnet test Hymnal.sln --no-build -nologo` — 86 passed, 0 failed, 0 skipped.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build src/Hymnal/Hymnal.csproj -nologo` | 0 | ✅ pass | 13600ms |
| 2 | `dotnet test Hymnal.sln --no-build -nologo` | 0 | ✅ pass — 86 passed, 0 failed | 611ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal/Views/GanttCanvas.cs`
