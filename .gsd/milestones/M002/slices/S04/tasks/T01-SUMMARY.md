---
id: T01
parent: S04
milestone: M002
key_files:
  - src/Hymnal/ViewModels/ChapterInfoViewModel.cs
key_decisions:
  - ProximityFill and HasTarget converted from OAPH to plain backing fields with RaiseAndSetIfChanged, driven by per-chapter WhenAnyValue subscriptions on ChapterViewModel — same pattern as WordCount/Status/Target subscriptions already in the file
  - ComputeProximityFill removed entirely — ChapterViewModel.ProximityFill is now the authoritative computed value
duration: 
verification_result: passed
completed_at: 2026-05-31T04:13:27.337Z
blocker_discovered: false
---

# T01: Replaced OAPH-based ProximityFill and HasTarget in ChapterInfoViewModel with plain backing fields driven by live ChapterViewModel subscriptions, eliminating the startup NullReferenceException.

**Replaced OAPH-based ProximityFill and HasTarget in ChapterInfoViewModel with plain backing fields driven by live ChapterViewModel subscriptions, eliminating the startup NullReferenceException.**

## What Happened

ChapterInfoViewModel had two ObservableAsPropertyHelper fields — `_proximityFill` and `_hasTarget` — initialized in the wrong order in the constructor. `_proximityFill` used `WhenAnyValue(x => x.HasTarget, ...)` which evaluates immediately; at that point `_hasTarget` was null (not yet assigned), causing a NullReferenceException on startup. Additionally, `ProximityFill` was permanently stubbed via `ComputeProximityFill` which always returned 0.0.

The fix: both fields converted to plain `double` and `bool` backing fields with full `RaiseAndSetIfChanged` properties. The OAPH initialization blocks were removed from the constructor entirely, along with their `Disposables.Add(...)` lines. In `OnActiveNodeChanged`, two new subscriptions were added after the existing four — `chapterVm.WhenAnyValue(x => x.HasTarget)` and `chapterVm.WhenAnyValue(x => x.ProximityFill)` — both tracked in `_chapterDisposables`. `SyncFromChapterVm` was updated to immediately sync both values on chapter switch. The null-node reset path was updated to set `HasTarget = false; ProximityFill = 0.0`. The now-unreferenced `ComputeProximityFill` stub method was removed entirely. The authoritative `ProximityFill` value now flows from `ChapterViewModel.ProximityFill` (which correctly computes `min(WordCount / effectiveMax, 1.0)`) rather than being permanently 0.0.

## Verification

Built with `dotnet build src/Hymnal/Hymnal.csproj -nologo` — succeeded with 0 errors, 0 warnings. Ran `dotnet test Hymnal.slnx --nologo -v q` — 57 passed, 0 failed. Confirmed: `_proximityFill` and `_hasTarget` are plain backing fields; no OAPH for either exists; `ComputeProximityFill` is absent; `OnActiveNodeChanged` subscribes to both `chapterVm.ProximityFill` and `chapterVm.HasTarget`.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build src/Hymnal/Hymnal.csproj -nologo` | 0 | ✅ pass — 0 errors, 0 warnings | 12150ms |
| 2 | `dotnet test Hymnal.slnx --nologo -v q` | 0 | ✅ pass — 57 passed, 0 failed | 6500ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal/ViewModels/ChapterInfoViewModel.cs`
