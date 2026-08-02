# S01: Solution Scaffold and Synthwave Theme — Research

**Date:** 2026-05-28
**Slice:** S01 — Solution Scaffold and Synthwave Theme
**Risk:** High (greenfield; scaffold shapes all downstream slices)

## Summary

S01 is a pure greenfield scaffold — no source files exist in the repository today. The repository contains only planning artifacts (`_bmad-output/`), research docs (`docs/research/`), GSD workflow state (`.gsd/`), and a minimal `README.md`. Every file in `src/` and `tests/` must be created from scratch.

The architecture is fully specified in `_bmad-output/planning-artifacts/architecture.md` and the UX design color palette in `_bmad-output/planning-artifacts/ux-designs/ux-Hymnal-2026-05-27/DESIGN.md`. There are no ambiguous decisions to make in S01 — the planner should treat both documents as authoritative and emit tasks that implement them verbatim.

The one confirmed risk is Avalonia 12.0 + .NET 10 compatibility. The architecture doc notes Avalonia 12 targets .NET 8+ minimum, and .NET 10 should work. The `avalonia.mvvm` template initialisation command uses `--framework net10.0`. This must be verified as the very first task.

## Recommendation

Bootstrap with `dotnet new avalonia.mvvm`, then restructure into the two-project layout (`src/Hymnal/`, `src/Hymnal.Core/`, `tests/Hymnal.Core.Tests/`). Write the synthwave theme resources, wire DI in `App.axaml.cs`, and confirm the app launches to a dark windowed shell. The slice is done when `dotnet build` succeeds and the window renders with the correct palette.

S01 must NOT attempt S02–S04 scope: no `ManuscriptService`, no `SidebarView`, no editor, no notes. The slice boundary in the roadmap is intentional — downstream slices depend on the scaffold contract, not on stub implementations of their own domain logic.

## Implementation Landscape

### Key Files

All files below must be **created** (none exist):

- `Hymnal.sln` — solution file; references the three projects
- `src/Hymnal/Hymnal.csproj` — Avalonia UI layer; references `Hymnal.Core`; packages: `Avalonia`, `Avalonia.ReactiveUI`, `Avalonia.Desktop`, `AvaloniaEdit`, `DynamicData`, `Microsoft.Extensions.DependencyInjection`, `Microsoft.Extensions.Hosting`
- `src/Hymnal/Program.cs` — AppBuilder entry point from `avalonia.mvvm` template
- `src/Hymnal/App.axaml` — merges `SynthwaveTheme.axaml` and `ControlStyles.axaml` into `Application.Resources`; references `ViewLocator`
- `src/Hymnal/App.axaml.cs` — DI registration; `INotificationService` + `ICredentialStore` (platform-conditional stub); all singletons registered here
- `src/Hymnal/ViewLocator.cs` — `IDataTemplate` convention: `XxxViewModel` → `XxxView` in same namespace
- `src/Hymnal/Views/MainWindow.axaml` — top-level shell window; dark background; uses `SynthwavePurpleBrush`; `TransparencyLevelHint` disabled for reliability
- `src/Hymnal/Views/MainWindow.axaml.cs` — code-behind; minimal
- `src/Hymnal/ViewModels/ViewModelBase.cs` — `ReactiveObject` + `CompositeDisposable Disposables`; all VMs inherit this
- `src/Hymnal/ViewModels/MainWindowViewModel.cs` — bound to `MainWindow`; placeholder `CurrentView` property
- `src/Hymnal/Themes/SynthwaveTheme.axaml` — palette `ResourceDictionary`; all named brush resources (see Color Palette below)
- `src/Hymnal/Themes/ControlStyles.axaml` — control theme overrides for `Button`, `TextBox`, `TreeView`, `ScrollBar`; uses palette brushes
- `src/Hymnal/Themes/Icons.axaml` — stub `ResourceDictionary` for vector path icons (can be empty in S01)
- `src/Hymnal/Assets/Fonts/` — embedded Inter and JetBrains Mono font files (TTF); declared as `<EmbeddedResource>` in `.csproj`
- `src/Hymnal/Infrastructure/NotificationService.cs` — implements `INotificationService` (banner overlay in `MainWindow`); ShowError/ShowInfo/ShowSuccess
- `src/Hymnal.Core/Hymnal.Core.csproj` — pure .NET 10; zero Avalonia reference; packages: `DynamicData`, `System.Reactive`, `Microsoft.Extensions.DependencyInjection.Abstractions`
- `src/Hymnal.Core/Common/Result.cs` — `readonly record struct Result<T>(T? Value, string? Error, bool IsSuccess)` with `Ok`/`Fail` statics
- `src/Hymnal.Core/Common/Unit.cs` — `readonly struct Unit` with singleton `Unit.Default`
- `src/Hymnal.Core/Interfaces/INotificationService.cs` — interface: `ShowError`, `ShowInfo`, `ShowSuccess`
- `src/Hymnal.Core/Interfaces/ICredentialStore.cs` — interface stub: `StoreAsync`, `RetrieveAsync`, `DeleteAsync` (no implementations in S01; just the interface + DI stub)
- `src/Hymnal.Core/Interfaces/IAppSettingsStore.cs` — interface stub (implemented in S02)
- `tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj` — xUnit 2.x + NSubstitute; references `Hymnal.Core`; zero Avalonia reference
- `tests/Hymnal.Core.Tests/Common/ResultTests.cs` — unit tests for `Result<T>` factory methods
- `tests/Hymnal.Core.Tests/Fixtures/SampleManuscripts/simple-book/Book.txt` — test fixture used in S02 but scaffold belongs in S01
- `tests/Hymnal.Core.Tests/Fixtures/SampleManuscripts/multi-part-book/Book.txt` — same
- `.github/workflows/release.yml` — matrix build: `[win-x64, linux-x64]`; triggers on tag `v*`; steps: restore → build → test → publish

