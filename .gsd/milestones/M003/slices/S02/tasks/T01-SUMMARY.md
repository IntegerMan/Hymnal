---
id: T01
parent: S02
milestone: M003
key_files:
  - src/Hymnal.Core/Models/GanttRowData.cs
  - src/Hymnal/ViewModels/GanttRowViewModel.cs
  - src/Hymnal/ViewModels/GanttViewModel.cs
  - tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs
key_decisions:
  - Part rollup uses simple Done-count fraction (not word-count weighted) per task plan guidance; TODO comment added for future word-count weighting
  - BuildPartRollup is a private static method on GanttViewModel to keep all list-logic in one place rather than extending GanttProjection (which operates on a single node)
  - Status field on Part rollup GanttRowData is set to ChapterStatus.Outlining as a neutral default since it has no per-Part meaning
duration: 
verification_result: passed
completed_at: 2026-06-01T19:01:07.536Z
blocker_discovered: false
---

# T01: GanttViewModel now computes Part rollup rows (min/max child dates + Done-fraction completion) and exposes CompletionPercentage on GanttRowData/GanttRowViewModel

**GanttViewModel now computes Part rollup rows (min/max child dates + Done-fraction completion) and exposes CompletionPercentage on GanttRowData/GanttRowViewModel**

## What Happened

Updated four files to implement Part rollup logic in the Gantt layer:

1. **GanttRowData.cs** — Added `double CompletionPercentage = 0.0` as a defaulted record parameter. Includes a TODO comment about future word-count weighting.

2. **GanttRowViewModel.cs** — Added `CompletionPercentage` property, mapped from the data record in the constructor.

3. **GanttViewModel.cs** — Refactored `RebuildRows()` from a simple `foreach` to an indexed `for` loop. When the current node is a Part, it scans ahead to collect all sibling Chapter nodes (stopping at the next Part or end of list) and delegates to the new `BuildPartRollup()` static method. The rollup computes: min(StartDate) across children, max(EndDate) across children, and fraction of children with `Status == Done` as `CompletionPercentage`. Chapter nodes continue to be projected via the existing `GanttProjection.Project()`. Required adding `using System;`, `using System.Collections.Generic;`, `using System.Linq;`, and `using Hymnal.Core.Models;` imports.

4. **GanttViewModelTests.cs** — Added four new `[Fact]` tests covering:
   - Part row spans min-start to max-end of child chapters
   - CompletionPercentage reflects the fraction of Done children
   - Part with no children yields zero completion and missing dates
   - Part rollup stops at the next Part boundary (two-Part scenario)
   
   Added an `InjectNode()` helper that writes to the `_node` backing field via reflection, replacing the default Chapter node created by `CreateChapterViewModel` with a Part node.

## Verification

Ran `dotnet build Hymnal.sln` (0 warnings, 0 errors) and `dotnet test Hymnal.sln` — all 86 tests passed, 0 failures. The Gantt-specific filter `--filter GanttViewModelTests` ran 27 tests (including 4 new Part rollup tests), all green.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build Hymnal.sln --nologo -v q` | 0 | ✅ pass | 19340ms |
| 2 | `dotnet test Hymnal.sln --nologo --filter "FullyQualifiedName~GanttViewModelTests"` | 0 | ✅ pass — 27 passed | 2000ms |
| 3 | `dotnet test Hymnal.sln --nologo` | 0 | ✅ pass — 86 passed, 0 failed | 534ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal.Core/Models/GanttRowData.cs`
- `src/Hymnal/ViewModels/GanttRowViewModel.cs`
- `src/Hymnal/ViewModels/GanttViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs`
