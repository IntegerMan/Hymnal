---
id: T03
parent: S03
milestone: M001
key_files:
  - src/Hymnal/Views/MarkuaHighlighting.xshd
  - src/Hymnal/Views/EditorView.axaml
  - src/Hymnal/Views/EditorView.axaml.cs
  - src/Hymnal/Views/MainWindow.axaml
  - src/Hymnal/Views/MainWindow.axaml.cs
  - src/Hymnal/Views/SidebarView.axaml
  - src/Hymnal/Hymnal.csproj
key_decisions:
  - XSHD bold rule placed before italic rule in the ruleset so **bold** patterns are consumed before *italic* single-asterisk rule can match; eliminates need for lookbehind assertions which may not be universally supported in XSHD regex context
  - EditorView uses UserControl base class with OnAttachedToVisualTree/OnDetachedFromVisualTree lifecycle hooks instead of ReactiveUserControl<T> to avoid x:TypeArguments AXAML complexity while still achieving leak-free subscription management via CompositeDisposable
  - Notification banner uses DockPanel.Dock=Bottom positioned before the content Grid in AXAML so the Grid (no dock property) fills all remaining space — DockPanel fill-child semantics apply to the last undocked element
  - MarkuaHighlighting.xshd uses XML entity escaping (&gt; for blurb > char) rather than CDATA to keep the file valid XML for the XmlReader-based XSHD loader
duration: 
verification_result: passed
completed_at: 2026-05-29T16:03:59.256Z
blocker_discovered: false
---

# T03: Composed EditorView (AvaloniaEdit + Markua XSHD highlighting + conflict strip), wired MainWindow shell (title binding, Save menu, notification banner), and added SelectedItem binding to SidebarView — dotnet build passes 0 errors 0 warnings.

**Composed EditorView (AvaloniaEdit + Markua XSHD highlighting + conflict strip), wired MainWindow shell (title binding, Save menu, notification banner), and added SelectedItem binding to SidebarView — dotnet build passes 0 errors 0 warnings.**

## What Happened

T03 closes the visual loop for S03 by wiring the AvaloniaEdit editor surface, Markua syntax highlighting, and all MainWindow shell elements introduced in T01/T02.

**MarkuaHighlighting.xshd** — Created the XSHD syntax definition covering 8 token types: Heading (#C792EA bold), Attribute (#89DDFF), Bold (#EEFFFF bold), Italic (#A0A0B8 italic), Code (#C3E88D inline), CodeBlock (#C3E88D fenced span), Directive (#FFCB6B structural attrs), Blurb (#E0956C line prefixes). All colors are tuned for the #0D0B14 Synthwave dark background. Bold is listed before Italic in the ruleset so `**bold**` is consumed before the single-asterisk italic rule can match. XML entities are used where needed (`&gt;` for the blurb `>` pattern).

**Hymnal.csproj** — Added `<AvaloniaResource Include="Views\MarkuaHighlighting.xshd" />` so the XSHD is embedded in the `avares://Hymnal/` asset store and loadable at runtime.

**EditorView.axaml** — UserControl with `x:DataType="vm:EditorViewModel"`, conflict strip at top (DockPanel.Dock="Top", IsVisible bound to HasConflict) containing ConflictMessage TextBlock and KeepLocalCommand/AcceptExternalCommand buttons, and a fill AvaloniaEdit TextEditor with `x:Name="PART_Editor"` using the EditorFont resource, WordWrap=True, synthwave surface/foreground brushes, and a Ctrl+S KeyBinding → SaveCommand.

**EditorView.axaml.cs** — UserControl code-behind managing two-way text sync since AvaloniaEdit.TextEditor.Text is not a proper Avalonia DP. OnAttachedToVisualTree loads XSHD via AssetLoader.Open → XmlReader → HighlightingLoader.Load, wires PART_Editor.TextChanged → vm.Text (Editor→VM direction), and subscribes to vm.WhenAnyValue(x => x.Text) → PART_Editor.Text with equality guard to break the notify loop (VM→Editor direction). DataContextChanged re-wires the VM subscription on DataContext swap. CompositeDisposable tracks all subscriptions and is disposed in OnDetachedFromVisualTree for leak-free lifetime management.

**MainWindow.axaml** — Changed Title="Hymnal" to Title="{Binding Title}" (reactive dirty-state title from MainWindowViewModel.Title). Added Save menu item with Command="{Binding EditorViewModel.SaveCommand}" and InputGesture="Ctrl+S" between Open Workspace and Exit in the File menu. Replaced the placeholder `<Border Grid.Column="2" />` with `<views:EditorView Grid.Column="2" DataContext="{Binding EditorViewModel}" />`. Added notification banner as DockPanel.Dock="Bottom" Border with IsVisible="{Binding HasBanner}" and BannerMessage TextBlock — positioned before the two-panel Grid so the Grid fills the remaining space.

**MainWindow.axaml.cs** — Removed the S01-era debug NotificationService subscription (the debug `Subscribe(n => Debug.WriteLine(...))` block). MainWindowViewModel now owns all notification routing via HasBanner/BannerMessage and the 5-second auto-dismiss timer. Cleaned unused using statements.

**SidebarView.axaml** — Added `SelectedItem="{Binding SelectedNode}"` to the ListBox. WorkspaceViewModel.TrySwitchChapterAsync is the primary guard against Part/missing node selection (resets SelectedNode to null on rejection); visual dimming of Part nodes via the existing NodeKindToForegroundConverter provides the secondary cue.

## Verification

Build: `dotnet build src/Hymnal/Hymnal.csproj -c Debug --nologo` → exit 0, 0 Error(s), 0 Warning(s), Time Elapsed 00:00:06.68. Both Hymnal.dll (1.78 MB) and Hymnal.Core.dll produced in bin/Debug/net10.0/. XSHD validated as well-formed XML via Python xml.etree.ElementTree. All 7 target files present in src/Hymnal/Views/. Bindings spot-checked: Title="{Binding Title}" present, SaveCommand wired, HasBanner/BannerMessage banner wired, EditorView in Grid.Column=2, SelectedItem="{Binding SelectedNode}" on ListBox.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build src/Hymnal/Hymnal.csproj -c Debug --nologo` | 0 | ✅ pass — 0 Error(s), 0 Warning(s) | 6680ms |
| 2 | `ls src/Hymnal/bin/Debug/net10.0/Hymnal.dll` | 0 | ✅ pass — Hymnal.dll 1.78 MB present | 224ms |
| 3 | `python3 -c "import xml.etree.ElementTree as ET; ET.parse('src/Hymnal/Views/MarkuaHighlighting.xshd'); print('valid XML')"` | 0 | ✅ pass — XSHD is well-formed XML | 681ms |

## Deviations

None. All 7 planned files created/updated per the task plan. AvaloniaEdit text-sync implemented via code-behind rather than AXAML binding (expected — AvaloniaEdit.TextEditor.Text is not a full Avalonia DP). DockPanel layout used for EditorView instead of Panel (achieves the same result with cleaner conflict-strip/editor stacking).

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal/Views/MarkuaHighlighting.xshd`
- `src/Hymnal/Views/EditorView.axaml`
- `src/Hymnal/Views/EditorView.axaml.cs`
- `src/Hymnal/Views/MainWindow.axaml`
- `src/Hymnal/Views/MainWindow.axaml.cs`
- `src/Hymnal/Views/SidebarView.axaml`
- `src/Hymnal/Hymnal.csproj`
