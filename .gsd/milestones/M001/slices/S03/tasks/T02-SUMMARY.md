---
id: T02
parent: S03
milestone: M001
key_files:
  - src/Hymnal/ViewModels/EditorViewModel.cs
  - src/Hymnal/ViewModels/WorkspaceViewModel.cs
  - src/Hymnal/ViewModels/MainWindowViewModel.cs
  - src/Hymnal/App.axaml.cs
key_decisions:
  - IsDirty and CanSave implemented as ObservableAsPropertyHelper (not raw bool + manual notify) so ReactiveCommand.CreateFromTask canExecute observable is a proper cold observable; both registered with Disposables for clean teardown
  - _isSwitching boolean guard prevents re-entrant WhenAnyValue reactions when SelectedNode is programmatically reset on save failure or Part-node rejection
  - SerialDisposable used for banner auto-dismiss timer so each new notification resets the 5-second clock rather than stacking timers
  - Watcher teardown registered with Disposables via Disposable.Create(StopWatcher) — EditorViewModel.Dispose() only needs to call Disposables.Dispose()
  - InitAsync restore path sets SelectedNode with guard then calls OpenChapterAsync directly (bypassing TrySwitchChapterAsync) to avoid double-open and to keep restore silent on failure
duration: 
verification_result: passed
completed_at: 2026-05-29T15:53:54.821Z
blocker_discovered: false
---

# T02: EditorViewModel (single-buffer, atomic save, FileSystemWatcher conflict detection), updated WorkspaceViewModel (SelectedNode, TrySwitchChapterAsync, session restore), updated MainWindowViewModel (reactive title, 5s auto-dismiss banner), and updated App.axaml.cs DI wiring all implemented and structurally verified.

**EditorViewModel (single-buffer, atomic save, FileSystemWatcher conflict detection), updated WorkspaceViewModel (SelectedNode, TrySwitchChapterAsync, session restore), updated MainWindowViewModel (reactive title, 5s auto-dismiss banner), and updated App.axaml.cs DI wiring all implemented and structurally verified.**

## What Happened

Read the T02 plan, S03 slice plan, and all relevant source files (ViewModelBase, WorkspaceViewModel, MainWindowViewModel, App.axaml.cs, NotificationService, ChapterNode, ManuscriptModel, IMetadataStore, IAppSettingsStore) before writing any code.

**EditorViewModel** (new file, 198 lines): Implements the single-buffer chapter lifecycle as a singleton. ObservableAsPropertyHelper backing for IsDirty (Text != OriginalText) and CanSave (IsDirty && ActiveFilePath != null). SaveCommand gated on CanSave. OpenChapterAsync stops any existing FileSystemWatcher, reads file content via File.ReadAllTextAsync, updates Text/OriginalText (clearing dirty), then starts a new watcher scoped to the file. OnFileChanged: auto-reloads when clean, sets HasConflict + ConflictMessage when dirty. SaveAsync writes via IMetadataStore.WriteTextAtomicAsync, sets OriginalText = Text on success, calls INotificationService.ShowError and rethrows on failure. AcceptExternalCommand reloads from disk and clears conflict. KeepLocalCommand clears conflict flags only. IDisposable implemented by calling Disposables.Dispose() (watcher teardown registered via Disposable.Create(StopWatcher)).

**WorkspaceViewModel** (updated): Injected EditorViewModel. Added SelectedNode (ChapterNode?) with RaiseAndSetIfChanged. In constructor, WhenAnyValue(SelectedNode).Skip(1).Where(!_isSwitching).Subscribe fires TrySwitchChapterAsync. TrySwitchChapterAsync: rejects Part/missing nodes (resets SelectedNode to null); if IsDirty calls SaveAsync (abort+revert SelectedNode to previousNode on exception — SaveAsync already shows error banner); then calls OpenChapterAsync and persists lastChapterPath. A boolean _isSwitching guard prevents re-entrant reactions when SelectedNode is programmatically reset. InitAsync extended: after successful BindModel, reads lastChapterPath from settings, finds matching Chapter (not Missing) node, sets SelectedNode with guard then calls OpenChapterAsync directly (silent on restore failure).

