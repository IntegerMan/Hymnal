---
estimated_steps: 20
estimated_files: 3
skills_used: []
---

# T02: Added NotesViewModel with reactive chapter observation, throttled auto-save, chapter-switch CancellationToken safety, and ToggleCommand; wired into MainWindowViewModel and App DI

Why: The notes panel needs a ViewModel that observes the active chapter, loads notes on chapter open, clears on chapter close, and debounce-saves text changes without stale writes on chapter switch.

Do:
1. Create `src/Hymnal/ViewModels/NotesViewModel.cs` (singleton, inherits `ViewModelBase`):
   - Properties: `string Text` (INPC), `bool IsVisible`, `string? ChapterTitle`
   - Constructor params: `EditorViewModel editorViewModel, WorkspaceViewModel workspaceViewModel, INotesService notesService, INotificationService notificationService`
   - On construction, subscribe to `editorViewModel.WhenAnyValue(x => x.ActiveNode)`:
     - If null: set `Text = ""`, `ChapterTitle = null`, `IsVisible = false`
     - If non-null Chapter node: set `ChapterTitle = node.Title`, load notes via `notesService.LoadAsync(derivedPath)`, set `Text = loaded`, show panel if it was already toggled on
   - Auto-save: use a `Subject<string> _saveSubject`; in `Text` setter, call `_saveSubject.OnNext(value)`; in constructor subscribe `_saveSubject.Throttle(TimeSpan.FromMilliseconds(1500), RxApp.TaskpoolScheduler).Subscribe(async text => await FlushSaveAsync(text))`
   - `FlushSaveAsync`: guard that `ActiveNode` is still the same node that triggered the text (capture node ref at load time, compare in flush); call `notesService.SaveAsync`; on error call `notificationService.ShowError`
   - Chapter-switch safety: when `ActiveNode` changes, cancel pending save by incrementing a version counter or by completing and recreating the subject (simplest: use a `CancellationTokenSource` field reset on each chapter load; pass the token into the async flush and early-exit if cancelled)
   - `ToggleCommand`: `ReactiveCommand<Unit, Unit>` that flips `IsVisible` when `ActiveNode != null`; if no active chapter, toggle does nothing (IsVisible stays false)
   - Subscribe `ThrownExceptions` on all ReactiveCommands
   - Dispose all subscriptions via `CompositeDisposable` in `Dispose()`

2. Register in `src/Hymnal/App.axaml.cs`:
   - `INotesService` → `NotesService` as singleton
   - `NotesViewModel` as singleton (after EditorViewModel and WorkspaceViewModel)
   - Inject `NotesViewModel` into the `MainWindowViewModel` factory lambda

3. Add `NotesViewModel NotesViewModel { get; }` property to `src/Hymnal/ViewModels/MainWindowViewModel.cs` and update its constructor to accept and store it.

Done when: `dotnet build src/Hymnal/Hymnal.csproj -c Debug --nologo` exits 0 with 0 errors.

## Inputs

- `src/Hymnal.Core/Interfaces/INotesService.cs`
- `src/Hymnal.Core/Infrastructure/NotesService.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/ViewModels/EditorViewModel.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal/App.axaml.cs`

## Expected Output

- `src/Hymnal/ViewModels/NotesViewModel.cs`

## Verification

dotnet build src/Hymnal/Hymnal.csproj -c Debug --nologo