### Color Palette (from DESIGN.md — authoritative)

Named brush resources in `SynthwaveTheme.axaml`:

| Resource Key | Hex | Usage |
|---|---|---|
| `SurfaceBaseBrush` | `#0D0B14` | Editor background, main window background |
| `SurfaceElevatedBrush` | `#141020` | Toolbar, sidebar background |
| `SurfaceOverlayBrush` | `#1C1729` | Selected items, part header background |
| `SurfaceHighBrush` | `#241F35` | Hover highlights |
| `BorderSubtleBrush` | `#2D2540` | Dividers, panel borders |
| `BorderDefaultBrush` | `#3D3560` | Control borders |
| `OnSurfaceBrush` | `#EDE8F5` | Primary text |
| `OnSurfaceDimBrush` | `#9589B0` | Secondary text, word count |
| `OnSurfaceMutedBrush` | `#574E70` | Placeholder text, section labels |
| `SynthwavePurpleBrush` | `#9D4EDD` | Primary accent, selected indicator, cursor |
| `PrimaryBrightBrush` | `#B469F0` | Hover state for primary elements |
| `PinkBrush` | `#E91E8C` | Status-reviewing, accents |
| `YellowBrush` | `#F5C842` | Status-polishing, warnings |
| `OrangeBrush` | `#FF6B35` | Accent |
| `CyanBrush` | `#38BDF8` | Status-drafting, info |
| `ErrorBrush` | `#FF4D4F` | Error banners |
| `SuccessBrush` | `#22D3A0` | Success banners, status-done |
| `InfoBrush` | `#38BDF8` | Info banners |

Typography (embed as Avalonia font resources):
- **UI font:** Inter — `13px / 400` for body; `11px / 500` for labels; `10px / 600` for caps labels; `14px / 600` for headings
- **Editor font:** JetBrains Mono — `16px / 400` for prose; `14px / 400` for code

### Build Order

1. **Verify Avalonia 12 + .NET 10** — run `dotnet new install Avalonia.Templates && dotnet new avalonia.mvvm --dry-run --framework net10.0`; if it fails, check Avalonia package version for net10 support. This is the only confirmed risk. Do not proceed until this passes.
2. **Scaffold the solution** — `dotnet new avalonia.mvvm -o Hymnal --framework net10.0`; then add `Hymnal.Core` class library and `Hymnal.Core.Tests` xUnit project; link all three into `Hymnal.sln`
3. **Establish Core boundary** — create `Hymnal.Core.csproj` with zero Avalonia reference; add `Result<T>`, `Unit`, and all interface stubs; add `ResultTests.cs`; `dotnet test` must pass
4. **Theme resources** — create `SynthwaveTheme.axaml` with all palette brushes; create `ControlStyles.axaml`; merge into `App.axaml`; embed Inter and JetBrains Mono fonts
5. **DI wiring** — implement `NotificationService`; register all singletons in `App.axaml.cs`; platform-conditional `ICredentialStore` stub
6. **MainWindow shell** — bind background to `SurfaceBaseBrush`; title "Hymnal"; verify window opens with dark background and purple accents
7. **CI workflow** — write `release.yml`; matrix `[win-x64, linux-x64]`; tag trigger

