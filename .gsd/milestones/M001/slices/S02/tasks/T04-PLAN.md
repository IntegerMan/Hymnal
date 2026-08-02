---
estimated_steps: 23
estimated_files: 4
skills_used: []
---

# T04: WorkspaceViewModel, FolderPickerService, SidebarView, and NodeKindConverters implemented; DynamicData Bind pattern wires ManuscriptModel.Nodes to ReadOnlyObservableCollection for AXAML

Why: WorkspaceViewModel bridges ManuscriptService + AppSettingsStore + IFolderPickerService for the UI. SidebarView renders the Part/Chapter tree. FolderPickerService wraps Avalonia's IStorageProvider.

Do:
1. Create `src/Hymnal/Infrastructure/FolderPickerService.cs` implementing `IFolderPickerService`:
   - Constructor takes `Func<TopLevel?>` topLevelAccessor (injected from MainWindow code-behind via DI or factory).
   - `PickFolderAsync()`: calls `topLevelAccessor()?.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { AllowMultiple = false })`. Returns the local path of the first result or null if cancelled.
   - Use `using Avalonia.Platform.Storage;`.
2. Create `src/Hymnal/ViewModels/WorkspaceViewModel.cs`:
   - Constructor: `ManuscriptService manuscriptService, IAppSettingsStore settingsStore, IFolderPickerService folderPicker, INotificationService notificationService`
   - Exposes `ReadOnlyObservableCollection<ChapterNode> Nodes` bound from `ManuscriptModel.Nodes` via DynamicData `Bind()` + `Subscribe()`.
   - `ReactiveCommand<Unit, Unit> OpenWorkspaceCommand`: calls `folderPicker.PickFolderAsync()`, null-guards, calls `manuscriptService.LoadWorkspaceAsync`, on success saves path with `settingsStore.SetAsync("lastWorkspacePath", path)` and updates internal `ManuscriptModel` ref. On failure calls `notificationService.ShowError(result.Error)`.
   - `Task InitAsync()`: reads `settingsStore.GetAsync<string>("lastWorkspacePath")`; if non-null calls `manuscriptService.LoadWorkspaceAsync` and binds result.
   - Subscribe `OpenWorkspaceCommand.ThrownExceptions` → `notificationService.ShowError(ex.Message)` (per S01 ReactiveCommand pattern).
   - `_disposables` CompositeDisposable pattern for DynamicData subscription cleanup.
3. Create `src/Hymnal/Views/SidebarView.axaml`:
   - Root is a `UserControl`.
   - Contains a `ListBox` bound to `{Binding Nodes}` with `ItemTemplate` showing an icon + title:
     - If `Kind == NodeKind.Part`: bold text style via `FontWeight=SemiBold`
     - If `Kind == NodeKind.Chapter`: normal weight, slight indent via `Margin="12,0,0,0"`
     - If `IsMissing`: append ` ⚠` to displayed title
   - Use a `DataTemplate` with a `StackPanel` (Horizontal) containing a `TextBlock`.
   - Apply `SynthwavePurpleBrush` for Part text color via `DynamicResource`.
4. Create `src/Hymnal/Views/SidebarView.axaml.cs`: standard Avalonia `InitializeComponent()` code-behind, no extra logic.

Done when: `dotnet build src/Hymnal/ --no-incremental` exits 0.

## Inputs

- `src/Hymnal.Core/Interfaces/IFolderPickerService.cs`
- `src/Hymnal.Core/Interfaces/IAppSettingsStore.cs`
- `src/Hymnal.Core/Interfaces/INotificationService.cs`
- `src/Hymnal.Core/Models/ChapterNode.cs`
- `src/Hymnal.Core/Models/ManuscriptModel.cs`
- `src/Hymnal.Core/Services/ManuscriptService.cs`
- `src/Hymnal/ViewModels/ViewModelBase.cs`
- `src/Hymnal/Themes/SynthwaveTheme.axaml`

## Expected Output

- `src/Hymnal/Infrastructure/FolderPickerService.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/Views/SidebarView.axaml`
- `src/Hymnal/Views/SidebarView.axaml.cs`

## Verification

dotnet build src/Hymnal/ --no-incremental
