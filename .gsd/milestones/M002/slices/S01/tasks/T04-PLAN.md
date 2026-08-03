---
estimated_steps: 44
estimated_files: 4
skills_used: []
---

# T04: WorkspaceViewModel refactor, sidebar status dot, and inline status flyout

**Why:** The sidebar is the primary author surface for status visibility and changes. WorkspaceViewModel must wire up registry reconciliation and phase-data hydration at workspace load. SidebarView DataTemplate must switch to ChapterViewModel and render the coloured dot + flyout.

**Do:**

### 1. WorkspaceViewModel refactor (`src/Hymnal/ViewModels/WorkspaceViewModel.cs`)
- Add constructor parameters: `ChapterRegistryService registryService`, `PhaseDataService phaseDataService`.
- Change `_nodes` from `ObservableCollectionExtended<ChapterNode>` to `ObservableCollectionExtended<ChapterViewModel>` and `Nodes` from `ReadOnlyObservableCollection<ChapterNode>` to `ReadOnlyObservableCollection<ChapterViewModel>`.
- Change `_selectedNode` and `SelectedNode` from `ChapterNode?` to `ChapterViewModel?`.
- In `TrySwitchChapterAsync`: accept `ChapterViewModel` parameter; use `viewModel.Node` wherever a `ChapterNode` was used. Update `_editor.OpenChapterAsync(viewModel.Node, absolutePath)`.
- In `WhenAnyValue(x => x.SelectedNode)` subscription: adapt to `ChapterViewModel?` type; call `TrySwitchChapterAsync(node)` with the ChapterViewModel.
- In `BindModel(ManuscriptModel model)`: after the model is stored, call `LoadRegistryAndPhaseDataAsync(model)` (fire-and-forget via `_ = LoadRegistryAndPhaseDataAsync(model)`).
- Add `private async Task LoadRegistryAndPhaseDataAsync(ManuscriptModel model)`: load registry, reconcile orphans (all active paths from model.Nodes), load phases, build `ChapterViewModel` for each `ChapterNode` (in `model.Nodes.Items` order by Index), populate `_nodes`. For missing-file nodes, still create ChapterViewModel with the status from phases (or Outlining default). On exception, call `_notificationService.ShowError`.
- In `InitAsync` restore-last-chapter loop: iterate `_nodes` (ChapterViewModel) instead of `_model.Nodes.Items`; compare `vm.Node.RelativePath` against lastChapterPath using PathHelper.IsSamePath; assign `SelectedNode = vm` (ChapterViewModel).
- In `CloseWorkspaceAsync`: dispose all ChapterViewModels in `_nodes` before clearing (`foreach (var vm in _nodes) vm.Dispose()`).
- Register `ChapterRegistryService` and `PhaseDataService` in the DI container (`src/Hymnal/App.axaml.cs` or wherever services are registered — find the DI registration file and add both as transient or singleton as appropriate).

### 2. StatusToBrushConverter (`src/Hymnal/Views/Converters/StatusToBrushConverter.cs`)
- `IValueConverter` (Avalonia) that accepts `ChapterStatus` and returns a brush key string for `Application.Current.Resources`.
- Mapping: Outlining→`OnSurfaceDimBrush`, Drafting→`CyanBrush`, Editing→`SynthwavePurpleBrush`, Polishing→`YellowBrush`, Reviewing→`PinkBrush`, Done→`SuccessBrush`.
- Return `Application.Current!.Resources[brushKey]` (IBrush). Fallback: return `Brushes.Transparent`.

