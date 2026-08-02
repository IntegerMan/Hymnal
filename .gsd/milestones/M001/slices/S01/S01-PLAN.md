# S01: Solution Scaffold and Synthwave Theme

**Goal:** Bootstrap the Hymnal solution from scratch: scaffold the two-project layout (Hymnal UI + Hymnal.Core), establish the compile-enforced Core boundary with Result/Unit/interfaces, apply the full synthwave palette and font embedding, wire MSDI DI with NotificationService, and confirm the app launches to a dark windowed shell with purple accents.
**Demo:** dotnet build succeeds; app launches to a dark windowed shell with purple accents and correct Inter/JetBrains Mono fonts

## Must-Haves

- dotnet build Hymnal.sln succeeds; dotnet test passes; app launches to a dark window with SurfaceBaseBrush background and SynthwavePurpleBrush accents; Hymnal.Core has zero Avalonia package references; Inter and JetBrains Mono fonts render in the shell; CI release.yml matrix exists.

## Proof Level

- This slice proves: integration — visual launch confirmation + passing unit tests + clean build required

## Integration Closure

Upstream: nothing (first slice). Produces: AppBuilder host, SynthwaveTheme.axaml named brush resources, INotificationService/NotificationService, Result&lt;T&gt;/Unit, ViewLocator, all interface stubs. Downstream S02 consumes the DI container and notification service directly.

## Verification

- INotificationService ShowError/ShowInfo/ShowSuccess wired to a MainWindow overlay — any downstream failure that calls the notification service will produce a visible banner. Build failures surface as dotnet build exit code != 0.

## Tasks

- [x] **T01: Scaffolded Hymnal.slnx with Avalonia 12 + ReactiveUI on net10.0 — dotnet build exits 0** `est:45m`
  Why: The research identified Avalonia 12 + .NET 10 compatibility as the only confirmed risk. We must verify dotnet new avalonia.mvvm supports net10.0 before creating any files. If it fails the fallback is net8.0, documented in a decision. Do: (1) Run dotnet new install Avalonia.Templates to get the latest templates. (2) Run dotnet new avalonia.mvvm --dry-run --framework net10.0 in a temp location to confirm the TFM is accepted. (3) Scaffold: dotnet new avalonia.mvvm -o src/Hymnal --framework net10.0 inside the worktree root; rename the namespace from Hymnal to Hymnal in all generated files. (4) Create src/Hymnal.Core/ as a classlib: dotnet new classlib -o src/Hymnal.Core --framework net10.0 --no-restore. (5) Create tests/Hymnal.Core.Tests/ as an xunit project: dotnet new xunit -o tests/Hymnal.Core.Tests --framework net10.0 --no-restore. (6) Create Hymnal.sln and add all three projects: dotnet new sln -n Hymnal && dotnet sln add src/Hymnal/Hymnal.csproj src/Hymnal.Core/Hymnal.Core.csproj tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj. (7) Add project references: Hymnal -> Hymnal.Core, Hymnal.Core.Tests -> Hymnal.Core. (8) Add required NuGet packages to src/Hymnal/Hymnal.csproj: Avalonia, Avalonia.ReactiveUI, Avalonia.Desktop, AvaloniaEdit, DynamicData, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Hosting. Add to src/Hymnal.Core/Hymnal.Core.csproj: DynamicData, System.Reactive, Microsoft.Extensions.DependencyInjection.Abstractions. Add to tests/Hymnal.Core.Tests/: NSubstitute. (9) Run dotnet restore Hymnal.sln. Done when: dotnet build Hymnal.sln --no-incremental exits 0.
  - Files: `Hymnal.sln`, `src/Hymnal/Hymnal.csproj`, `src/Hymnal/Program.cs`, `src/Hymnal/App.axaml`, `src/Hymnal/App.axaml.cs`, `src/Hymnal/ViewLocator.cs`, `src/Hymnal.Core/Hymnal.Core.csproj`, `tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj`
  - Verify: dotnet build Hymnal.sln --no-incremental

