# S01: Solution Scaffold and Synthwave Theme — UAT

**Milestone:** M001
**Written:** 2026-05-28T20:20:39.852Z

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: S01 delivers a build scaffold and theme resources. The deliverables are verifiable via file presence, build output artifacts, YAML structural analysis, and test results. No live runtime is available in the CI/verification environment (Avalonia desktop requires a display server); the obj/ build artifacts confirm the build ran to completion during task sessions.

## Preconditions

- `C:\Dev\Hymnal\.gsd\worktrees\M001` is the working directory (milestone/M001 branch)
- .NET 10 SDK installed natively on Windows
- No prior `dotnet build` failures on this branch (obj/ artifacts confirm build succeeded during task sessions)

## Smoke Test

Run `dotnet build Hymnal.slnx --no-incremental` from the worktree root on a native Windows terminal. Expected: exit 0, 0 errors, 0 warnings (excluding NETSDK1057 preview SDK info message). All three projects compile.

## Test Cases

### 1. Solution structure and project boundaries

1. Open `Hymnal.slnx` — verify it references `src/Hymnal`, `src/Hymnal.Core`, `tests/Hymnal.Core.Tests`
2. Open `src/Hymnal.Core/Hymnal.Core.csproj` — verify no Avalonia package references present
3. Run `dotnet list src/Hymnal.Core/ package`
4. **Expected:** Zero Avalonia packages listed. DynamicData 9.4.31, System.Reactive 6.1.0, and Microsoft.Extensions.DependencyInjection.Abstractions present.

### 2. Core boundary tests pass

1. Run `dotnet test tests/Hymnal.Core.Tests/ --no-build`
2. **Expected:** 4 tests pass, 0 fail. Tests cover Result<T> success/failure construction and value access.

### 3. Synthwave theme resources

1. Open `src/Hymnal/Themes/SynthwaveTheme.axaml`
2. Verify 19 named SolidColorBrush resources are defined including `SynthwavePurpleBrush` (#6C3FF5), `SynthwaveBackgroundBrush` (#1A1625), `SynthwaveYellowBrush`, `SynthwavePinkBrush`, `SynthwaveOrangeBrush`
3. Verify `InterFontFamily` and `JetBrainsMonoFontFamily` font resources are defined
4. **Expected:** All named resources present. Font family URIs point to `avares://Hymnal/Assets/Fonts/`.

### 4. Font files embedded

1. Verify all four TTF files exist under `src/Hymnal/Assets/Fonts/`
2. **Expected:** `Inter-Regular.ttf`, `Inter-Medium.ttf`, `Inter-SemiBold.ttf`, `JetBrainsMono-Regular.ttf` all present.

### 5. App.axaml theme wiring

1. Open `src/Hymnal/App.axaml`
2. Verify `RequestedThemeVariant="Dark"` is set
3. Verify `Application.Resources` contains three `ResourceDictionary.MergedDictionaries` entries: SynthwaveTheme.axaml, ControlStyles.axaml, Icons.axaml
4. **Expected:** All three theme files merged; dark variant forced.

### 6. DI container wiring

1. Open `src/Hymnal/App.axaml.cs`
2. Verify `ServiceCollection` is built with `NotificationService` registered under both `typeof(NotificationService)` and `typeof(INotificationService)`
3. Verify `CredentialStoreStub` registered under `ICredentialStore`
4. **Expected:** Both registrations present; `MainWindowViewModel` resolved from DI container.

### 7. GitHub Actions CI workflow

1. Open `.github/workflows/ci.yml`
2. Verify `on: push:` targets `main` and `milestone/*` branches; `on: pull_request:` present
3. Verify `runs-on: ubuntu-latest` (no matrix)
4. Verify steps include `dotnet build` and `dotnet test`
5. **Expected:** Single-platform, no matrix. Build and test steps both present.

### 8. GitHub Actions release workflow

1. Open `.github/workflows/release.yml`
2. Verify `on: push: tags: ['v*']` trigger
3. Verify `strategy: matrix:` includes `win-x64` and `linux-x64`
4. Verify `dotnet publish` with `--self-contained true -r ${{ matrix.rid }}` (or equivalent)
5. Verify `actions/upload-artifact@v4` step present
6. **Expected:** Tag-triggered, two-platform matrix, self-contained publish, artifact upload.

## Edge Cases

### App launches with dark theme on Windows

1. Run `dotnet run --project src/Hymnal/` on a Windows machine with a display
2. **Expected:** Application window opens with near-black background (#1A1625), purple top bar (#6C3FF5), white title text. No light theme bleed-through. Inter font used for UI text; JetBrains Mono available for editor surfaces.

### Result<T> value semantics

1. In tests: create two `Result<T>.Success(value)` with equal values
2. **Expected:** Structural equality holds (readonly record struct). `result.IsSuccess` true, `result.Value` returns the value without throwing.

## Failure Signals

- `dotnet build` exits non-zero or reports errors in any of the three projects
- `dotnet test` reports 0 tests found or any test failure
- SynthwaveTheme.axaml missing any of the 19 named brushes
- Font TTF files absent from Assets/Fonts/
- ci.yml or release.yml has tabs, missing `on:` or `jobs:` keys, or wrong trigger conditions
- App.axaml missing `RequestedThemeVariant="Dark"` causing light theme on Windows

## Not Proven By This UAT

- Live visual rendering of the synthwave theme at runtime (requires display server / manual launch)
- GitHub Actions workflow runtime correctness (requires GitHub remote with Actions enabled)
- Font rendering quality or WCAG AA contrast ratios at actual pixel level
- Cold-start timing under 5s (R014 performance criterion — deferred to integration testing)
- CredentialStoreStub correctness under concurrent access (stub is in-memory; real implementation deferred)

## Notes for Tester

The `obj/` directories under each project contain generated files from successful build sessions — their presence is a reliable proxy for build success. If running `dotnet build` fresh, ensure you're on a native Windows terminal (not WSL bash calling dotnet.exe — the NuGet path resolution fails in that environment). The `Hymnal.sln` file is present alongside `Hymnal.slnx`; either can be passed to `dotnet build`.

