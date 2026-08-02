---
estimated_steps: 51
estimated_files: 4
skills_used: []
---

# T02: ChapterInfoView and right-rail MainWindow refactor

**Why:** ChapterInfoView is the visible F3 pane. MainWindow.axaml must be refactored so the right rail hosts both Chapter Info and Notes as independently toggleable stacked sections with a row GridSplitter when both are visible.

**Steps:**
1. Create `src/Hymnal/Views/ChapterInfoView.axaml` (UserControl, DataContext=ChapterInfoViewModel):
   - Background matching SurfaceElevatedBrush
   - Empty state: `TextBlock` 'Open a chapter to view its info.' visible when `ChapterTitle` is null
   - Content panel: visible when ChapterTitle is not null
     - **Status ComboBox**: `ItemsSource` from an `ObjectConverters.IsNotNull`-guarded source or a static list of `ChapterStatus` values. Use a custom `DataTemplate` showing a coloured circle (Ellipse, Stroke matching synthwave status colours from `StatusToBrushConverter`) plus the enum name as `TextBlock`. Bind SelectedItem TwoWay to `Status`; on selection change execute `SetStatusCommand`.
     - **Phase Start DatePicker** (or TextBox fallback): attempt Avalonia DatePicker with a `StringToDateTimeOffsetConverter` (implement this converter inline in ChapterInfoView.axaml.cs as a nested class, or in `src/Hymnal/Views/Converters/`). Bind `SelectedDate` TwoWay to `PhaseStartDate` via converter. If DatePicker binding to string proves problematic at build time, fall back to a `TextBox` with placeholder 'YYYY-MM-DD' bound directly to `PhaseStartDate`. Label: 'Phase Start'.
     - **Phase End DatePicker / TextBox**: same approach. Label: 'Phase End'.
     - **Save Dates Button**: 'Apply' button; Command=`{Binding SaveDatesCommand}`. Only show when either date field is non-empty.
     - **Word Count label**: `TextBlock` bound to `WordCountDisplay`; label 'Words'.
     - **Target field**: `NumericUpDown` or `TextBox` (integer) bound to a new `PendingTarget` (int?) property on ChapterInfoViewModel; 'Set Target' button executes `SetTargetCommand` with PendingTarget value; 'Clear' button executes `SetTargetCommand` with null.
     - **Proximity bar**: `ProgressBar` Value=`ProximityFill` Minimum=0 Maximum=1, visible when HasTarget.
     - **Pre-fill toggle**: `CheckBox` Content='Auto-fill today on status change' IsChecked=`{Binding PrefillPhaseDate, Mode=TwoWay}`; wire TwoWay binding so setting the checkbox triggers AppSettingsStore save (use `WhenAnyValue(x => x.PrefillPhaseDate).Skip(1).Subscribe(v => ...)` in ChapterInfoViewModel).
   - Apply synthwave palette: use DynamicResource brushes; keep typography consistent with NotesView (FontSize 12–13, OnSurfaceDimBrush labels, SynthwavePurpleBrush accents).
2. Create `src/Hymnal/Views/ChapterInfoView.axaml.cs`: minimal code-behind (InitializeComponent only); any converters needed for DatePicker may be declared as nested classes here.
3. Modify `src/Hymnal/Views/MainWindow.axaml` — right-rail refactor:
   - The outer 4-column Grid already has: Col0=Sidebar, Col1=Editor, Col2=GridSplitter, Col3=RightPane.
   - **Column splitter (Col2)**: change IsVisible binding from `NotesViewModel.IsVisible` to `IsAnyRightPaneOpen`.
   - **Right pane (Col3)** — replace the existing Border+Panel with the new shared-host structure:
     ```
     <Border Col3 Background=SurfaceElevated BorderLeft>
       <Panel>
         <!-- COLLAPSED: 48px icon strip — visible when !IsAnyRightPaneOpen -->
         <StackPanel Width=48 IsVisible={!IsAnyRightPaneOpen}>
           <Button F3 icon (chapter info) Command=ChapterInfoViewModel.ToggleCommand />
           <Button F4 icon (notes) Command=NotesViewModel.ToggleCommand />
         </StackPanel>
         <!-- EXPANDED: shared 280px host — visible when IsAnyRightPaneOpen -->
         <Grid Width=280 IsVisible={IsAnyRightPaneOpen} RowDefinitions="*,Auto,*">
           <!-- Row 0: Chapter Info section -->
           <Grid Row=0 RowDefinitions="48,*" IsVisible={ChapterInfoViewModel.IsVisible}>
             <Border Row=0 header with F3 icon + "CHAPTER INFO" label + close button />
             <views:ChapterInfoView Row=1 DataContext={ChapterInfoViewModel} />
           </Grid>
           <!-- Row 1: Row GridSplitter between panes -->
           <GridSplitter Row=1 Height=4 ResizeDirection=Rows IsVisible={IsBothRightPanesOpen} />
           <!-- Row 2: Notes section -->
           <Grid Row=2 RowDefinitions="48,*" IsVisible={NotesViewModel.IsVisible}>
             <Border Row=0 header with F4 icon + "NOTES" label (existing pattern) />
             <views:NotesView Row=1 DataContext={NotesViewModel} />
           </Grid>
         </Grid>
       </Panel>
     </Border>
     ```
   - **Row height when one pane is hidden**: use a `BoolToGridLengthConverter` (new converter in `src/Hymnal/Views/Converters/`) that converts `bool` → `GridLength(1, Star)` when true and `GridLength(0)` when false. Bind `RowDefinition Height` for Row 0 to `ChapterInfoViewModel.IsVisible` and Row 2 to `NotesViewModel.IsVisible` via this converter. This collapses hidden sections without consuming space.
   - **F3 KeyBinding** in `Window.KeyBindings`: `<KeyBinding Gesture="F3" Command="{Binding ChapterInfoViewModel.ToggleCommand}" />`
   - **View menu item**: `<MenuItem Header="Toggle _Chapter Info" Command="{Binding ChapterInfoViewModel.ToggleCommand}" InputGesture="F3" />` before the Toggle Notes item.
4. Preserve all existing Notes (F4) behaviour: IsVisible restore on chapter switch, debounce save, ToggleCommand gate — these are in NotesViewModel and require no changes.

**Done when:** `dotnet build src/Hymnal/Hymnal.csproj -nologo` exits 0.

## Inputs

- `src/Hymnal/ViewModels/ChapterInfoViewModel.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal/ViewModels/ChapterViewModel.cs`
- `src/Hymnal/Views/MainWindow.axaml`
- `src/Hymnal/Views/NotesView.axaml`
- `src/Hymnal/Views/NotesView.axaml.cs`
- `src/Hymnal/Views/Converters/StatusToBrushConverter.cs`
- `src/Hymnal/Views/Converters/NodeKindConverters.cs`
- `src/Hymnal.Core/Models/ChapterStatus.cs`

## Expected Output

- `src/Hymnal/Views/ChapterInfoView.axaml`
- `src/Hymnal/Views/ChapterInfoView.axaml.cs`
- `src/Hymnal/Views/MainWindow.axaml`
- `src/Hymnal/Views/Converters/NodeKindConverters.cs`

## Verification

dotnet build src/Hymnal/Hymnal.csproj -nologo