- [x] **T02: Established Hymnal.Core boundary with Result&lt;T&gt;, Unit, three service interfaces, and 4 passing xUnit tests — no Avalonia reference in Core** `est:30m`
  Why: Hymnal.Core is the zero-UI contract layer that all downstream slices depend on. Establishing it now with compile-enforced boundaries (no Avalonia reference) lets S02-S04 executor agents trust the interface contracts exist. Do: (1) Delete the template-generated Class1.cs from src/Hymnal.Core/. (2) Create src/Hymnal.Core/Common/Result.cs — readonly record struct Result<T>(T? Value, string? Error, bool IsSuccess) with static Ok(T value) and Fail(string error) factory methods. (3) Create src/Hymnal.Core/Common/Unit.cs — readonly struct Unit with static readonly Unit Default singleton. (4) Create src/Hymnal.Core/Interfaces/INotificationService.cs — interface with ShowError(string message), ShowInfo(string message), ShowSuccess(string message). (5) Create src/Hymnal.Core/Interfaces/ICredentialStore.cs — interface stub: Task StoreAsync(string key, string value), Task<string?> RetrieveAsync(string key), Task DeleteAsync(string key). (6) Create src/Hymnal.Core/Interfaces/IAppSettingsStore.cs — interface stub: Task<T?> GetAsync<T>(string key), Task SetAsync<T>(string key, T value) (implemented in S02). (7) Delete the template-generated UnitTest1.cs from tests/Hymnal.Core.Tests/. (8) Create tests/Hymnal.Core.Tests/Common/ResultTests.cs — xUnit tests covering: Result<int>.Ok(42).IsSuccess == true, Result<int>.Ok(42).Value == 42, Result<int>.Fail("err").IsSuccess == false, Result<int>.Fail("err").Error == "err". (9) Create fixture directories: tests/Hymnal.Core.Tests/Fixtures/SampleManuscripts/simple-book/ and multi-part-book/ (both with placeholder Book.txt — one line each: "chapter-one.md" and "part-one/chapter-one.md"). (10) Verify Hymnal.Core has no Avalonia packages: dotnet list src/Hymnal.Core/ package must show no Avalonia entries. Done when: dotnet test tests/Hymnal.Core.Tests/ exits 0 and all ResultTests pass.
  - Files: `src/Hymnal.Core/Common/Result.cs`, `src/Hymnal.Core/Common/Unit.cs`, `src/Hymnal.Core/Interfaces/INotificationService.cs`, `src/Hymnal.Core/Interfaces/ICredentialStore.cs`, `src/Hymnal.Core/Interfaces/IAppSettingsStore.cs`, `tests/Hymnal.Core.Tests/Common/ResultTests.cs`, `tests/Hymnal.Core.Tests/Fixtures/SampleManuscripts/simple-book/Book.txt`, `tests/Hymnal.Core.Tests/Fixtures/SampleManuscripts/multi-part-book/Book.txt`
  - Verify: dotnet test tests/Hymnal.Core.Tests/ --no-build