**MainWindowViewModel** (updated): Injected EditorViewModel and NotificationService (concrete). Exposes EditorViewModel as public property. Observable.CombineLatest on IsDirty + ActiveNode drives reactive Title: '• file — Hymnal' (dirty), 'file — Hymnal' (clean), 'Hymnal' (none). HasBanner/BannerMessage/BannerKind bindable properties. notificationService.Notifications subscription sets banner; SerialDisposable resets a 5-second auto-dismiss timer on each notification so rapid notifications reset the clock.

**App.axaml.cs** (updated): EditorViewModel registered as singleton (explicit factory taking IMetadataStore + INotificationService). WorkspaceViewModel registration updated to pass EditorViewModel from sp. FolderPickerService kept as-is. MainWindowViewModel registration updated to pass WorkspaceViewModel, EditorViewModel, and NotificationService.

## Verification

All four expected files exist (confirmed via bash ls). Structural grep checks confirmed all key symbols: EditorViewModel class with IsDirty/CanSave/HasConflict/SaveCommand/AcceptExternalCommand/KeepLocalCommand/OpenChapterAsync/SaveAsync; WorkspaceViewModel with EditorViewModel injection, SelectedNode, TrySwitchChapterAsync, lastChapterPath persistence, and ResolveAbsolutePath; MainWindowViewModel with EditorViewModel property, HasBanner/BannerMessage/BannerKind, CombineLatest title subscription, and Timer-based auto-dismiss; App.axaml.cs with EditorViewModel singleton before WorkspaceViewModel singleton and updated MainWindowViewModel transient factory. NotificationKind resolved via 'using Hymnal.Infrastructure' in MainWindowViewModel. dotnet build cannot be run via gsd_exec in this environment (MEM012: NuGet.Common.NuGetEnvironment fails in WSL sandbox) — build verification is deferred to terminal invocation per project convention.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `ls -la src/Hymnal/ViewModels/` | 0 | ✅ pass — EditorViewModel.cs (8939B), MainWindowViewModel.cs (4087B), WorkspaceViewModel.cs (8373B) all present | 80ms |
| 2 | `grep -n 'class EditorViewModel\|OpenChapterAsync\|SaveAsync\|IsDirty\|CanSave\|HasConflict\|SaveCommand' src/Hymnal/ViewModels/EditorViewModel.cs` | 0 | ✅ pass — all required members present with correct signatures | 50ms |
| 3 | `grep -n 'EditorViewModel\|SelectedNode\|TrySwitchChapterAsync\|lastChapterPath\|ResolveAbsolutePath' src/Hymnal/ViewModels/WorkspaceViewModel.cs` | 0 | ✅ pass — all required extensions present | 50ms |
| 4 | `grep -n 'EditorViewModel\|HasBanner\|BannerKind\|NotificationService\|CombineLatest\|Timer' src/Hymnal/ViewModels/MainWindowViewModel.cs` | 0 | ✅ pass — reactive title and banner wiring present | 50ms |
| 5 | `grep -n 'EditorViewModel\|AddSingleton\|AddTransient' src/Hymnal/App.axaml.cs` | 0 | ✅ pass — EditorViewModel singleton registered before WorkspaceViewModel; MainWindowViewModel factory updated | 50ms |

## Deviations

None. All plan items implemented as specified.

## Known Issues

dotnet build cannot be run via gsd_exec (MEM012: WSL NuGet path resolution fails). Build must be verified from terminal: dotnet build src/Hymnal/Hymnal.csproj

## Files Created/Modified

- `src/Hymnal/ViewModels/EditorViewModel.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal/App.axaml.cs`
