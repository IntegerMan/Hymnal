---
estimated_steps: 33
estimated_files: 7
skills_used: []
---

# T03: Composed EditorView (AvaloniaEdit + Markua XSHD highlighting + conflict strip), wired MainWindow shell (title binding, Save menu, notification banner), and added SelectedItem binding to SidebarView — dotnet build passes 0 errors 0 warnings.

Why: T02 delivers the behavioral core in view-model land. T03 closes the visual loop: replace the placeholder editor pane with a real AvaloniaEdit surface, load Markua syntax rules, bind the window title, activate the notification banner, wire Ctrl+S and File>Save, and update SidebarView to activate chapter selection with guard rails on Part/missing nodes.

Do:
1. Create `src/Hymnal/Views/MarkuaHighlighting.xshd` using standard AvaloniaEdit XSHD format (XmlDoc with SyntaxDefinition root, RuleSet, Keywords, Span, Rule elements). Cover these token groups:
   - Headings: `^#{1,6}\s.*` (line rule) — Color name "Heading", bright purple/lavender (#C792EA or synthwave heading color)
   - Attribute lists: `^\{[^}]*\}$` — Color "Attribute", cyan (#89DDFF)
   - Inline bold: `\*\*[^*]+\*\*` — Color "Bold", slightly brighter than default foreground
   - Inline italic: `(?<!\*)\*(?!\*)[^*]+\*(?!\*)` — Color "Italic", muted
   - Inline code: `` `[^`]+` `` — Color "Code", green (#C3E88D)
   - Fenced code block: Span from `^```.*$` to `^```$` — Color "CodeBlock"
   - Markua directives (standalone attribute lines starting with known keywords): `^\{(?:mainmatter|backmatter|frontmatter|sample|pagebreak|blurb)` — Color "Directive", gold/yellow (#FFCB6B)
   - Blurb line prefixes: `^[ABDEIQTWXC]>` — Color "Blurb", muted orange
   Use hex colors compatible with the dark synthwave theme background (approximately #1A1625).
2. Update `src/Hymnal/Hymnal.csproj`: add `<AvaloniaResource Include="Views/MarkuaHighlighting.xshd" />` inside the existing AvaloniaResource ItemGroup.
3. Create `src/Hymnal/Views/EditorView.axaml`:
   - Root `UserControl` with `x:DataType="vm:EditorViewModel"`, `xmlns:avedit="using:AvaloniaEdit"` namespace
   - Panel layout:
     (a) Conflict strip at top: `<Border IsVisible="{Binding HasConflict}" Background="{DynamicResource SurfaceHighBrush}" Padding="8,6">` containing a horizontal StackPanel with `<TextBlock Text="{Binding ConflictMessage}" />` and two Buttons: `<Button Content="Keep my edits" Command="{Binding KeepLocalCommand}" />` and `<Button Content="Reload from disk" Command="{Binding AcceptExternalCommand}" />`
     (b) AvaloniaEdit TextEditor with `x:Name="PART_Editor"`, `FontFamily="{DynamicResource JetBrainsMono}"` (or JetBrains Mono font family key), `FontSize="14"`, `WordWrap="True"`, `ShowLineNumbers="False"`, `Background="{DynamicResource SurfaceBaseBrush}"`, `Foreground="{DynamicResource OnSurfaceBrush}"`
   - Keyboard shortcut: `<UserControl.KeyBindings><KeyBinding Gesture="Ctrl+S" Command="{Binding SaveCommand}" /></UserControl.KeyBindings>`
4. Create `src/Hymnal/Views/EditorView.axaml.cs`:
   - In constructor (or `OnAttachedToVisualTree`): find `PART_Editor`, load XSHD via `Avalonia.Platform.AssetLoader.Open(new Uri("avares://Hymnal/Views/MarkuaHighlighting.xshd"))`, wrap in `System.Xml.XmlReader.Create(stream)`, pass to `AvaloniaEdit.Highlighting.HighlightingLoader.Load(reader, AvaloniaEdit.Highlighting.HighlightingManager.Instance)`, assign to `PART_Editor.SyntaxHighlighting`
   - Two-way text sync: (a) subscribe to `PART_Editor.TextChanged` event → update ViewModel.Text only if ViewModel exists; (b) in `WhenActivated`, observe `this.WhenAnyValue(x => x.DataContext).WhereNotNull()` then observe `vm.WhenAnyValue(x => x.Text)` → if PART_Editor.Text != new value, set `PART_Editor.Text = value` (handles external reload and initial load)
   - Use `ReactiveUI.WhenActivated` for observable subscriptions to properly manage lifetime
5. Update `src/Hymnal/Views/MainWindow.axaml`:
   - Change `Title="Hymnal"` to `Title="{Binding Title}"`
   - Add `<MenuItem Header="_Save" Command="{Binding EditorViewModel.SaveCommand}" InputGesture="Ctrl+S" />` immediately before the Separator in the File menu
   - Replace `<Border Grid.Column="2" Background="{DynamicResource SurfaceBaseBrush}" />` with `<views:EditorView Grid.Column="2" DataContext="{Binding EditorViewModel}" />`
   - Add notification banner: before the closing `</DockPanel>` tag, add `<Border DockPanel.Dock="Bottom" IsVisible="{Binding HasBanner}" Padding="12,8" Background="{DynamicResource SurfaceElevatedBrush}"><TextBlock Text="{Binding BannerMessage}" Foreground="{DynamicResource OnSurfaceBrush}" /></Border>`
6. Update `src/Hymnal/Views/MainWindow.axaml.cs`: remove the NotificationService debug-log subscription entirely (MainWindowViewModel now handles banner state). Keep the partial class structure.
7. Update `src/Hymnal/Views/SidebarView.axaml`:
   - Add `SelectedItem="{Binding SelectedNode}"` to the `<ListBox>` element
   - Add a style for disabled Part/missing items: add a Style targeting `ListBoxItem` that sets `IsEnabled="False"` when the item's Kind is Part or IsMissing — since compiled bindings require explicit patterns, use a value converter approach or rely on WorkspaceViewModel.TrySwitchChapterAsync rejecting Part/missing nodes (the VM guard is the primary defense; visual dimming via the existing muted foreground on Part nodes is sufficient for the first pass)

Done when: All files created/updated, XSHD registered in csproj, EditorView compiles, MainWindow compiles with title binding and Save menu and EditorView and banner, SidebarView has SelectedItem binding. `dotnet build src/Hymnal/Hymnal.csproj` passes.

## Inputs

- `src/Hymnal/ViewModels/EditorViewModel.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/Views/MainWindow.axaml`
- `src/Hymnal/Views/SidebarView.axaml`
- `src/Hymnal/Hymnal.csproj`

## Expected Output

- `src/Hymnal/Views/MarkuaHighlighting.xshd`
- `src/Hymnal/Views/EditorView.axaml`
- `src/Hymnal/Views/EditorView.axaml.cs`
- `src/Hymnal/Views/MainWindow.axaml`
- `src/Hymnal/Views/MainWindow.axaml.cs`
- `src/Hymnal/Views/SidebarView.axaml`
- `src/Hymnal/Hymnal.csproj`

## Verification

dotnet build src/Hymnal/Hymnal.csproj

## Observability Impact

Notification banner in MainWindow is now rendered from HasBanner/BannerMessage on MainWindowViewModel. Title bar reflects dirty state. Conflict strip in EditorView exposes HasConflict state visually. These surfaces together let a debugging agent check app state without attaching a debugger.