- [x] **T03: Embedded Inter (Regular/Medium/SemiBold) and JetBrains Mono Regular fonts as AvaloniaResource; created SynthwaveTheme.axaml with 19 named brushes and 2 font family resources, ControlStyles.axaml with 5 ControlTheme overrides, and Icons.axaml stub; merged all three into App.axaml — dotnet build exits 0** `est:45m`
  Why: The visual identity of Hymnal is defined by the synthwave palette from DESIGN.md. All downstream UI work depends on the named brush resources existing in App.axaml. Fonts must be embedded as AvaloniaResource so the FontManager resolves them via avares:// URI. Do: (1) Download Inter font TTF files (Regular, Medium, SemiBold) from the embedded copy or note they must be placed at src/Hymnal/Assets/Fonts/Inter-Regular.ttf, Inter-Medium.ttf, Inter-SemiBold.ttf. Download JetBrains Mono Regular TTF to src/Hymnal/Assets/Fonts/JetBrainsMono-Regular.ttf. Declare all four as <AvaloniaResource Include="Assets/Fonts/*.ttf" /> in Hymnal.csproj. (2) Create src/Hymnal/Themes/SynthwaveTheme.axaml as ResourceDictionary with all 19 named SolidColorBrush resources: SurfaceBaseBrush=#0D0B14, SurfaceElevatedBrush=#141020, SurfaceOverlayBrush=#1C1729, SurfaceHighBrush=#241F35, BorderSubtleBrush=#2D2540, BorderDefaultBrush=#3D3560, OnSurfaceBrush=#EDE8F5, OnSurfaceDimBrush=#9589B0, OnSurfaceMutedBrush=#574E70, SynthwavePurpleBrush=#9D4EDD, PrimaryBrightBrush=#B469F0, PinkBrush=#E91E8C, YellowBrush=#F5C842, OrangeBrush=#FF6B35, CyanBrush=#38BDF8, ErrorBrush=#FF4D4F, SuccessBrush=#22D3A0, InfoBrush=#38BDF8. Also declare FontFamily resources: InterFont=avares://Hymnal/Assets/Fonts/Inter#Inter, EditorFont=avares://Hymnal/Assets/Fonts/JetBrainsMono#JetBrains Mono. (3) Create src/Hymnal/Themes/ControlStyles.axaml as ResourceDictionary with ControlTheme overrides for Button (background=SurfaceElevatedBrush, foreground=OnSurfaceBrush, border=BorderDefaultBrush, hover background=SurfaceHighBrush), TextBox (background=SurfaceBaseBrush, foreground=OnSurfaceBrush, border=BorderDefaultBrush, caret=SynthwavePurpleBrush, selection=SynthwavePurpleBrush at 40% opacity), TreeView (background=SurfaceElevatedBrush, selected item=SurfaceOverlayBrush, selected indicator=SynthwavePurpleBrush), ScrollBar (track=SurfaceElevatedBrush, thumb=BorderDefaultBrush). Use ControlTheme TargetType syntax (Avalonia 12.0 — not old Style TargetType). (4) Create src/Hymnal/Themes/Icons.axaml as an empty ResourceDictionary stub. (5) Update src/Hymnal/App.axaml to merge SynthwaveTheme.axaml, ControlStyles.axaml, and Icons.axaml into Application.Resources via ResourceDictionary.MergedDictionaries. Done when: dotnet build Hymnal.sln --no-incremental exits 0 and the theme files exist with correct brush keys.
  - Files: `src/Hymnal/Hymnal.csproj`, `src/Hymnal/App.axaml`, `src/Hymnal/Themes/SynthwaveTheme.axaml`, `src/Hymnal/Themes/ControlStyles.axaml`, `src/Hymnal/Themes/Icons.axaml`, `src/Hymnal/Assets/Fonts/Inter-Regular.ttf`, `src/Hymnal/Assets/Fonts/Inter-Medium.ttf`, `src/Hymnal/Assets/Fonts/Inter-SemiBold.ttf`, `src/Hymnal/Assets/Fonts/JetBrainsMono-Regular.ttf`
  - Verify: dotnet build Hymnal.sln --no-incremental

- [x] **T04: Wired MSDI DI container, implemented NotificationService with IObservable&lt;Notification&gt;, added CredentialStoreStub, and finalised MainWindow shell with synthwave theme (dark background, purple top bar) — dotnet build exits 0, 4 tests pass** `est:45m`
  Why: The DI container and NotificationService are the integration contracts promised to S02. MainWindow must demonstrate the synthwave theme is live — dark background, purple accent — before S01 is closed. Do: (1) Create src/Hymnal/ViewModels/ViewModelBase.cs — inherits ReactiveObject; exposes protected CompositeDisposable Disposables; documents ReactiveCommand.ThrownExceptions subscription convention in a comment. (2) Create src/Hymnal/ViewModels/MainWindowViewModel.cs — inherits ViewModelBase; placeholder [Reactive] string Title = "Hymnal"; placeholder CurrentView object property (null in S01). (3) Verify ViewLocator.cs (template-generated) replaces "ViewModel" with "View" in the fully-qualified type name. If the template locator does not handle sub-namespaces correctly, update the Build method to strip trailing "ViewModel" and append "View", resolving from the current assembly. (4) Create src/Hymnal/Infrastructure/NotificationService.cs — implements INotificationService from Hymnal.Core; ShowError/ShowInfo/ShowSuccess each post a notification object (type: error/info/success, message) to an internal Subject<Notification>; expose IObservable<Notification> Notifications property so MainWindow can subscribe. Define a local Notification record in the same file (record Notification(NotificationKind Kind, string Message)). (5) Update src/Hymnal/App.axaml.cs — in OnFrameworkInitializationCompleted: build ServiceCollection, register NotificationService as INotificationService (singleton), register MainWindowViewModel (transient), call BuildServiceProvider, resolve MainWindowViewModel, assign as DataContext of new MainWindow. Add platform stub for ICredentialStore: on Windows register a CredentialStoreStub (in-memory Dictionary), on Linux the same stub — real implementation deferred to a future milestone. (6) Update src/Hymnal/Views/MainWindow.axaml — set Background={StaticResource SurfaceBaseBrush}, Width=1280, Height=800, Title="Hymnal", TransparencyLevelHint=None; add a thin top border with Background={StaticResource SynthwavePurpleBrush} Height=2 to confirm accent colour is live; set FontFamily={StaticResource InterFont} on the Window. (7) Update src/Hymnal/Views/MainWindow.axaml.cs — minimal code-behind; subscribe to NotificationService.Notifications in constructor for future banner support (log to Debug for now). Done when: dotnet build Hymnal.sln --no-incremental exits 0 and dotnet run --project src/Hymnal/ opens a dark window (dark background, purple top bar, no crash).
  - Files: `src/Hymnal/ViewModels/ViewModelBase.cs`, `src/Hymnal/ViewModels/MainWindowViewModel.cs`, `src/Hymnal/ViewLocator.cs`, `src/Hymnal/Infrastructure/NotificationService.cs`, `src/Hymnal/App.axaml.cs`, `src/Hymnal/Views/MainWindow.axaml`, `src/Hymnal/Views/MainWindow.axaml.cs`
  - Verify: dotnet build Hymnal.sln --no-incremental

