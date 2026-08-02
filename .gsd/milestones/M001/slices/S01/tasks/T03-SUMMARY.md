---
id: T03
parent: S01
milestone: M001
key_files:
  - src/Hymnal/Assets/Fonts/Inter-Regular.ttf
  - src/Hymnal/Assets/Fonts/Inter-Medium.ttf
  - src/Hymnal/Assets/Fonts/Inter-SemiBold.ttf
  - src/Hymnal/Assets/Fonts/JetBrainsMono-Regular.ttf
  - src/Hymnal/Themes/SynthwaveTheme.axaml
  - src/Hymnal/Themes/ControlStyles.axaml
  - src/Hymnal/Themes/Icons.axaml
  - src/Hymnal/App.axaml
key_decisions:
  - Font files sourced from rsms/inter v4.0 and JetBrains/JetBrainsMono v2.304 GitHub releases — official upstream sources for TTF variants
  - csproj not modified: existing Assets\** AvaloniaResource glob already covers the Fonts subdirectory
  - ControlTheme uses x:Key={x:Type ...} syntax (Avalonia 12.0 implicit style convention) rather than deprecated Style TargetType attribute form
  - SelectionBrush for TextBox set as literal #669D4EDD (40% alpha on SynthwavePurple) since AXAML ResourceDictionary does not support opacity modifiers on DynamicResource references at this level
  - App.axaml RequestedThemeVariant changed from Default to Dark to align with synthwave dark palette intent
duration: 
verification_result: passed
completed_at: 2026-05-28T20:02:25.346Z
blocker_discovered: false
---

# T03: Embedded Inter (Regular/Medium/SemiBold) and JetBrains Mono Regular fonts as AvaloniaResource; created SynthwaveTheme.axaml with 19 named brushes and 2 font family resources, ControlStyles.axaml with 5 ControlTheme overrides, and Icons.axaml stub; merged all three into App.axaml — dotnet build exits 0

**Embedded Inter (Regular/Medium/SemiBold) and JetBrains Mono Regular fonts as AvaloniaResource; created SynthwaveTheme.axaml with 19 named brushes and 2 font family resources, ControlStyles.axaml with 5 ControlTheme overrides, and Icons.axaml stub; merged all three into App.axaml — dotnet build exits 0**

## What Happened

Downloaded Inter 4.0 TTF files (Regular, Medium, SemiBold) from the rsms/inter GitHub release and JetBrains Mono 2.304 Regular TTF from the JetBrains GitHub release. Placed all four TTFs under src/Hymnal/Assets/Fonts/. The existing csproj already declares `<AvaloniaResource Include="Assets\**" />` which covers the font files — no csproj change was needed beyond confirming coverage.

Created src/Hymnal/Themes/SynthwaveTheme.axaml as a ResourceDictionary containing all 19 SolidColorBrush resources matching the DESIGN.md palette (SurfaceBaseBrush through InfoBrush) plus two FontFamily resources: InterFont pointing to avares://Hymnal/Assets/Fonts/Inter#Inter and EditorFont pointing to avares://Hymnal/Assets/Fonts/JetBrainsMono#JetBrains Mono.

Created src/Hymnal/Themes/ControlStyles.axaml with Avalonia 12.0 ControlTheme overrides (x:Key="{x:Type ...}" syntax) for Button, TextBox, TreeView, TreeViewItem, and ScrollBar. Button uses SurfaceElevatedBrush background with SurfaceHighBrush on pointer-over. TextBox uses SurfaceBaseBrush background, SynthwavePurpleBrush caret, and #669D4EDD (40% opacity purple) selection brush. TreeView uses SurfaceElevatedBrush background. TreeViewItem selected state uses SurfaceOverlayBrush. ScrollBar thumb uses BorderDefaultBrush.

Created src/Hymnal/Themes/Icons.axaml as an empty ResourceDictionary stub for future icon resources.

Updated src/Hymnal/App.axaml to add Application.Resources with a ResourceDictionary.MergedDictionaries block that includes all three theme files via ResourceInclude with avares:// URIs. Also changed RequestedThemeVariant from "Default" to "Dark" to align with the synthwave dark theme intent.

## Verification

Ran `dotnet build Hymnal.sln --no-incremental` from the worktree root. All three projects (Hymnal.Core, Hymnal.Core.Tests, Hymnal) compiled successfully with 0 warnings and 0 errors in ~10.68s. Font files confirmed present at Assets/Fonts/ (4 TTFs). Theme files confirmed present at Themes/ (3 AXAML files).

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build Hymnal.sln --no-incremental` | 0 | pass — Build succeeded, 0 Warning(s), 0 Error(s) | 10680ms |

## Deviations

RequestedThemeVariant changed from "Default" to "Dark" in App.axaml — not in the task plan but logically required since all brush resources are dark-surface colors; using Default would apply them over a light system theme on Windows.

## Known Issues

none

## Files Created/Modified

- `src/Hymnal/Assets/Fonts/Inter-Regular.ttf`
- `src/Hymnal/Assets/Fonts/Inter-Medium.ttf`
- `src/Hymnal/Assets/Fonts/Inter-SemiBold.ttf`
- `src/Hymnal/Assets/Fonts/JetBrainsMono-Regular.ttf`
- `src/Hymnal/Themes/SynthwaveTheme.axaml`
- `src/Hymnal/Themes/ControlStyles.axaml`
- `src/Hymnal/Themes/Icons.axaml`
- `src/Hymnal/App.axaml`
