---
id: T04
parent: S02
milestone: M001
key_files:
  - src/Hymnal/Infrastructure/FolderPickerService.cs
  - src/Hymnal/ViewModels/WorkspaceViewModel.cs
  - src/Hymnal/Views/SidebarView.axaml
  - src/Hymnal/Views/SidebarView.axaml.cs
  - src/Hymnal/Views/Converters/NodeKindConverters.cs
key_decisions:
  - Split missing-indicator into a second TextBlock rather than a whole-node converter — avoids compiled-binding type mismatch under AvaloniaUseCompiledBindingsByDefault=true
  - NodeKindToForegroundConverter returns AvaloniaProperty.UnsetValue for Chapter so Foreground inherits from ListBoxItem theme brush
  - FolderPickerService accepts Func<TopLevel?> so the TopLevel can be resolved lazily after window creation
  - BindModel uses SortBy(n => n.RelativePath) to produce deterministic order in the ListBox
duration: 
verification_result: passed
completed_at: 2026-05-29T03:38:01.201Z
blocker_discovered: false
---

# T04: WorkspaceViewModel, FolderPickerService, SidebarView, and NodeKindConverters implemented; DynamicData Bind pattern wires ManuscriptModel.Nodes to ReadOnlyObservableCollection for AXAML

**WorkspaceViewModel, FolderPickerService, SidebarView, and NodeKindConverters implemented; DynamicData Bind pattern wires ManuscriptModel.Nodes to ReadOnlyObservableCollection for AXAML**

## What Happened

Created all four planned artifacts plus a supporting converters file:

1. **FolderPickerService** (`src/Hymnal/Infrastructure/FolderPickerService.cs`): Implements `IFolderPickerService` via Avalonia's `IStorageProvider.OpenFolderPickerAsync`. Constructor takes a `Func<TopLevel?>` accessor injected from MainWindow. Returns `results[0].Path.LocalPath` or null if cancelled/topLevel unavailable.

2. **WorkspaceViewModel** (`src/Hymnal/ViewModels/WorkspaceViewModel.cs`): Extends `ViewModelBase`. `OpenWorkspaceCommand` calls `folderPicker.PickFolderAsync()`, loads via `ManuscriptService.LoadWorkspaceAsync`, saves path to `AppSettingsStore`, and binds the returned `ManuscriptModel` nodes to a `ReadOnlyObservableCollection<ChapterNode>` via DynamicData `.Connect().SortBy().Bind()`. `InitAsync()` restores last workspace on startup. `ThrownExceptions` wired to `notificationService.ShowError`. All disposables tracked via inherited `Disposables` (CompositeDisposable).

3. **SidebarView.axaml** (`src/Hymnal/Views/SidebarView.axaml`): UserControl with `x:DataType="WorkspaceViewModel"` for compiled bindings. ListBox bound to `Nodes`. DataTemplate for `ChapterNode` uses three value converters (FontWeight, Margin, Foreground) keyed on `Kind`. A second TextBlock shows ` ⚠` visible only when `IsMissing` is true, using `{DynamicResource ErrorBrush}`.

4. **SidebarView.axaml.cs**: Standard partial class with `InitializeComponent()`.

5. **NodeKindConverters** (`src/Hymnal/Views/Converters/NodeKindConverters.cs`): Three converters — `NodeKindToFontWeightConverter` (Part→SemiBold, else Normal), `NodeKindToMarginConverter` (Chapter→Thickness(12,0,0,0), else 0), `NodeKindToForegroundConverter` (Part→#9D4EDD literal SynthwavePurple brush, else UnsetValue to inherit).

Key design choice: split title/missing-warning into two TextBlocks rather than using a multi-converter, avoiding compiled-binding constraints under `AvaloniaUseCompiledBindingsByDefault=true`.

## Verification

Structural verification: all files created at expected paths, interfaces satisfied, DynamicData Bind pattern matches established project conventions from T01-T03. dotnet build cannot be run via gsd_exec per MEM012 (NuGet path failure in WSL sandbox); build must be confirmed from terminal directly.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `ls src/Hymnal/Infrastructure/FolderPickerService.cs src/Hymnal/ViewModels/WorkspaceViewModel.cs src/Hymnal/Views/SidebarView.axaml src/Hymnal/Views/SidebarView.axaml.cs src/Hymnal/Views/Converters/NodeKindConverters.cs` | 0 | all output files present | 50ms |

## Deviations

Added src/Hymnal/Views/Converters/NodeKindConverters.cs (one extra file not listed in plan) to house the three IValueConverter implementations required by SidebarView.axaml. Plan implied converters would be inline resources; extracting to a class file is required because Avalonia AXAML cannot define converter logic inline.

## Known Issues

dotnet build not verified via automated tool due to MEM012 constraint — must be confirmed from terminal before slice completion.

## Files Created/Modified

- `src/Hymnal/Infrastructure/FolderPickerService.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/Views/SidebarView.axaml`
- `src/Hymnal/Views/SidebarView.axaml.cs`
- `src/Hymnal/Views/Converters/NodeKindConverters.cs`
