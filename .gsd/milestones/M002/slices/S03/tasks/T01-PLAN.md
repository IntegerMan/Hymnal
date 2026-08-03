---
estimated_steps: 26
estimated_files: 4
skills_used: []
---

# T01: ChapterInfoViewModel, ApplyPhaseData mutator, and DI wiring

**Why:** ChapterInfoViewModel is the logic backbone for the F3 pane. It must mirror the NotesViewModel lifecycle pattern (observe ActiveNode, cancel-on-switch CancellationToken, gated ToggleCommand) while adding status-change, date-edit, and target-edit commands that persist via PhaseDataService/TargetsService with optional phase-date pre-fill. PhaseDataService.UpsertAsync must be called directly by ChapterInfoViewModel (not via ChapterViewModel.ChangeStatusCommand) so the pre-fill toggle can be honoured; ChapterViewModel then needs a public ApplyPhaseData() mutator to re-sync observable state.

**Steps:**
1. Add `public void ApplyPhaseData(PhaseData phaseData)` to `ChapterViewModel.cs` — sets `Status = phaseData.Status; PhaseData = phaseData;` on the UI thread (use `Avalonia.Threading.Dispatcher.UIThread.InvokeAsync` if called from non-UI context; if called from UI context directly, plain assignment is fine). Mirror the `UpdateWordCount(int)` pattern.
2. Create `src/Hymnal/ViewModels/ChapterInfoViewModel.cs`:
   - Constructor params: `EditorViewModel editorViewModel`, `WorkspaceViewModel workspaceViewModel`, `PhaseDataService phaseDataService`, `TargetsService targetsService`, `IAppSettingsStore settingsStore`, `INotificationService notificationService`
   - Private fields: `_activeChapterVm` (ChapterViewModel?), `_saveCts` (CancellationTokenSource), `_loadedUuid` (string?)
   - Public observable props: `IsVisible` (bool, private set), `ChapterTitle` (string?, private set)
   - Bindable display props (wrapping active ChapterViewModel state): `Status` (ChapterStatus), `PhaseStartDate` (string?, ISO 8601), `PhaseEndDate` (string?, ISO 8601), `WordCount` (int, read from ChapterViewModel.WordCount), `WordCountDisplay` (string, delegates to ChapterViewModel.WordCountDisplay), `TargetDisplay` (string?, shows MinWords or '—'), `ProximityFill` (double, delegates to ChapterViewModel.ProximityFill), `HasTarget` (bool, delegates to ChapterViewModel.HasTarget), `PrefillPhaseDate` (bool, persisted via AppSettingsStore key "prefillPhaseDate")
   - `ToggleCommand`: `ReactiveCommand.Create`, `canExecute: editorViewModel.WhenAnyValue(x => x.ActiveNode).Select(n => n != null)`, toggles IsVisible when `_loadedUuid != null`
   - `SetStatusCommand: ReactiveCommand.CreateFromTask<ChapterStatus>` — calls `PhaseDataService.UpsertAsync` respecting PrefillPhaseDate toggle; then calls `_activeChapterVm.ApplyPhaseData(updated)` on UI thread
   - `SaveDatesCommand: ReactiveCommand.CreateFromTask` — reads current PhaseStartDate/PhaseEndDate string fields; calls `PhaseDataService.UpsertAsync` preserving current status; calls `_activeChapterVm.ApplyPhaseData(updated)` on UI thread
   - `SetTargetCommand: ReactiveCommand.CreateFromTask<int?>` — constructs `WordCountTarget{MinWords=value}` and calls `_activeChapterVm.SetTargetCommand.Execute(target).Subscribe()`; or null to clear
   - Lifecycle: observe `editorViewModel.WhenAnyValue(x => x.ActiveNode)` → `OnActiveNodeChanged(ChapterNode? node)`. On node change: cancel CTS, if null clear state; if non-null find ChapterViewModel from `workspaceViewModel.Nodes.FirstOrDefault(vm => vm.Node.RelativePath == node.RelativePath)`, store as `_activeChapterVm`, load state from it + observe its reactive props via WhenAnyValue subscriptions (for live word count, status, target — re-subscribe on each chapter switch using `DisposeWith` on a per-chapter CompositeDisposable). Restore IsVisible = wasVisible after switch.
   - Load PrefillPhaseDate preference async on construction (default true).
   - All ThrownExceptions → `notificationService.ShowError()`
3. Modify `src/Hymnal/ViewModels/MainWindowViewModel.cs`:
   - Add `public ChapterInfoViewModel ChapterInfoViewModel { get; }` property
   - Add two OAPH computed booleans:
     - `IsAnyRightPaneOpen`: `Observable.CombineLatest(ChapterInfoViewModel.WhenAnyValue(x => x.IsVisible), NotesViewModel.WhenAnyValue(x => x.IsVisible), (a, b) => a || b).ToProperty(this, x => x.IsAnyRightPaneOpen)`
     - `IsBothRightPanesOpen`: same CombineLatest with `(a, b) => a && b`
   - Accept `ChapterInfoViewModel chapterInfoViewModel` as constructor parameter; assign to property
4. Modify `src/Hymnal/App.axaml.cs`:
   - Register `services.AddSingleton<ChapterInfoViewModel>(sp => new ChapterInfoViewModel(sp.GetRequiredService<EditorViewModel>(), sp.GetRequiredService<WorkspaceViewModel>(), sp.GetRequiredService<PhaseDataService>(), sp.GetRequiredService<TargetsService>(), sp.GetRequiredService<IAppSettingsStore>(), sp.GetRequiredService<INotificationService>()))`
   - Add it to the `MainWindowViewModel` factory call: pass `sp.GetRequiredService<ChapterInfoViewModel>()`
   - Registration order: after WorkspaceViewModel, before MainWindowViewModel

**Done when:** `dotnet build src/Hymnal/Hymnal.csproj -nologo` exits 0.

## Inputs

- `src/Hymnal/ViewModels/ChapterViewModel.cs`
- `src/Hymnal/ViewModels/NotesViewModel.cs`
- `src/Hymnal/ViewModels/EditorViewModel.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal/App.axaml.cs`
- `src/Hymnal.Core/Interfaces/IAppSettingsStore.cs`
- `src/Hymnal.Core/Models/PhaseData.cs`
- `src/Hymnal.Core/Models/ChapterStatus.cs`
- `src/Hymnal.Core/Models/WordCountTarget.cs`
- `src/Hymnal.Core/Services/PhaseDataService.cs`
- `src/Hymnal.Core/Services/TargetsService.cs`

## Expected Output

- `src/Hymnal/ViewModels/ChapterInfoViewModel.cs`
- `src/Hymnal/ViewModels/ChapterViewModel.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal/App.axaml.cs`

## Verification

dotnet build src/Hymnal/Hymnal.csproj -nologo
