---
id: T03
parent: S04
milestone: M001
key_files:
  - src/Hymnal/Views/NotesView.axaml
  - src/Hymnal/Views/NotesView.axaml.cs
  - src/Hymnal/Views/MainWindow.axaml
key_decisions:
  - ToggleButton IsChecked uses Mode=OneWay + explicit Command binding because NotesViewModel.IsVisible has private set — avoids binding write-back error while keeping visual state in sync
  - Empty-state TextBlock uses ObjectConverters.IsNull (Avalonia built-in) to avoid a custom converter
duration: 
verification_result: passed
completed_at: 2026-05-29T20:18:37.334Z
blocker_discovered: false
---

# T03: Added NotesView AXAML/code-behind, wired 5-column MainWindow shell with GridSplitter, added toolbar ToggleButton, and F4 KeyBinding — build passes 0 errors 0 warnings

**Added NotesView AXAML/code-behind, wired 5-column MainWindow shell with GridSplitter, added toolbar ToggleButton, and F4 KeyBinding — build passes 0 errors 0 warnings**

## What Happened

Created src/Hymnal/Views/NotesView.axaml with a 2-row Grid: a header Border showing ChapterTitle and a TextBox (AcceptsReturn=True, TextWrapping=Wrap) two-way bound to Text, plus an empty-state TextBlock (visible when ChapterTitle is null via ObjectConverters.IsNull). Created the minimal NotesView.axaml.cs code-behind. Updated MainWindow.axaml: expanded the inner grid from 3 to 5 columns (240,Auto,*,Auto,280); added GridSplitter on column 3 and a Border+NotesView on column 4, both bound to NotesViewModel.IsVisible. Added ToggleButton with toolbar-icon class to the toolbar, using IsChecked=OneWay + Command=ToggleCommand (IsVisible has private set so two-way binding was avoided). Added Window.KeyBindings F4→NotesViewModel.ToggleCommand. Added ToggleButton styles (pointerover, checked, disabled) to Window.Styles.

## Verification

dotnet build src/Hymnal/Hymnal.csproj -c Debug --nologo → Build succeeded, 0 Warning(s), 0 Error(s)

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build src/Hymnal/Hymnal.csproj -c Debug --nologo` | 0 | ✅ pass | 5870ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal/Views/NotesView.axaml`
- `src/Hymnal/Views/NotesView.axaml.cs`
- `src/Hymnal/Views/MainWindow.axaml`
