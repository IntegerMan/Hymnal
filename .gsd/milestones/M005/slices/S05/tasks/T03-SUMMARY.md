---
id: T03
parent: S05
milestone: M005
key_files:
  - src/Hymnal/Views/CorkboardView.axaml.cs
  - src/Hymnal/Views/CorkboardView.axaml
  - tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs
key_decisions:
  - MEM109: Keep Corkboard drag/drop code-behind as a thin mapper with static helper predicates/request builders and route all structural mutations through CorkboardViewModel/Core services.
duration: 
verification_result: passed
completed_at: 2026-06-18T13:56:14.695Z
blocker_discovered: false
---

# T03: Wired Corkboard Part headers and empty-Part hints into drag/drop and routed all card drops through rich CorkboardDropRequest helpers.

**Wired Corkboard Part headers and empty-Part hints into drag/drop and routed all card drops through rich CorkboardDropRequest helpers.**

## What Happened

Updated CorkboardView drag/drop wiring so included present chapter cards remain the only drag sources while Part divider headers and empty-Part hint regions now act as real drop targets. Added small static helper predicates and request builders on CorkboardView to validate drag eligibility and create CorkboardDropRequest values for chapter-card, Part-header, and empty-Part drops, keeping code-behind limited to UI event mapping and leaving all Book.txt/file mutations in CorkboardViewModel. Switched card-target drops to emit the richer DropCardCommand request shape from T02, attached Loaded-based drop handlers to Part divider and empty-Part templates in AXAML, and expanded CorkboardViewSmokeTests to cover handler wiring, helper behavior, and a manual desktop checklist for same-Part reorder, cross-Part move, empty-Part drops, collapsed-Part header drops, restart verification, and invalid-drop feedback.

## Verification

Ran the task-plan smoke-test command for CorkboardViewSmokeTests and it exited 0 after the view/code-behind/XAML updates. Verified the smoke test suite now covers XAML drop-handler declarations plus helper-level drag-source and request-shape assertions for chapter cards, Part dividers, and empty-Part hints.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter CorkboardViewSmokeTests --verbosity minimal"` | 0 | ✅ pass | 3858ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal/Views/CorkboardView.axaml.cs`
- `src/Hymnal/Views/CorkboardView.axaml`
- `tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs`
