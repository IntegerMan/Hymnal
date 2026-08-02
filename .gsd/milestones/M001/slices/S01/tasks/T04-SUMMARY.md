---
id: T04
parent: S01
milestone: M001
key_files:
  - src/Hymnal/Infrastructure/NotificationService.cs
  - src/Hymnal/Infrastructure/CredentialStoreStub.cs
  - src/Hymnal/App.axaml.cs
  - src/Hymnal/Views/MainWindow.axaml
  - src/Hymnal/Views/MainWindow.axaml.cs
key_decisions:
  - NotificationService registered as singleton under both concrete type and INotificationService so MainWindow can resolve NotificationService directly for the Notifications observable while services resolve INotificationService
  - CredentialStoreStub is an in-memory Dictionary — real platform credential store deferred to a future milestone as documented in the task plan
  - Used System overload Subscribe(Action&lt;T&gt;, Action&lt;Exception&gt;) in MainWindow.axaml.cs to avoid CS1660 (lambda-to-IObserver ambiguity)
duration: 
verification_result: passed
completed_at: 2026-05-28T20:05:25.398Z
blocker_discovered: false
---

# T04: Wired MSDI DI container, implemented NotificationService with IObservable&lt;Notification&gt;, added CredentialStoreStub, and finalised MainWindow shell with synthwave theme (dark background, purple top bar) — dotnet build exits 0, 4 tests pass

**Wired MSDI DI container, implemented NotificationService with IObservable&lt;Notification&gt;, added CredentialStoreStub, and finalised MainWindow shell with synthwave theme (dark background, purple top bar) — dotnet build exits 0, 4 tests pass**

## What Happened

ViewModelBase and MainWindowViewModel were already scaffolded from a prior step. Created src/Hymnal/Infrastructure/NotificationService.cs implementing INotificationService via a Subject&lt;Notification&gt;, exposing IObservable&lt;Notification&gt; Notifications; each Show* method also writes to Debug. Created CredentialStoreStub as an in-memory ICredentialStore (real impl deferred). Updated App.axaml.cs to build a ServiceCollection, register NotificationService (singleton) as both its concrete type and INotificationService, register ICredentialStore as CredentialStoreStub, register MainWindowViewModel (transient), then resolve the VM and assign it as MainWindow.DataContext. Updated MainWindow.axaml with Background=SurfaceBaseBrush, FontFamily=InterFont, TransparencyLevelHint=None, Width=1280 Height=800, and a 2px purple top border (SynthwavePurpleBrush) confirming the accent colour is live. Updated MainWindow.axaml.cs to subscribe to NotificationService.Notifications in the constructor, logging to Debug. Fixed a CS1660 compile error (missing System namespace for Action overload of Subscribe) on first build attempt.

## Verification

dotnet build Hymnal.sln --no-incremental: exit 0, 0 warnings, 0 errors. dotnet test tests/Hymnal.Core.Tests/: 4 passed, 0 failed.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build Hymnal.sln --no-incremental` | 0 | pass | 10220ms |
| 2 | `dotnet test tests/Hymnal.Core.Tests/ --no-build` | 0 | pass — 4 passed, 0 failed | 3000ms |

## Deviations

none

## Known Issues

none

## Files Created/Modified

- `src/Hymnal/Infrastructure/NotificationService.cs`
- `src/Hymnal/Infrastructure/CredentialStoreStub.cs`
- `src/Hymnal/App.axaml.cs`
- `src/Hymnal/Views/MainWindow.axaml`
- `src/Hymnal/Views/MainWindow.axaml.cs`
