---
id: T02
parent: S01
milestone: M004
key_files:
  - src/Hymnal/ViewModels/CardViewModel.cs
  - src/Hymnal/ViewModels/CorkboardItemViewModel.cs
  - tests/Hymnal.Core.Tests/ViewModels/CorkboardProjectionTests.cs
key_decisions:
  - Projected corkboard cards directly from ChapterViewModel rather than adding a second manuscript model.
  - Represented part dividers, empty-part hints, and chapter cards as a discriminated item hierarchy.
duration: 
verification_result: passed
completed_at: 2026-06-04T01:56:31.925Z
blocker_discovered: false
---

# T02: Projected live corkboard card items from ChapterViewModel state with selectable chapter cards, part dividers, empty-part hints, and refreshable display fields.

**Projected live corkboard card items from ChapterViewModel state with selectable chapter cards, part dividers, empty-part hints, and refreshable display fields.**

## What Happened

Added CardViewModel as a live projection over ChapterViewModel and exposed title, relative path, status display/brush key, word count display, target display, proximity fill, phase date displays, missing-state text, and selected state. Added CorkboardItemViewModel plus concrete part-divider, empty-part-hint, and chapter-card item variants so the board can mix structural rows with selectable chapter cards without introducing a second manuscript model. Wrote projection tests that verify mixed-row output, safe fallbacks for missing/absent target and invalid dates, non-selectable part rows, empty-part hint emission, and live refresh behavior for node, word count, phase data, and target updates. During testing, the target-refresh assertion was isolated from the dispatcher-backed persistence command by setting ChapterViewModel.Target through its private setter in the test harness, which keeps the projection test focused on subscription behavior rather than storage plumbing.

## Verification

Ran the focused corkboard projection suite with dotnet test Hymnal.sln --filter FullyQualifiedName~CorkboardProjectionTests and confirmed all 4 tests passed.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `powershell.exe -NoProfile -Command "Set-Location 'C:\Dev\Hymnal'; & 'C:\Program Files\dotnet\dotnet.exe' test Hymnal.sln --filter FullyQualifiedName~CorkboardProjectionTests; exit $LASTEXITCODE"` | 0 | ✅ pass | 584000ms |

## Deviations

Used reflection in the projection test to set ChapterViewModel.Target directly instead of driving the persistence command path, because the command path depends on dispatcher/persistence behavior that is noisy in the unit test harness.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal/ViewModels/CardViewModel.cs`
- `src/Hymnal/ViewModels/CorkboardItemViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/CorkboardProjectionTests.cs`
