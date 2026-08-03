---
id: S01
parent: M001
milestone: M001
provides:
  - ["Avalonia AppBuilder host with MSDI DI container", "SynthwaveTheme.axaml with 19 named brushes and 2 font family resources", "ControlStyles.axaml with 5 ControlTheme overrides", "Icons.axaml stub", "Inter Regular/Medium/SemiBold and JetBrains Mono Regular embedded fonts", "INotificationService / NotificationService with IObservable<Notification>", "CredentialStoreStub (in-memory)", "Result<T> + Unit types in Hymnal.Core/Common/", "INotificationService, ICredentialStore, IAppSettingsStore interfaces", "ViewLocator convention (XxxView/XxxViewModel)", "MainWindow shell with dark synthwave chrome", "ci.yml (ubuntu build+test on push/PR)", "release.yml (win-x64+linux-x64 matrix publish on v* tags)"]
requires:
  []
affects:
  - ["S02"]
key_files:
  - ["Hymnal.slnx", "src/Hymnal/Hymnal.csproj", "src/Hymnal.Core/Hymnal.Core.csproj", "tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj", "src/Hymnal.Core/Common/Result.cs", "src/Hymnal.Core/Common/Unit.cs", "src/Hymnal.Core/Interfaces/INotificationService.cs", "src/Hymnal.Core/Interfaces/ICredentialStore.cs", "src/Hymnal.Core/Interfaces/IAppSettingsStore.cs", "src/Hymnal/Themes/SynthwaveTheme.axaml", "src/Hymnal/Themes/ControlStyles.axaml", "src/Hymnal/Themes/Icons.axaml", "src/Hymnal/Assets/Fonts/Inter-Regular.ttf", "src/Hymnal/Assets/Fonts/Inter-Medium.ttf", "src/Hymnal/Assets/Fonts/Inter-SemiBold.ttf", "src/Hymnal/Assets/Fonts/JetBrainsMono-Regular.ttf", "src/Hymnal/Infrastructure/NotificationService.cs", "src/Hymnal/Infrastructure/CredentialStoreStub.cs", "src/Hymnal/App.axaml", "src/Hymnal/App.axaml.cs", "src/Hymnal/Views/MainWindow.axaml", "src/Hymnal/Views/MainWindow.axaml.cs", ".github/workflows/ci.yml", ".github/workflows/release.yml"]
key_decisions:
  - ["ReactiveUI.Avalonia 23.2.1 (not Avalonia.ReactiveUI which tops at 11.x); Avalonia.AvaloniaEdit 12.0.0; DynamicData 9.4.31 + System.Reactive 6.1.0 pinned to satisfy transitive version floors", "Result<T> as readonly record struct (not class) for value semantics; Unit as readonly struct with static Default singleton", "Hymnal.Core csproj has zero Avalonia package references — compile boundary, not convention", "Avalonia 12 ControlTheme uses x:Key={x:Type ...} implicit-style syntax (not deprecated TargetType attribute form)", "SelectionBrush as literal #669D4EDD — AXAML ResourceDictionary does not support opacity modifiers on DynamicResource references", "App.axaml RequestedThemeVariant=Dark (changed from Default to force dark surface brushes on Windows)", "NotificationService registered under both concrete type and INotificationService so MainWindow and services each resolve what they need", "Subscribe(Action<T>, Action<Exception>) overload to avoid CS1660 lambda-to-IObserver ambiguity in code-behind", "ci.yml ubuntu-only (no matrix) to minimise CI minutes; release.yml win-x64+linux-x64 matrix on v* tags only", "No dotnet run in CI workflows — Avalonia desktop requires display server unavailable in headless GitHub Actions"]
patterns_established:
  - ["MSDI DI container wired in App.axaml.cs AppBuilder chain; services resolved via GetRequiredService in Program.cs/App", "ViewLocator convention: XxxView pairs with XxxViewModel by name suffix", "Result<T> value-typed error propagation — all async service methods return Task<Result<T>>; ViewModels surface failures via INotificationService.ShowError()", "Atomic write pattern (write-temp-then-rename) established for future metadata stores", "Theme resources in separate AXAML files merged into App.axaml ResourceDictionary — SynthwaveTheme.axaml, ControlStyles.axaml, Icons.axaml"]
observability_surfaces:
  - none
drill_down_paths:
  []
duration: ""
verification_result: passed
completed_at: 2026-05-28T20:20:39.850Z
blocker_discovered: false
---

# S01: Solution Scaffold and Synthwave Theme

**Avalonia 12 + ReactiveUI solution on net10.0 with embedded Inter/JetBrains Mono fonts, 19-brush synthwave dark theme, MSDI DI container, NotificationService, and GitHub Actions CI/release workflows — dotnet build exits 0, 4 tests pass**

## What Happened

Five tasks built the complete runnable scaffold from nothing to a themed, DI-wired Avalonia desktop app with CI coverage.

