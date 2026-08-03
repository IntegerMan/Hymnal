---
estimated_steps: 26
estimated_files: 4
skills_used: []
---

# T02: EditorViewModel (single-buffer, atomic save, FileSystemWatcher conflict detection), updated WorkspaceViewModel (SelectedNode, TrySwitchChapterAsync, session restore), updated MainWindowViewModel (reactive title, 5s auto-dismiss banner), and updated App.axaml.cs DI wiring all implemented and structurally verified.

Why: The behavioral core of S03 — single-buffer chapter management, dirty state, atomic save, save-before-switch, last-chapter restore, and reactive title updates — must live in view-model land before the UI shell can be assembled. This task creates EditorViewModel and extends WorkspaceViewModel and MainWindowViewModel to wire the full chapter lifecycle.

Do:
1. Create `src/Hymnal/ViewModels/EditorViewModel.cs` extending `ViewModelBase` / `ReactiveObject`. It must be a singleton (one buffer at a time). Include:
   - Injected dependencies: `IMetadataStore`, `INotificationService`
   - Properties (all with RaiseAndSetIfChanged): `ActiveNode` (ChapterNode?), `ActiveFilePath` (string?), `Text` (string, default ""), `OriginalText` (string, private set), `IsDirty` (bool, derived as `Text != OriginalText`), `CanSave` (bool, derived as `IsDirty && ActiveFilePath != null`), `HasConflict` (bool), `ConflictMessage` (string?)
   - `SaveCommand`: `ReactiveCommand.CreateFromTask(SaveAsync, this.WhenAnyValue(x => x.CanSave))`
   - `AcceptExternalCommand`: `ReactiveCommand.CreateFromTask` — reload file from disk, update Text and OriginalText, clear HasConflict
   - `KeepLocalCommand`: `ReactiveCommand.Create` — clear HasConflict and ConflictMessage only
   - `OpenChapterAsync(ChapterNode node, string absolutePath)`: dispose any existing chapter file watcher, read file content via `File.ReadAllTextAsync`, set Text = OriginalText = content, set ActiveNode = node, set ActiveFilePath = absolutePath, start new FileSystemWatcher scoped to the file's directory watching just that filename, on change event: if !IsDirty auto-reload and call INotificationService.ShowInfo; if IsDirty set HasConflict = true and ConflictMessage
   - `SaveAsync()`: validate CanSave; call `_metadataStore.WriteTextAtomicAsync(ActiveFilePath!, Text)`; on success set OriginalText = Text (clears IsDirty); on exception call `_notificationService.ShowError(ex.Message)` and throw to propagate to save-before-switch caller
   - Implement `IDisposable` via `Disposables` in ViewModelBase to stop the chapter watcher
2. Extend `WorkspaceViewModel`:
   - Inject `EditorViewModel` into constructor
   - Add `SelectedNode` (ChapterNode?) bindable property
   - Observe `WhenAnyValue(x => x.SelectedNode).Skip(1)` and call `TrySwitchChapterAsync(node)` on each non-null change
   - `TrySwitchChapterAsync(ChapterNode? node)`: if node is null, return; if node.Kind == NodeKind.Part or node.IsMissing, reset SelectedNode to null and return; if EditorViewModel.IsDirty, call `await EditorViewModel.SaveAsync()` — if it throws (caught), call ShowError and reset SelectedNode to the previous node (abort switch); then call `await EditorViewModel.OpenChapterAsync(node, resolveAbsolutePath(node))` and persist `lastChapterPath` to `_settingsStore`
   - `resolveAbsolutePath(ChapterNode node)`: return `Path.Combine(_model!.ManuscriptRoot, node.RelativePath)` — this uses the ManuscriptRoot added in T01
3. Extend `WorkspaceViewModel.InitAsync`: after `BindModel(result.Value!)` succeeds, read `lastChapterPath` from `_settingsStore`; find the matching ChapterNode in `_nodes` by comparing `resolveAbsolutePath(n) == lastChapterPath`; if found and node is a Chapter and not Missing, call `TrySwitchChapterAsync(node)` silently (no banner on restore failure).
4. Extend `MainWindowViewModel`:
   - Inject `EditorViewModel` and `NotificationService` (the concrete class, for banner subscription)
   - Expose `EditorViewModel EditorViewModel { get; }` as public property
   - Subscribe to `Observable.CombineLatest(EditorViewModel.WhenAnyValue(x => x.IsDirty), EditorViewModel.WhenAnyValue(x => x.ActiveNode), (dirty, node) => ...)` to recompute `Title`: when node != null and dirty → `$"• {Path.GetFileName(node.RelativePath)} — Hymnal"`; when node != null and !dirty → `$"{Path.GetFileName(node.RelativePath)} — Hymnal"`; else → "Hymnal"
   - Add `HasBanner` (bool), `BannerMessage` (string?), `BannerKind` (NotificationKind) bindable properties
   - Subscribe to `notificationService.Notifications` to set HasBanner = true, BannerMessage, BannerKind; chain `Observable.Timer(TimeSpan.FromSeconds(5))` after each notification to auto-clear (set HasBanner = false)
5. Update `App.axaml.cs`: register `EditorViewModel` as singleton before `WorkspaceViewModel`; update `WorkspaceViewModel` registration to pass `EditorViewModel` (resolve from `sp`); update `MainWindowViewModel` to be `AddTransient` taking EditorViewModel and NotificationService from `sp`.

Done when: EditorViewModel, updated WorkspaceViewModel, updated MainWindowViewModel, and updated App.axaml.cs all exist and the project compiles (`dotnet build src/Hymnal/Hymnal.csproj` passes).

## Inputs

- `src/Hymnal.Core/Interfaces/IMetadataStore.cs`
- `src/Hymnal.Core/Infrastructure/MetadataStore.cs`
- `src/Hymnal.Core/Models/ManuscriptModel.cs`
- `src/Hymnal.Core/Models/ChapterNode.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal/App.axaml.cs`
- `src/Hymnal/Infrastructure/NotificationService.cs`

## Expected Output

- `src/Hymnal/ViewModels/EditorViewModel.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal/App.axaml.cs`

## Verification

dotnet build src/Hymnal/Hymnal.csproj

## Observability Impact

EditorViewModel.SaveAsync routes all save failures through INotificationService.ShowError, surfacing them to the banner in T03. IsDirty and HasConflict properties expose editor state for debugging in reactive subscriptions.
