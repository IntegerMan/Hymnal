---
id: T01
parent: S03
milestone: M002
key_files:
  - src/Hymnal/ViewModels/ChapterInfoViewModel.cs
  - src/Hymnal/ViewModels/ChapterViewModel.cs
  - src/Hymnal/ViewModels/MainWindowViewModel.cs
  - src/Hymnal/App.axaml.cs
key_decisions:
  - DisposeWith not used — codebase pattern is Disposables.Add(subscription); applied same for _chapterDisposables
  - ApplyPhaseData uses CheckAccess() thread-safety guard matching UpdateWordCount pattern
  - ProximityFill OAPH in ChapterInfoViewModel stubs at 0.0 — authoritative fill will be bound from ChapterViewModel.ProximityFill in the View (T02/T03)
  - ChapterInfoViewModel.SetTargetCommand delegates to ChapterViewModel.SetTargetCommand to preserve lock ownership in TargetsService
duration: 
verification_result: passed
completed_at: 2026-05-31T03:24:09.772Z
blocker_discovered: false
---

# T01: Created ChapterInfoViewModel with status/date/target commands and DI wiring; added ApplyPhaseData mutator to ChapterViewModel; wired IsAnyRightPaneOpen and IsBothRightPanesOpen aggregates into MainWindowViewModel

**Created ChapterInfoViewModel with status/date/target commands and DI wiring; added ApplyPhaseData mutator to ChapterViewModel; wired IsAnyRightPaneOpen and IsBothRightPanesOpen aggregates into MainWindowViewModel**

## What Happened

Implemented the four-file change set that forms the ViewModel backbone for the F3 Chapter Info pane.

**ChapterViewModel.cs — ApplyPhaseData mutator (Step 1)**
Added `public void ApplyPhaseData(PhaseData phaseData)` following the `UpdateWordCount` pattern. Uses `Dispatcher.UIThread.CheckAccess()` to decide whether to set properties directly or post via `InvokeAsync`, allowing safe calls from both UI and background threads.

**ChapterInfoViewModel.cs — new file (Step 2)**
Created the full lifecycle VM modelled on NotesViewModel:
- Constructor params: `EditorViewModel`, `WorkspaceViewModel`, `PhaseDataService`, `TargetsService`, `IAppSettingsStore`, `INotificationService`
- Private state: `_activeChapterVm`, `_saveCts` (CancellationTokenSource), `_loadedUuid`, `_chapterDisposables` (per-chapter CompositeDisposable)
- Observable props: `IsVisible`, `ChapterTitle`, `Status`, `PhaseStartDate`, `PhaseEndDate`, `WordCount`, `WordCountDisplay` (OAPH), `TargetDisplay`, `ProximityFill` (OAPH), `HasTarget` (OAPH), `PrefillPhaseDate`
- `ToggleCommand`: gated on `ActiveNode != null`, toggles IsVisible when `_loadedUuid != null`
- `SetStatusCommand`: `ReactiveCommand.CreateFromTask<ChapterStatus>` — calls `PhaseDataService.UpsertAsync` with prefill-date toggle; calls `ApplyPhaseData` on ChapterViewModel
- `SaveDatesCommand`: `ReactiveCommand.CreateFromTask` — persists current date fields preserving status; calls `ApplyPhaseData`
- `SetTargetCommand`: `ReactiveCommand.CreateFromTask<int?>` — delegates to `ChapterViewModel.SetTargetCommand`
- `OnActiveNodeChanged`: cancels CTS, disposes per-chapter subscriptions, locates ChapterViewModel by `RelativePath`, syncs state, re-subscribes WordCount/Status/PhaseData/Target via `_chapterDisposables.Add()` (not `DisposeWith` — that extension is not used in this codebase)
- PrefillPhaseDate persisted to `IAppSettingsStore` key `"prefillPhaseDate"` with silent fail on load/save
- All `ThrownExceptions` routed to `notificationService.ShowError()`

**MainWindowViewModel.cs (Step 3)**
- Added `ChapterInfoViewModel ChapterInfoViewModel { get; }` property
- Added `IsAnyRightPaneOpen` and `IsBothRightPanesOpen` OAPHs using `Observable.CombineLatest` over both pane `IsVisible` streams
- Constructor accepts `ChapterInfoViewModel chapterInfoViewModel`

**App.axaml.cs (Step 4)**
- Registered `ChapterInfoViewModel` as singleton after `NotesViewModel`, before `MainWindowViewModel`
- Passed to `MainWindowViewModel` factory call

**Deviation noted:** `DisposeWith(CompositeDisposable)` extension is not used anywhere in the existing ViewModels (the ViewModelBase comment mentions it but the actual pattern throughout is `Disposables.Add(subscription)`). Switched to `_chapterDisposables.Add(subscription)` pattern accordingly.

**ProximityFill note:** `ComputeProximityFill` returns 0.0 as a stub — the authoritative proximity fill for the UI pane will be bound directly from `ChapterViewModel.ProximityFill` in the View layer (T02/T03). The OAPH is present for binding infrastructure.

## Verification

dotnet build src/Hymnal/Hymnal.csproj -nologo → exit 0, 0 warnings, 0 errors (10s)
dotnet test Hymnal.sln --nologo → 57 passed, 0 failed, 0 skipped

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build src/Hymnal/Hymnal.csproj -nologo` | 0 | ✅ pass | 9910ms |
| 2 | `dotnet test Hymnal.sln --nologo` | 0 | ✅ pass — 57 passed, 0 failed | 5200ms |

## Deviations

DisposeWith() extension not available in this codebase despite ViewModelBase doc comment; used _chapterDisposables.Add() pattern consistent with all other ViewModels.

## Known Issues

ComputeProximityFill returns 0.0 stub — the View should bind directly to _activeChapterVm.ProximityFill or a re-exposed OAPH fed from ChapterViewModel.ProximityFill will be needed in a subsequent task.

## Files Created/Modified

- `src/Hymnal/ViewModels/ChapterInfoViewModel.cs`
- `src/Hymnal/ViewModels/ChapterViewModel.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal/App.axaml.cs`
