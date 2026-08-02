---
estimated_steps: 20
estimated_files: 4
skills_used: []
---

# T03: Added NotesView AXAML/code-behind, wired 5-column MainWindow shell with GridSplitter, added toolbar ToggleButton, and F4 KeyBinding — build passes 0 errors 0 warnings

Why: The ViewModel is useless without a visible panel. This task wires NotesView into the 4-column shell, adds the toolbar toggle button and F4 shortcut, and collapses the notes column properly when hidden.

Do:
1. Create `src/Hymnal/Views/NotesView.axaml`:
   - Root `UserControl` with `DataContext` bound by ViewLocator convention
   - Layout: `DockPanel` or `Grid` with a header label showing `{Binding ChapterTitle}` and a `TextBox` (AcceptsReturn=True, TextWrapping=Wrap, multi-line, no syntax highlighting) bound two-way to `{Binding Text}` — use `UpdateSourceTrigger=PropertyChanged` so Text flows back immediately
   - Apply `SynthwaveTheme` brushes for background (match sidebar background color)
   - Empty-state: when `ChapterTitle` is null, show a placeholder message (use a `TextBlock` with `IsVisible=False` when ChapterTitle is not null)

2. Create `src/Hymnal/Views/NotesView.axaml.cs` (minimal code-behind, `InitializeComponent()` only).

3. Modify `src/Hymnal/Views/MainWindow.axaml`:
   - Change inner grid at Row=3 from `ColumnDefinitions="240,Auto,*"` to `ColumnDefinitions="240,Auto,*,Auto,280"`
     - Column 0 = sidebar (240px)
     - Column 1 = sidebar/editor splitter (Auto, existing `<Border>` separator)
     - Column 2 = editor (`*`, MinWidth=200)
     - Column 3 = editor/notes splitter (Auto) — add `<GridSplitter Grid.Column="3" Width="4" ResizeDirection="Columns" />`
     - Column 4 = notes panel (280px, MinWidth=180) — add `<views:NotesView Grid.Column="4" DataContext="{Binding NotesViewModel}" />`
   - Collapse column 4 by binding its width: use a converter or bind the `Width` of a wrapping `Border` on column 4 to `NotesViewModel.IsVisible`. Simplest approach: wrap the NotesView in a `Border` and set `IsVisible` on the `Border` — BUT also set the column width to 0 when hidden to prevent dead whitespace. Do this by defining a `GridLength` converter or simply use a named ColumnDefinition and set its `Width` via a style trigger. Practical approach: add `<Grid.ColumnDefinitions>` with `Width="280"` for column 4 and `MinWidth="0"` and bind IsVisible on the containing `Border` — Avalonia collapses the space when IsVisible=false on a fixed-width column child. Verify this behavior; if not: use `Width="0"` when hidden by binding column width through a `BoolToGridLengthConverter` defined inline.
   - Add a toolbar `Notes` toggle button in the toolbar row (Grid.Row=2 StackPanel) after the existing Open button: `<ToggleButton Content="Notes" IsChecked="{Binding NotesViewModel.IsVisible}" />` styled with `toolbar-icon` class.

4. Modify `src/Hymnal/Views/MainWindow.axaml.cs`:
   - Add F4 keyboard shortcut: override `OnKeyDown` or add a `KeyBinding` in XAML. Prefer XAML `<KeyBinding Gesture="F4" Command="{Binding NotesViewModel.ToggleCommand}" />` in the Window's InputBindings.

Done when: `dotnet build src/Hymnal/Hymnal.csproj -c Debug --nologo` exits 0 with 0 errors and 0 warnings.

## Inputs

- `src/Hymnal/ViewModels/NotesViewModel.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal/Views/MainWindow.axaml`
- `src/Hymnal/Views/MainWindow.axaml.cs`
- `src/Hymnal/Views/SidebarView.axaml`

## Expected Output

- `src/Hymnal/Views/NotesView.axaml`
- `src/Hymnal/Views/NotesView.axaml.cs`

## Verification

dotnet build src/Hymnal/Hymnal.csproj -c Debug --nologo
