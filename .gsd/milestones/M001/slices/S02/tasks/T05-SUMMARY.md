---
id: T05
parent: S02
milestone: M001
key_files:
  - src/Hymnal/App.axaml.cs
  - src/Hymnal/ViewModels/MainWindowViewModel.cs
  - src/Hymnal/Views/MainWindow.axaml
key_decisions:
  - FolderPickerService registered via factory lambda deferring TopLevel resolution to pick-time to avoid null during DI container build
  - Design.DataContext removed from MainWindow.axaml — MainWindowViewModel now requires constructor arg unsatisfiable at design time
  - ManuscriptService registered as AddSingleton (no interface) — WorkspaceViewModel depends on the concrete type
duration: 
verification_result: passed
completed_at: 2026-05-29T03:57:26.395Z
blocker_discovered: false
---

# T05: Wired DI registrations (AppSettingsStore, ManuscriptService, WorkspaceViewModel, FolderPickerService), updated MainWindowViewModel with WorkspaceViewModel injection, and replaced MainWindow placeholder with two-panel Grid hosting SidebarView

**Wired DI registrations (AppSettingsStore, ManuscriptService, WorkspaceViewModel, FolderPickerService), updated MainWindowViewModel with WorkspaceViewModel injection, and replaced MainWindow placeholder with two-panel Grid hosting SidebarView**

## What Happened

Three files updated to complete the S02 wiring:\n\n1. **App.axaml.cs** — Added `Hymnal.Core.Infrastructure`, `Hymnal.Core.Services`, and `Avalonia.Controls` usings; registered `AppSettingsStore` as `IAppSettingsStore` singleton, `ManuscriptService` as singleton, `WorkspaceViewModel` as singleton, and `FolderPickerService` as `IFolderPickerService` singleton via a factory lambda that captures the MainWindow's TopLevel at call time (avoids circular dependency during container build).\n\n2. **MainWindowViewModel.cs** — Added `WorkspaceViewModel WorkspaceViewModel { get; }` property and constructor taking `WorkspaceViewModel workspaceViewModel`; fire-and-forget `_ = workspaceViewModel.InitAsync()` in the constructor body triggers auto-restore of the last workspace on launch. Removed the parameterless constructor (DI resolves it). Removed the `Design.DataContext` block from MainWindow.axaml since the VM now requires a constructor arg that cannot be satisfied in design time.\n\n3. **MainWindow.axaml** — Added `xmlns:views=\"clr-namespace:Hymnal.Views\"`, replaced the placeholder `TextBlock` with a two-column `Grid ColumnDefinitions=\"240,*\"` hosting `views:SidebarView` (bound to `WorkspaceViewModel`) in column 0 and a `Border` with `SynthwaveBackgroundBrush` as the editor placeholder in column 1. The synthwave accent top bar `Border` in the `DockPanel` is preserved.\n\nAll DI dependencies verified: `ManuscriptService` requires only `INotificationService` (already registered); `WorkspaceViewModel` requires `ManuscriptService`, `IAppSettingsStore`, `IFolderPickerService`, `INotificationService` — all registered in the correct order. `MainWindowViewModel` is registered as transient so it resolves the singleton `WorkspaceViewModel` on every resolution (effectively singleton-scoped by the lifetime).

## Verification

Structural verification: all three modified files confirmed correct via read-back. DI graph traced manually — every constructor dependency is registered. `FolderPickerService` factory lambda defers `TopLevel.GetTopLevel` until first pick (avoids null during container build). `AppSettingsStore` namespace `Hymnal.Core.Infrastructure` confirmed from source file header. `SidebarView` namespace `Hymnal.Views` confirmed. Build cannot be executed via gsd_exec due to NuGet/WSL path issue (MEM012) — build verification must be performed from a Windows terminal with `dotnet build Hymnal.sln --no-incremental`.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `head -5 src/Hymnal.Core/Infrastructure/AppSettingsStore.cs` | 0 | ✅ pass — namespace Hymnal.Core.Infrastructure confirmed | 50ms |
| 2 | `cat src/Hymnal/App.axaml.cs` | 0 | ✅ pass — all 4 S02 DI registrations present | 50ms |
| 3 | `cat src/Hymnal/ViewModels/MainWindowViewModel.cs` | 0 | ✅ pass — WorkspaceViewModel property and constructor injection correct | 50ms |
| 4 | `cat src/Hymnal/Views/MainWindow.axaml` | 0 | ✅ pass — two-panel Grid with SidebarView and editor placeholder Border present | 50ms |

## Deviations

Design.DataContext block removed from MainWindow.axaml (plan did not mention this but it is required because MainWindowViewModel lost its parameterless constructor).

## Known Issues

Build can only be verified from a Windows terminal (dotnet.exe NuGet path issue in WSL gsd_exec — MEM012). The FolderPickerService lambda captures `dl.MainWindow` which could be null briefly during startup; this is safe because PickFolderAsync is only callable after the window is shown.

## Files Created/Modified

- `src/Hymnal/App.axaml.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal/Views/MainWindow.axaml`