### Verification Approach

```bash
# 1. Build succeeds
dotnet build Hymnal.sln --no-incremental

# 2. Tests pass
dotnet test tests/Hymnal.Core.Tests/

# 3. App launches (visual check — window appears with dark background)
dotnet run --project src/Hymnal/

# 4. Boundary enforced — Hymnal.Core has no Avalonia reference
dotnet list src/Hymnal.Core/ package | grep -i Avalonia  # must return nothing
```

The slice is complete when all four pass and the developer can visually confirm: dark windowed shell with purple accents, correct Inter and JetBrains Mono fonts.

## Don't Hand-Roll

| Problem | Existing Solution | Why Use It |
|---|---|---|
| MVVM scaffolding + AppBuilder host | `avalonia.mvvm` template (`dotnet new install Avalonia.Templates`) | Correct DI wiring, ViewLocator, ReactiveUI plumbing — hand-rolling this is error-prone |
| Reactive command + disposal pattern | ReactiveUI `ReactiveCommand`, `CompositeDisposable` | All VMs must follow this pattern; do not build custom command types |
| Reactive collection transforms | DynamicData `SourceCache<T,K>` | Required for `ManuscriptModel.Chapters` in S02; must be a dependency in S01 Core csproj |

## Constraints

- `Hymnal.Core.csproj` must have zero Avalonia package references — compile-enforced; any import of `Avalonia.*` into Core is a build error by design
- All font files must be embedded as `<AvaloniaResource>` or `<EmbeddedResource>` — Avalonia's `FontManager` resolves embedded fonts via `avares://` URI scheme
- `JsonIgnoreCondition.WhenWritingNull` and `camelCase` properties are the serialization convention for all `.hymnal-data/` JSON; set this globally in `AppSettingsStore` registration (not per-call)
- `ReactiveCommand.ThrownExceptions` must be subscribed in every ViewModel — add this to `ViewModelBase` or document it in a convention comment

## Common Pitfalls

- **`dotnet new avalonia.mvvm` defaults to .NET 8** — always pass `--framework net10.0` explicitly or edit the `.csproj` afterward; mixing TFMs causes confusing restore errors
- **Inter font embedding** — Avalonia requires the font to be declared in `App.axaml` as `<FontFamily x:Key="InterFont">avares://Hymnal/Assets/Fonts/Inter#Inter</FontFamily>`; the `#Inter` suffix is the font family name, not the file name; omitting it causes the fallback system font to load silently
- **ControlTheme vs Style** — Avalonia 12.0 uses `ControlTheme` for control-level overrides (replacing WPF/old Avalonia `Style`); using the old `Style TargetType` pattern compiles but doesn't apply in 12.0
- **ViewLocator namespace mismatch** — the template-generated `ViewLocator` replaces `ViewModel` with `View` in the fully-qualified type name; if Views and ViewModels are in different sub-namespaces (e.g. `Views/Editor/` vs `ViewModels/Editor/`), the locator must be adjusted to handle the sub-namespace correctly
- **Linux headless CI** — Avalonia requires a display server for `dotnet run`; CI builds should only run `dotnet build` and `dotnet test`, not `dotnet run`; the `release.yml` must not attempt to launch the app in CI

## Open Risks

- Avalonia 12.0 NuGet packages for .NET 10 TFM: the architecture doc was written in May 2026 and references .NET 10 as the target. Avalonia 12.x officially targets .NET 8+; confirm the exact package version supports net10 at scaffold time. Fallback: .NET 8 TFM is acceptable if net10 fails, but net10 is strongly preferred.
- Inter and JetBrains Mono font licensing: both are OFL-licensed; they can be embedded. Download sources: Google Fonts (Inter), JetBrains GitHub (JetBrains Mono). Confirm TTF files are available before writing the csproj embed declarations.

## Sources

- Architecture specification: `_bmad-output/planning-artifacts/architecture.md` (complete directory tree, patterns, naming conventions, DI wiring)
- Color palette and typography: `_bmad-output/planning-artifacts/ux-designs/ux-Hymnal-2026-05-27/DESIGN.md` (authoritative hex values and font specs)
- Template initialization: Avalonia docs — `dotnet new install Avalonia.Templates && dotnet new avalonia.mvvm --framework net10.0`
