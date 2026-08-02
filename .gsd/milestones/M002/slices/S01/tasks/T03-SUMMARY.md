---
id: T03
parent: S01
milestone: M002
key_files:
  - src/Hymnal/ViewModels/ChapterViewModel.cs
key_decisions:
  - Used Avalonia.Threading.Dispatcher.UIThread.InvokeAsync for UI-thread dispatch — RxApp not available from ReactiveUI.Avalonia alone; DispatcherOperation does not support ConfigureAwait so awaited directly.
duration: 
verification_result: passed
completed_at: 2026-05-30T19:17:25.223Z
blocker_discovered: false
---

# T03: Created ChapterViewModel wrapping ChapterNode with reactive Status/PhaseData and ChangeStatusCommand that persists to PhaseDataService; build clean at exit 0.

**Created ChapterViewModel wrapping ChapterNode with reactive Status/PhaseData and ChangeStatusCommand that persists to PhaseDataService; build clean at exit 0.**

## What Happened

Read ViewModelBase (Disposables pattern), NotesViewModel (IDisposable + ReactiveCommand lifecycle), and all Core types (ChapterNode, ChapterStatus, PhaseData, PhaseDataService, IAppSettingsStore, INotificationService) before writing.

Created `src/Hymnal/ViewModels/ChapterViewModel.cs` as a sealed class extending ViewModelBase and implementing IDisposable. Key implementation points:

- Constructor stores all injected services; initialises `_status` from `phaseData?.Status ?? ChapterStatus.Outlining` and `_phaseData` from the passed value.
- `Status` and `PhaseData` are `RaiseAndSetIfChanged` reactive properties; `Status` setter is `private set`.
- `ChangeStatusCommand = ReactiveCommand.CreateFromTask<ChapterStatus>(ChangeStatusAsync)` with `ThrownExceptions` subscribed to `notificationService.ShowError(ex.Message)` via `Disposables.Add`.
- `ChangeStatusAsync`: reads `prefillPhaseDate` from `IAppSettingsStore` (null → defaults to true), builds updated `PhaseData`, loads current phases dict, upserts this UUID's entry, saves, then dispatches state update to UI thread via `Avalonia.Threading.Dispatcher.UIThread.InvokeAsync`.
- `Dispose()` guards against double-dispose and calls `Disposables.Dispose()`.

Two build fixes required:
1. `RxApp.MainThreadScheduler.Schedule` — not available without standalone `ReactiveUI` package (only `ReactiveUI.Avalonia` is referenced); replaced with `Dispatcher.UIThread.InvokeAsync`.
2. `DispatcherOperation.ConfigureAwait(false)` — not supported; removed and awaited directly.

## Verification

dotnet build src/Hymnal/Hymnal.csproj — exit 0, 0 warnings, 0 errors (9.75s).

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build src/Hymnal/Hymnal.csproj` | 0 | ✅ pass | 9750ms |

## Deviations

Plan specified `RxApp.MainThreadScheduler.Schedule` for UI-thread dispatch; replaced with `Avalonia.Threading.Dispatcher.UIThread.InvokeAsync` because the project only references `ReactiveUI.Avalonia` (not standalone `ReactiveUI`). Functionally identical.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal/ViewModels/ChapterViewModel.cs`
