---
estimated_steps: 14
estimated_files: 1
skills_used: []
---

# T03: ChapterViewModel: wrap ChapterNode with reactive status and ChangeStatusCommand

**Why:** ChapterViewModel is the single binding target for the sidebar list and later the Chapter Info pane (S03). It wraps ChapterNode (immutable record) to add mutable reactive status state without touching Core models.

**Do:**
1. Create `src/Hymnal/ViewModels/ChapterViewModel.cs`. Follows the `NotesViewModel` IDisposable lifecycle pattern.
   - Constructor: `ChapterViewModel(ChapterNode node, string uuid, PhaseData? phaseData, PhaseDataService phaseDataService, IAppSettingsStore settingsStore, INotificationService notificationService, string workspaceRoot)`. Store all injected values as private fields.
   - `public ChapterNode Node { get; }` — exposes the underlying ChapterNode for EditorViewModel.
   - `public string Uuid { get; }` — the UUID string.
   - Backing field + `RaiseAndSetIfChanged` for `public ChapterStatus Status { get; private set; }` — initialized from phaseData?.Status ?? ChapterStatus.Outlining.
   - `public PhaseData? PhaseData { get; private set; }` — reactive property.
   - `public ReactiveCommand<ChapterStatus, Unit> ChangeStatusCommand { get; }` — created with `ReactiveCommand.CreateFromTask<ChapterStatus>(ChangeStatusAsync)`. ThrownExceptions subscribed: `notificationService.ShowError(ex.Message)`.
   - `private async Task ChangeStatusAsync(ChapterStatus newStatus)`: reads `prefillPhaseDate` from settingsStore (default true if absent); builds new `PhaseData { Status = newStatus, PhaseStartDate = prefill ? DateTime.UtcNow.ToString("yyyy-MM-dd") : PhaseData?.PhaseStartDate }`. Calls `phaseDataService.SaveAsync` with updated dict (load current, upsert this uuid, save). Sets `Status` and `PhaseData` on UI thread via `RxApp.MainThreadScheduler.Schedule`.
   - `IDisposable` impl: dispose Disposables CompositeDisposable.
   - Note: `Disposables` is inherited from `ViewModelBase`; verify this is the pattern used in NotesViewModel and reuse it.

**Constraint:** `ChapterViewModel` lives in `src/Hymnal/ViewModels/` (Avalonia project) since it references ReactiveUI and INotificationService. It takes PhaseDataService (from Hymnal.Core) and IAppSettingsStore (Core interface) — both are Core types, so no circular dependency.

**Done when:** `dotnet build src/Hymnal/Hymnal.csproj` exits 0.

## Inputs

- `src/Hymnal.Core/Models/ChapterNode.cs`
- `src/Hymnal.Core/Models/ChapterStatus.cs`
- `src/Hymnal.Core/Models/PhaseData.cs`
- `src/Hymnal.Core/Services/PhaseDataService.cs`
- `src/Hymnal.Core/Interfaces/IAppSettingsStore.cs`
- `src/Hymnal.Core/Interfaces/INotificationService.cs`
- `src/Hymnal/ViewModels/ViewModelBase.cs`
- `src/Hymnal/ViewModels/NotesViewModel.cs`

## Expected Output

- `src/Hymnal/ViewModels/ChapterViewModel.cs`

## Verification

dotnet build src/Hymnal/Hymnal.csproj
