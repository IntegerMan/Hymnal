---
id: T02
parent: S04
milestone: M001
key_files:
  - src/Hymnal/ViewModels/NotesViewModel.cs
  - src/Hymnal/ViewModels/MainWindowViewModel.cs
  - src/Hymnal/App.axaml.cs
key_decisions:
  - Used System.Reactive.Concurrency.TaskPoolScheduler.Default instead of RxApp.TaskpoolScheduler — ReactiveUI.Avalonia 12.0.1 reference in this project does not expose RxApp in the compilation unit
  - LoadNotesAsync sets _text backing field directly (bypassing the public setter) to avoid enqueuing an auto-save for freshly-loaded content
duration: 
verification_result: passed
completed_at: 2026-05-29T20:16:46.850Z
blocker_discovered: false
---

# T02: Added NotesViewModel with reactive chapter observation, throttled auto-save, chapter-switch CancellationToken safety, and ToggleCommand; wired into MainWindowViewModel and App DI

**Added NotesViewModel with reactive chapter observation, throttled auto-save, chapter-switch CancellationToken safety, and ToggleCommand; wired into MainWindowViewModel and App DI**

## What Happened

Created NotesViewModel (singleton, inherits ViewModelBase) with: Text/IsVisible/ChapterTitle INPC properties; Subject-based auto-save throttled at 1500ms via TaskPoolScheduler.Default (RxApp not available in this project's ReactiveUI.Avalonia reference — used System.Reactive.Concurrency.TaskPoolScheduler.Default instead); CancellationTokenSource reset on every chapter switch to prevent stale writes; LoadNotesAsync sets the backing field directly on load to avoid triggering auto-save for the loaded content; FlushSaveAsync guards nodeAtSave == _loadedNode implicitly via CTS; ToggleCommand gated on ActiveNode != null; all subscriptions added to Disposables. Updated MainWindowViewModel constructor signature to accept NotesViewModel and expose it as a public property. Registered INotesService → NotesService and NotesViewModel as singletons in App.axaml.cs and updated MainWindowViewModel factory lambda.

## Verification

dotnet build src/Hymnal/Hymnal.csproj -c Debug --nologo → Build succeeded, 0 errors, 0 warnings.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build src/Hymnal/Hymnal.csproj -c Debug --nologo` | 0 | ✅ pass | 5210ms |

## Deviations

RxApp.TaskpoolScheduler replaced with TaskPoolScheduler.Default — RxApp was not accessible in the Hymnal project despite ReactiveUI.Avalonia being referenced; functionally equivalent.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal/ViewModels/NotesViewModel.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal/App.axaml.cs`