### 3. SidebarView update (`src/Hymnal/Views/SidebarView.axaml`)
- Add `xmlns:vm="using:Hymnal.ViewModels"` (already present), add `conv:StatusToBrushConverter x:Key="StatusToBrushConverter"`.
- `ListBox.ItemsSource=" {Binding Nodes}"`, `SelectedItem="{Binding SelectedNode}"` — unchanged.
- Change `DataTemplate DataType="models:ChapterNode"` → `DataTemplate DataType="vm:ChapterViewModel"`.
- Inside the DataTemplate StackPanel, add before the TextBlock: an `Ellipse` for the status dot:
  ```xml
  <Ellipse Width="8" Height="8" Margin="0,0,6,0" VerticalAlignment="Center"
           IsVisible="{Binding Node.Kind, Converter=...}"  <!-- visible only for Chapter nodes -->
           Opacity="{Binding Node.IsMissing, Converter=...}"  <!-- 0.35 if missing, else 1.0 -->
           Cursor="{Binding Node.IsMissing, Converter=...}"  <!-- Arrow if missing -->
           Fill="{Binding Status, Converter={StaticResource StatusToBrushConverter}}">
    <Ellipse.GestureRecognizers>
      <!-- click handled via Button wrapper instead for simplicity -->
    </Ellipse.GestureRecognizers>
  </Ellipse>
  ```
  Wrap the Ellipse in a `Button` with `Background=Transparent`, `BorderThickness=0`, `Padding=0`, `Command="{Binding ChangeStatusCommand}"` — BUT since ChangeStatusCommand takes a ChapterStatus parameter, use a Popup approach instead: wrap the dot Ellipse in a `Button` that opens a `Popup` (IsLightDismissEnabled=true) via a `bool` property `IsStatusFlyoutOpen` on ChapterViewModel.
  
  **Simpler approach (preferred):** Add `public bool IsStatusFlyoutOpen { get; set; }` reactive property to ChapterViewModel. In SidebarView, render the dot as a `Button` (transparent) containing the Ellipse. The Button's `Click` (code-behind handler) sets `vm.IsStatusFlyoutOpen = true`. Inside the Button, attach a `Flyout` (Avalonia FlyoutBase via `Button.Flyout`) with a `StackPanel` listing 6 `Button` items (one per status), each with a coloured dot + label. Each status button's Command is `{Binding ChangeStatusCommand}` with CommandParameter set to the ChapterStatus enum value (use converters or MenuItems). 
  
  **Concrete implementation:** Use a `MenuFlyout` on the dot Button with 6 `MenuItem` entries. Each MenuItem: `Header="Drafting"`, `Command="{Binding ChangeStatusCommand}"`, `CommandParameter="{x:Static models:ChapterStatus.Drafting}"`. This avoids custom Popup management. MenuItem icon is a small Ellipse showing the status colour.
  
- Update existing bindings in the DataTemplate to use `Node.Title`, `Node.Kind`, `Node.IsMissing` (via `Node.` prefix) since DataType is now ChapterViewModel.
- For missing-file chapter dot: bind `Opacity` to a converter `BoolToOpacityConverter` (add to NodeKindConverters.cs or StatusToBrushConverter.cs) that returns 0.35 for true, 1.0 for false. Bind `IsHitTestVisible="{Binding !Node.IsMissing}"` on the dot Button to prevent flyout on missing-file chapters.

### 4. Find and update DI registration
- Read `src/Hymnal/App.axaml.cs` (or equivalent DI registration file) to find where services are wired up. Add registrations for `ChapterRegistryService` and `PhaseDataService`. Add `WorkspaceViewModel` constructor parameter wiring.

**Done when:** `dotnet build src/Hymnal/Hymnal.csproj` exits 0 with no errors.

## Inputs

- `src/Hymnal/ViewModels/ChapterViewModel.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/Views/SidebarView.axaml`
- `src/Hymnal/Views/Converters/NodeKindConverters.cs`
- `src/Hymnal.Core/Services/ChapterRegistryService.cs`
- `src/Hymnal.Core/Services/PhaseDataService.cs`
- `src/Hymnal.Core/Models/ChapterStatus.cs`
- `src/Hymnal.Core/Infrastructure/AppSettingsStore.cs`

## Expected Output

- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/Views/Converters/StatusToBrushConverter.cs`
- `src/Hymnal/Views/SidebarView.axaml`
- `src/Hymnal/App.axaml.cs`

## Verification

dotnet build src/Hymnal/Hymnal.csproj
