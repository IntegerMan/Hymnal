---
estimated_steps: 1
estimated_files: 7
skills_used: []
---

# T04: Wired MSDI DI container, implemented NotificationService with IObservable&lt;Notification&gt;, added CredentialStoreStub, and finalised MainWindow shell with synthwave theme (dark background, purple top bar) — dotnet build exits 0, 4 tests pass

Why: The DI container and NotificationService are the integration contracts promised to S02. MainWindow must demonstrate the synthwave theme is live — dark background, purple accent — before S01 is closed. Do: (1) Create src/Hymnal/ViewModels/ViewModelBase.cs — inherits ReactiveObject; exposes protected CompositeDisposable Disposables; documents ReactiveCommand.ThrownExceptions subscription convention in a comment. (2) Create src/Hymnal/ViewModels/MainWindowViewModel.cs — inherits ViewModelBase; placeholder [Reactive] string Title = "Hymnal"; placeholder CurrentView object property (null in S01). (3) Verify ViewLocator.cs (template-generated) replaces "ViewModel" with "View" in the fully-qualified type name. If the template locator does not handle sub-namespaces correctly, update the Build method to strip trailing "ViewModel" and append "View", resolving from the current assembly. (4) Create src/Hymnal/Infrastructure/NotificationService.cs — implements INotificationService from Hymnal.Core; ShowError/ShowInfo/ShowSuccess each post a notification object (type: error/info/success, message) to an internal Subject<Notification>; expose IObservable<Notification> Notifications property so MainWindow can subscribe. Define a local Notification record in the same file (record Notification(NotificationKind Kind, string Message)). (5) Update src/Hymnal/App.axaml.cs — in OnFrameworkInitializationCompleted: build ServiceCollection, register NotificationService as INotificationService (singleton), register MainWindowViewModel (transient), call BuildServiceProvider, resolve MainWindowViewModel, assign as DataContext of new MainWindow. Add platform stub for ICredentialStore: on Windows register a CredentialStoreStub (in-memory Dictionary), on Linux the same stub — real implementation deferred to a future milestone. (6) Update src/Hymnal/Views/MainWindow.axaml — set Background={StaticResource SurfaceBaseBrush}, Width=1280, Height=800, Title="Hymnal", TransparencyLevelHint=None; add a thin top border with Background={StaticResource SynthwavePurpleBrush} Height=2 to confirm accent colour is live; set FontFamily={StaticResource InterFont} on the Window. (7) Update src/Hymnal/Views/MainWindow.axaml.cs — minimal code-behind; subscribe to NotificationService.Notifications in constructor for future banner support (log to Debug for now). Done when: dotnet build Hymnal.sln --no-incremental exits 0 and dotnet run --project src/Hymnal/ opens a dark window (dark background, purple top bar, no crash).

## Inputs

- `src/Hymnal.Core/Interfaces/INotificationService.cs`
- `src/Hymnal.Core/Interfaces/ICredentialStore.cs`
- `src/Hymnal/Themes/SynthwaveTheme.axaml`
- `src/Hymnal/App.axaml`
- `src/Hymnal/ViewLocator.cs`

## Expected Output

- `src/Hymnal/ViewModels/ViewModelBase.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal/ViewLocator.cs`
- `src/Hymnal/Infrastructure/NotificationService.cs`
- `src/Hymnal/App.axaml.cs`
- `src/Hymnal/Views/MainWindow.axaml`
- `src/Hymnal/Views/MainWindow.axaml.cs`

## Verification

dotnet build Hymnal.sln --no-incremental

## Observability Impact

NotificationService.Notifications observable is the primary failure-visibility channel for all downstream slices. Any slice that calls ShowError will produce a visible banner in the running app and a Debug log entry. Failures in DI wiring surface as InvalidOperationException at startup with a clear missing-registration message.
