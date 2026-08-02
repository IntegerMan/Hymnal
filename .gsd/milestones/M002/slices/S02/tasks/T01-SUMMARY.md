---
id: T01
parent: S02
milestone: M002
key_files:
  - src/Hymnal/ViewModels/EditorViewModel.cs
  - src/Hymnal/ViewModels/ChapterViewModel.cs
  - src/Hymnal/ViewModels/WorkspaceViewModel.cs
  - src/Hymnal/App.axaml.cs
key_decisions:
  - AvaloniaScheduler.Instance (from ReactiveUI.Avalonia namespace) used for ObserveOn instead of RxApp.MainThreadScheduler — RxApp failed CS0103 in this build config
  - Single-arg WhenAnyValue selector overload is ambiguous with two-arg overload — chain .Select() separately instead
duration: 
verification_result: passed
completed_at: 2026-05-31T02:53:49.145Z
blocker_discovered: false
---

# T01: EditorViewModel gains 300ms-debounced LiveWordCount + Saved observable; ChapterViewModel gains word-count state, target OAPH props, and three target commands

**EditorViewModel gains 300ms-debounced LiveWordCount + Saved observable; ChapterViewModel gains word-count state, target OAPH props, and three target commands**

## What Happened

Resumed after prior-session interruption. Git state was clean (57 tests passing on milestone/M002 branch with main merged in). Implemented T01 across three files:

**EditorViewModel**: Added `WordCountService` constructor injection, `Subject<Unit> _savedSubject` with `IObservable<Unit> Saved` public accessor, and `ObservableAsPropertyHelper<int> _liveWordCount` wired via `WhenAnyValue(x => x.Text).Throttle(300ms, TaskPoolScheduler.Default).Select(CountWords).ObserveOn(AvaloniaScheduler.Instance).ToProperty(...)`. Added `using ReactiveUI.Avalonia` for `AvaloniaScheduler` (which is the correct scheduler class in ReactiveUI.Avalonia 12). `_savedSubject.OnNext(Unit.Default)` fires in `SaveAsync` immediately after `OriginalText = Text`.

**ChapterViewModel**: Added mutable backing properties: `WordCount` (int, default 0), `WordCountKnown` (bool, default false), `PartTotalWordCount` (int, public setter for WorkspaceViewModel), `Target` (WordCountTarget?), `PendingMinWords`/`PendingMaxWords` (int?, staging for flyout). Added four OAPHs: `WordCountDisplay` ('—' until known, then '$count N0 w'), `PartTotalDisplay` (same format for part totals), `ProximityFill` (0.0–1.0 proximity bar fraction, clamped at 1.0), `HasTarget` (bool). Added `TargetsService _targetsService` injection plus three commands: `SetTargetCommand<WordCountTarget?>` (upsert/delete via TargetsService), `ConfirmTargetCommand` (builds target from pending fields), `ClearTargetCommand` (calls SetTarget(null)). All three commands have ThrownExceptions wired to INotificationService.ShowError. Added `UpdateWordCount(int)` public mutator.

**WorkspaceViewModel**: Added `TargetsService _targetsService` field and constructor parameter. Updated ChapterViewModel construction call to pass `_targetsService` as the new 5th parameter.

**App.axaml.cs**: Registered `WordCountService` and `TargetsService` as singletons. Updated `EditorViewModel` factory to pass `WordCountService`. Updated `WorkspaceViewModel` factory to pass `TargetsService`.

One gotcha during implementation: `RxApp.TaskpoolScheduler` and `RxApp.MainThreadScheduler` failed to compile (CS0103) despite `using ReactiveUI;` — possibly a symbol visibility issue in this ReactiveUI 12.0.1 build config. Fixed by using `TaskPoolScheduler.Default` (from `System.Reactive.Concurrency`) for Throttle and `AvaloniaScheduler.Instance` (from `ReactiveUI.Avalonia` namespace) for ObserveOn. Also the single-arg `WhenAnyValue(x => x.Prop, selector)` overload was ambiguous — resolved by chaining `.Select()` separately.

## Verification

dotnet build src/Hymnal/Hymnal.csproj -nologo → Build succeeded (0 errors). dotnet test Hymnal.sln -nologo → Passed: 57, Failed: 0.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build src/Hymnal/Hymnal.csproj -nologo` | 0 | ✅ pass | 5200ms |
| 2 | `dotnet test Hymnal.sln -nologo` | 0 | ✅ pass — 57 passed, 0 failed | 3800ms |

## Deviations

WorkspaceViewModel required TargetsService injection (to pass into ChapterViewModel factory) — not listed as a modified file in the task plan, but is a necessary structural change. No logic in WorkspaceViewModel was changed beyond the field/constructor and the one ChapterViewModel construction call.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal/ViewModels/EditorViewModel.cs`
- `src/Hymnal/ViewModels/ChapterViewModel.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/App.axaml.cs`