**T01** established the solution skeleton: `Hymnal.slnx` (dotnet 10 default format) with three projects — `Hymnal` (Avalonia 12 UI), `Hymnal.Core` (pure .NET 10), and `Hymnal.Core.Tests` (xUnit). Key package decisions: ReactiveUI.Avalonia 23.2.1 (not the defunct Avalonia.ReactiveUI), Avalonia.AvaloniaEdit 12.0.0, DynamicData 9.4.31 and System.Reactive 6.1.0 pinned to satisfy ReactiveUI transitive floors. Both `dotnet build` and `dotnet test` exited 0.

**T02** drew the Core boundary: `Result<T>` as a readonly record struct (value semantics, stack-allocated), `Unit` as readonly struct with `Default` singleton, and three service interfaces (`INotificationService`, `ICredentialStore`, `IAppSettingsStore`). The Hymnal.Core csproj carries zero Avalonia package references — the compile boundary is enforced by the project file, not convention. Four xUnit tests covering `Result<T>` pass.

**T03** embedded fonts and theme resources. Inter Regular/Medium/SemiBold and JetBrains Mono Regular are embedded as AvaloniaResource under `Assets/Fonts/` — the existing `Assets\**` glob in the csproj covers them without modification. `SynthwaveTheme.axaml` defines 19 named brushes (dark surfaces, purple primary, yellow/pink/orange accents) and 2 font family resources. `ControlStyles.axaml` provides 5 ControlTheme overrides using the Avalonia 12 `x:Key={x:Type ...}` implicit-style convention. `App.axaml` has `RequestedThemeVariant="Dark"` (changed from Default to force the dark surface brushes rather than letting Windows light theme override them). All three resource dictionaries merged into `App.axaml`.

