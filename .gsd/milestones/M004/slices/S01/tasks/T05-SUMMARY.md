---
id: T05
parent: S01
milestone: M004
key_files:
  - src/Hymnal/Views/CorkboardView.axaml
  - src/Hymnal/Views/CorkboardView.axaml.cs
  - src/Hymnal/ViewModels/CorkboardViewModel.cs
  - tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs
key_decisions:
  - (none)
duration: 
verification_result: passed
completed_at: 2026-06-04T04:10:30.922Z
blocker_discovered: false
---

# T05: Built the Plan-mode corkboard surface with compact cards, in-view context actions, keyboard/open behavior, and smoke coverage.

**Built the Plan-mode corkboard surface with compact cards, in-view context actions, keyboard/open behavior, and smoke coverage.**

## What Happened

Implemented a polished `CorkboardView` for Plan mode with a compact header, empty state, chapter cards, part dividers, target/progress and phase-date rows, selection styling, and card interaction wiring. Card open behavior stays on the existing `CorkboardViewModel` command surface, while rename/new/include/remove/delete actions are surfaced from the card context menu through code-behind prompt/confirm dialogs and the existing structural edit commands. Drag-reorder is wired through a simple drag gesture in code-behind that feeds the ViewModel reorder command and ignores invalid targets. I also added a smoke test that loads the view, verifies empty/populated state binding, and documented a manual interaction checklist for the brittle UI flows.

## Verification

Verified by running `dotnet test Hymnal.sln --filter FullyQualifiedName~CorkboardViewSmokeTests`, which passed after fixing the Avalonia 12 XAML and drag/drop API mismatches and swapping the test-loaded icon to a text glyph so the view can construct without a render interface.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test Hymnal.sln --filter FullyQualifiedName~CorkboardViewSmokeTests` | 0 | ✅ pass | 657ms |

## Deviations

Used code-behind prompt/confirmation windows for Rename/New/Include/Delete instead of trying to force the dialogs through XAML-only bindings; attached drag/drop handlers from code-behind because Avalonia 12 does not expose the older Button XAML drag properties used in prior versions.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal/Views/CorkboardView.axaml`
- `src/Hymnal/Views/CorkboardView.axaml.cs`
- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs`
