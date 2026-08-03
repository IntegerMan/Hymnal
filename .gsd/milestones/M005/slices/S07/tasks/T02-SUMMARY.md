---
id: T02
parent: S07
milestone: M005
key_files:
  - src/Hymnal/Views/GanttCanvas.cs
  - tests/Hymnal.Core.Tests/Views/GanttCanvasTests.cs
key_decisions:
  - (none)
duration: 
verification_result: passed
completed_at: 2026-06-19T00:24:04.112Z
blocker_discovered: false
---

# T02: Added GanttCanvas row-reorder drag intent events for legal editable chapter-row drags within the same Part scope.

**Added GanttCanvas row-reorder drag intent events for legal editable chapter-row drags within the same Part scope.**

## What Happened

Extended GanttCanvas with GanttRowReorderRequestedEventArgs and a RowReorderRequested event. Pointer press now preserves existing editable-cell behavior, then tracks drag candidates only for editable chapter rows; pointer release translates the source and drop positions into a deterministic reorder intent. The shared hit-test path reuses the existing HeaderHeight and RowHeight constants, rejects header/out-of-row hits, rejects book and Part rollup rows, rejects cross-Part drops by nearest preceding Part scope, and suppresses no-op same/adjacent drops. No Book.txt, workspace, filesystem, or service references were added to the canvas. Added focused GanttCanvasTests for legal source/target detection, target midpoint before/after placement, ignored header/outside positions, ignored book/Part rows, cross-Part rejection, no-op drops, and event raising.

## Verification

Ran the task-specified focused test command for GanttCanvasTests. The final exact command exited 0; the harness captured empty stdout/stderr for the dotnet test run, but gsd_exec metadata recorded a successful exit.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter GanttCanvasTests --verbosity minimal"` | 0 | ✅ pass | 3424ms |

## Deviations

Implemented same-Part scope rejection in the canvas by nearest preceding Part row because the slice goal constrains row reorder to chapters within the same Part and the row list already carries Part rollup boundaries.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal/Views/GanttCanvas.cs`
- `tests/Hymnal.Core.Tests/Views/GanttCanvasTests.cs`