**T04** wired the runtime: `App.axaml.cs` builds an MSDI `ServiceCollection`, registers `NotificationService` under both its concrete type and `INotificationService` (so `MainWindow` can subscribe to `NotificationService.Notifications` directly while services resolve the interface), adds `CredentialStoreStub` (in-memory; real platform credential store deferred), and resolves `MainWindowViewModel` from DI. `MainWindow.axaml` renders the dark synthwave shell — near-black background (#1A1625), purple top bar (#6C3FF5), white title text. `Subscribe(Action<T>, Action<Exception>)` overload avoids CS1660 ambiguity. Build exits 0, 4 tests pass.

**T05** added GitHub Actions: `ci.yml` runs on push to `main`/`milestone/*` and PRs — ubuntu-latest only, no matrix, to minimise CI minutes; `release.yml` triggers on `v*` tags with win-x64 + linux-x64 matrix, builds self-contained publish artifacts, and uploads each as a named `upload-artifact@v4` step. No `dotnet run` step in either workflow — Avalonia desktop requires a display server unavailable in headless CI. Both files are structurally valid (no tabs, correct name/on/jobs keys).

## Verification

**T01:** `dotnet build Hymnal.slnx --no-incremental` exited 0 with 0 errors, 0 warnings. All three projects compiled. Solution and csproj files confirmed present.

**T02:** `dotnet build src/Hymnal.Core/` exited 0, no warnings. `dotnet test tests/Hymnal.Core.Tests/` exited 0, 4/4 tests passed. `dotnet list src/Hymnal.Core/ package` confirmed zero Avalonia packages.

**T03:** `dotnet build Hymnal.sln --no-incremental` exited 0, 0 warnings, 0 errors (~10.68s). All 4 font TTFs present at `src/Hymnal/Assets/Fonts/`. All 3 theme AXAML files present at `src/Hymnal/Themes/`.

**T04:** `dotnet build Hymnal.sln --no-incremental` exited 0, 0 warnings, 0 errors. `dotnet test tests/Hymnal.Core.Tests/` exited 0, 4 passed, 0 failed.

**T05:** `dotnet build Hymnal.sln --no-incremental` exited 0, 0 errors. Node-based YAML structural check on both workflow files — no tabs, name/on/jobs keys present.

**Closure verification:** All `obj/` generated files confirmed present for Hymnal.Core, Hymnal, and Hymnal.Core.Tests (proving a successful build ran during task sessions). Both YAML workflows validated: ci.yml (32 lines, ubuntu-only, push+branch triggers), release.yml (46 lines, v* tag trigger, win-x64+linux-x64 matrix). Note: `gsd_exec` cannot re-run `dotnet build` in the closure environment — Windows `dotnet.exe` invoked through WSL bash fails NuGet path resolution. Task-session obj/ artifacts and SUMMARY verification fields are the authoritative build evidence.

## Requirements Advanced

- R011 — SynthwaveTheme.axaml defines 19 named brushes (dark surfaces, purple primary #6C3FF5, yellow #F5C842, pink #E040FB, orange #FF6D00 accents); ControlStyles.axaml applies them via 5 ControlTheme overrides; Inter UI font and JetBrains Mono editor font embedded and registered
- R014 — Solution targets net10.0, self-contained publish configured in release.yml for win-x64 and linux-x64; CI baseline build confirmed on ubuntu-latest

## Requirements Validated

None.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

["Solution file is Hymnal.slnx (dotnet 10 default) in addition to Hymnal.sln — both present", "App.axaml RequestedThemeVariant changed from Default to Dark (logically required; all brushes are dark-surface colors)", "upload-artifact@v4 step added to release.yml (not in task plan but standard practice — artifacts would otherwise be silently discarded)", "T03 task plan mentioned Hymnal.sln but T01 created Hymnal.slnx; T03 verification used Hymnal.sln (which also exists and resolves the same projects)"]

## Known Limitations

["CredentialStoreStub is in-memory only — real platform credential store (Windows Credential Manager / libsecret) deferred to a future milestone", "GitHub Actions workflows have not been exercised end-to-end; YAML structural validity confirmed but runtime correctness requires a GitHub remote with Actions enabled", "Icons.axaml is a stub with no icon resources yet — actual icons deferred to later slices", "MainWindow shell has purple top bar and dark background but no sidebar, editor, or content panels — those are S02–S04 scope", "gsd_exec cannot run dotnet build in the WSL environment (Windows dotnet.exe NuGet path resolution fails through WSL bash mount)"]

## Follow-ups

["S02 should add a nuget.config if package restore issues appear in CI (feeds may need explicit configuration)", "Real CredentialStore implementation (Windows Credential Manager + Linux Secret Service) should be tracked for a future milestone", "Icons.axaml stub should be populated when icon assets are designed/sourced"]

## Files Created/Modified

- `Hymnal.slnx` — Solution file (dotnet 10 format) referencing all three projects
- `Hymnal.sln` — Classic solution file (also present, references same projects)
- `src/Hymnal/Hymnal.csproj` — Avalonia 12 UI project — ReactiveUI.Avalonia, Avalonia.AvaloniaEdit, DI packages
- `src/Hymnal.Core/Hymnal.Core.csproj` — Pure .NET 10 Core project — zero Avalonia references
- `tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj` — xUnit test project referencing Core only
- `src/Hymnal.Core/Common/Result.cs` — Result<T> readonly record struct for value-typed error propagation
- `src/Hymnal.Core/Common/Unit.cs` — Unit readonly struct with Default singleton
- `src/Hymnal.Core/Interfaces/INotificationService.cs` — Banner notification service interface
- `src/Hymnal.Core/Interfaces/ICredentialStore.cs` — Credential storage interface
- `src/Hymnal.Core/Interfaces/IAppSettingsStore.cs` — App settings persistence interface
- `tests/Hymnal.Core.Tests/Common/ResultTests.cs` — 4 xUnit tests for Result<T>
- `src/Hymnal/Assets/Fonts/Inter-Regular.ttf` — Inter Regular font — embedded AvaloniaResource
- `src/Hymnal/Assets/Fonts/Inter-Medium.ttf` — Inter Medium font — embedded AvaloniaResource
- `src/Hymnal/Assets/Fonts/Inter-SemiBold.ttf` — Inter SemiBold font — embedded AvaloniaResource
- `src/Hymnal/Assets/Fonts/JetBrainsMono-Regular.ttf` — JetBrains Mono Regular font — embedded AvaloniaResource
- `src/Hymnal/Themes/SynthwaveTheme.axaml` — 19 named brush resources + 2 font family resources
- `src/Hymnal/Themes/ControlStyles.axaml` — 5 ControlTheme overrides applying synthwave brushes
- `src/Hymnal/Themes/Icons.axaml` — Icons resource dictionary stub (no icons yet)
- `src/Hymnal/App.axaml` — Merges SynthwaveTheme, ControlStyles, Icons; RequestedThemeVariant=Dark
- `src/Hymnal/App.axaml.cs` — MSDI ServiceCollection wiring, DI container built and used for ViewModel resolution
- `src/Hymnal/Program.cs` — AppBuilder entry point
- `src/Hymnal/ViewLocator.cs` — XxxView/XxxViewModel naming convention locator
- `src/Hymnal/ViewModels/ViewModelBase.cs` — ReactiveObject base class
- `src/Hymnal/ViewModels/MainWindowViewModel.cs` — Main window view model
- `src/Hymnal/Views/MainWindow.axaml` — Shell layout — dark background, purple top bar, title
- `src/Hymnal/Views/MainWindow.axaml.cs` — Code-behind subscribing to NotificationService.Notifications
- `src/Hymnal/Infrastructure/NotificationService.cs` — INotificationService implementation with IObservable<Notification> subject
- `src/Hymnal/Infrastructure/CredentialStoreStub.cs` — In-memory ICredentialStore stub
- `.github/workflows/ci.yml` — PR/branch CI: ubuntu-latest build+test on push to main/milestone/* and PRs
- `.github/workflows/release.yml` — Release: win-x64+linux-x64 self-contained publish on v* tag push with artifact upload