- [x] **T05: Created .github/workflows/release.yml (win-x64 + linux-x64 matrix publish on v* tags) and ci.yml (ubuntu-only build+test on main/milestone branches and PRs)** `est:20m`
  Why: R014 requires self-contained win-x64 and linux-x64 publish targets. The CI workflow validates the build matrix on every tag push so regressions are caught before release. Do: (1) Create .github/workflows/release.yml. Trigger: on push tags matching v*. Matrix: os=[windows-latest, ubuntu-latest], rid=[win-x64, linux-x64]. Steps per matrix job: (a) actions/checkout@v4, (b) actions/setup-dotnet@v4 with dotnet-version: 10.x, (c) dotnet restore Hymnal.sln, (d) dotnet build Hymnal.sln --configuration Release --no-restore, (e) dotnet test tests/Hymnal.Core.Tests/ --configuration Release --no-build --no-restore, (f) dotnet publish src/Hymnal/Hymnal.csproj --configuration Release --runtime ${{ matrix.rid }} --self-contained true --output publish/${{ matrix.rid }}. IMPORTANT: do NOT add a dotnet run step — headless CI has no display server; the app cannot launch in CI. (2) Add a .github/workflows/ci.yml for PR/branch validation (triggers: push branches [main, milestone/*], pull_request): same steps as release.yml minus the publish step; matrix can be simplified to ubuntu-latest only for speed. Done when: .github/workflows/release.yml and ci.yml exist and are valid YAML (dotnet build Hymnal.sln exits 0 is the proxy since we cannot run GH Actions locally).
  - Files: `.github/workflows/release.yml`, `.github/workflows/ci.yml`
  - Verify: dotnet build Hymnal.sln --no-incremental

## Files Likely Touched

- Hymnal.sln
- src/Hymnal/Hymnal.csproj
- src/Hymnal/Program.cs
- src/Hymnal/App.axaml
- src/Hymnal/App.axaml.cs
- src/Hymnal/ViewLocator.cs
- src/Hymnal.Core/Hymnal.Core.csproj
- tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj
- src/Hymnal.Core/Common/Result.cs
- src/Hymnal.Core/Common/Unit.cs
- src/Hymnal.Core/Interfaces/INotificationService.cs
- src/Hymnal.Core/Interfaces/ICredentialStore.cs
- src/Hymnal.Core/Interfaces/IAppSettingsStore.cs
- tests/Hymnal.Core.Tests/Common/ResultTests.cs
- tests/Hymnal.Core.Tests/Fixtures/SampleManuscripts/simple-book/Book.txt
- tests/Hymnal.Core.Tests/Fixtures/SampleManuscripts/multi-part-book/Book.txt
- src/Hymnal/Themes/SynthwaveTheme.axaml
- src/Hymnal/Themes/ControlStyles.axaml
- src/Hymnal/Themes/Icons.axaml
- src/Hymnal/Assets/Fonts/Inter-Regular.ttf
- src/Hymnal/Assets/Fonts/Inter-Medium.ttf
- src/Hymnal/Assets/Fonts/Inter-SemiBold.ttf
- src/Hymnal/Assets/Fonts/JetBrainsMono-Regular.ttf
- src/Hymnal/ViewModels/ViewModelBase.cs
- src/Hymnal/ViewModels/MainWindowViewModel.cs
- src/Hymnal/Infrastructure/NotificationService.cs
- src/Hymnal/Views/MainWindow.axaml
- src/Hymnal/Views/MainWindow.axaml.cs
- .github/workflows/release.yml
- .github/workflows/ci.yml
