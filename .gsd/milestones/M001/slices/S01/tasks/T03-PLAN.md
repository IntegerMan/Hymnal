---
estimated_steps: 1
estimated_files: 9
skills_used: []
---

# T03: Embedded Inter (Regular/Medium/SemiBold) and JetBrains Mono Regular fonts as AvaloniaResource; created SynthwaveTheme.axaml with 19 named brushes and 2 font family resources, ControlStyles.axaml with 5 ControlTheme overrides, and Icons.axaml stub; merged all three into App.axaml — dotnet build exits 0

Why: The visual identity of Hymnal is defined by the synthwave palette from DESIGN.md. All downstream UI work depends on the named brush resources existing in App.axaml. Fonts must be embedded as AvaloniaResource so the FontManager resolves them via avares:// URI. Do: (1) Download Inter font TTF files (Regular, Medium, SemiBold) from the embedded copy or note they must be placed at src/Hymnal/Assets/Fonts/Inter-Regular.ttf, Inter-Medium.ttf, Inter-SemiBold.ttf. Download JetBrains Mono Regular TTF to src/Hymnal/Assets/Fonts/JetBrainsMono-Regular.ttf. Declare all four as <AvaloniaResource Include="Assets/Fonts/*.ttf" /> in Hymnal.csproj. (2) Create src/Hymnal/Themes/SynthwaveTheme.axaml as ResourceDictionary with all 19 named SolidColorBrush resources: SurfaceBaseBrush=#0D0B14, SurfaceElevatedBrush=#141020, SurfaceOverlayBrush=#1C1729, SurfaceHighBrush=#241F35, BorderSubtleBrush=#2D2540, BorderDefaultBrush=#3D3560, OnSurfaceBrush=#EDE8F5, OnSurfaceDimBrush=#9589B0, OnSurfaceMutedBrush=#574E70, SynthwavePurpleBrush=#9D4EDD, PrimaryBrightBrush=#B469F0, PinkBrush=#E91E8C, YellowBrush=#F5C842, OrangeBrush=#FF6B35, CyanBrush=#38BDF8, ErrorBrush=#FF4D4F, SuccessBrush=#22D3A0, InfoBrush=#38BDF8. Also declare FontFamily resources: InterFont=avares://Hymnal/Assets/Fonts/Inter#Inter, EditorFont=avares://Hymnal/Assets/Fonts/JetBrainsMono#JetBrains Mono. (3) Create src/Hymnal/Themes/ControlStyles.axaml as ResourceDictionary with ControlTheme overrides for Button (background=SurfaceElevatedBrush, foreground=OnSurfaceBrush, border=BorderDefaultBrush, hover background=SurfaceHighBrush), TextBox (background=SurfaceBaseBrush, foreground=OnSurfaceBrush, border=BorderDefaultBrush, caret=SynthwavePurpleBrush, selection=SynthwavePurpleBrush at 40% opacity), TreeView (background=SurfaceElevatedBrush, selected item=SurfaceOverlayBrush, selected indicator=SynthwavePurpleBrush), ScrollBar (track=SurfaceElevatedBrush, thumb=BorderDefaultBrush). Use ControlTheme TargetType syntax (Avalonia 12.0 — not old Style TargetType). (4) Create src/Hymnal/Themes/Icons.axaml as an empty ResourceDictionary stub. (5) Update src/Hymnal/App.axaml to merge SynthwaveTheme.axaml, ControlStyles.axaml, and Icons.axaml into Application.Resources via ResourceDictionary.MergedDictionaries. Done when: dotnet build Hymnal.sln --no-incremental exits 0 and the theme files exist with correct brush keys.

## Inputs

- `src/Hymnal/Hymnal.csproj`
- `src/Hymnal/App.axaml`

## Expected Output

- `src/Hymnal/Themes/SynthwaveTheme.axaml`
- `src/Hymnal/Themes/ControlStyles.axaml`
- `src/Hymnal/Themes/Icons.axaml`
- `src/Hymnal/App.axaml`

## Verification

dotnet build Hymnal.sln --no-incremental
