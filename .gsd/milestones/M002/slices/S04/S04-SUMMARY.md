---
id: S04
parent: M002
milestone: M002
provides:
  - (none)
requires:
  []
affects:
  []
key_files:
  - src/Hymnal/ViewModels/ChapterInfoViewModel.cs
key_decisions:
  - ChapterInfoViewModel now uses plain backing fields for ProximityFill and HasTarget instead of OAPHs to avoid constructor-time self-dependency and null-init hazards.
  - ChapterViewModel.ProximityFill and HasTarget remain the authoritative live sources for ChapterInfoViewModel subscriptions.
  - Adjacent OAPH chains in MainWindowViewModel and ChapterViewModel were reviewed and require no code changes.
patterns_established:
  - Use plain backing fields plus RaiseAndSetIfChanged when a value is driven directly by live per-chapter subscriptions.
  - Keep ChapterInfo state synchronized from ChapterViewModel subscriptions instead of derived display-string parsing.
observability_surfaces:
  - none
drill_down_paths:
  []
duration: ""
verification_result: passed
completed_at: 2026-05-31T05:52:06.220Z
blocker_discovered: false
---

# S04: Runtime Stabilization and Override Alignment

**Chapter Info now starts safely, reflects live chapter target/proximity state, and the full solution still builds and tests cleanly.**

## What Happened

T01 replaced ChapterInfoViewModel's constructor-time OAPH setup for ProximityFill and HasTarget with plain backing fields driven by the existing per-chapter subscription pattern, eliminating the startup NullReferenceException and removing the stale ComputeProximityFill stub so ChapterViewModel becomes the authoritative source for target/proximity state. T02 then performed the defensive sweep called for in the slice plan, confirming MainWindowViewModel's OAPHs are non-self-referential and ChapterViewModel's OAPHs only depend on plain backing fields. The solution was verified end-to-end with a clean build and full test run after the wiring fix, leaving the workspace in a runnable state for S05 desktop UAT.

## Verification

Code inspection confirmed ChapterInfoViewModel no longer contains OAPHs for _proximityFill or _hasTarget and now subscribes to ChapterViewModel.HasTarget and ChapterViewModel.ProximityFill. Fresh verification passed: dotnet build src/Hymnal/Hymnal.csproj -nologo exited 0 with 0 errors and 0 warnings, and dotnet test Hymnal.sln -nologo passed 57 tests with 0 failures. Defensive sweep confirmed MainWindowViewModel OAPHs observe external view models and do not self-reference, while ChapterViewModel OAPHs depend on plain backing fields only.

## Requirements Advanced

- R004 — Advanced live target/proximity wiring by ensuring ChapterInfoViewModel consumes authoritative ChapterViewModel state without stubbed fallback behavior.

## Requirements Validated

None.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

None.

## Known Limitations

S05 still owns the full desktop UAT sweep for restart persistence, rename survival, and latency evidence.

## Follow-ups

Run S05 desktop UAT to validate the complete user workflow end-to-end.

## Files Created/Modified

- `src/Hymnal/ViewModels/ChapterInfoViewModel.cs` — Removed OAPH-based ProximityFill/HasTarget wiring, added live subscriptions, and eliminated the startup crash path.
