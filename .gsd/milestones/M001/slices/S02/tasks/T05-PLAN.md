---
estimated_steps: 24
estimated_files: 3
skills_used: []
---

# T05: Wired DI registrations (AppSettingsStore, ManuscriptService, WorkspaceViewModel, FolderPickerService), updated MainWindowViewModel with WorkspaceViewModel injection, and replaced MainWindow placeholder with two-panel Grid hosting SidebarView

Why: Everything built in T01–T04 is dead code until it is wired into the running application via DI and the MainWindow layout is updated to host the sidebar.

Do:
1. Update `src/Hymnal/App.axaml.cs`:
   - Register `AppSettingsStore` as `IAppSettingsStore` (singleton).
   - Register `ManuscriptService` as singleton.
   - Register `WorkspaceViewModel` as singleton.
   - Register `FolderPickerService` as `IFolderPickerService` singleton. Because `FolderPickerService` needs a `Func<TopLevel?>`, register it with a factory lambda: `sp => new FolderPickerService(() => App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime dl ? TopLevel.GetTopLevel(dl.MainWindow) : null)`.
   - Add `using Avalonia.Controls;` and `using Avalonia.Platform.Storage;` as needed.
2. Update `src/Hymnal/ViewModels/MainWindowViewModel.cs`:
   - Add `WorkspaceViewModel WorkspaceViewModel { get; }` property.
   - Constructor injection: takes `WorkspaceViewModel workspaceViewModel`.
   - Call `_ = workspaceViewModel.InitAsync()` in constructor body (fire-and-forget; errors surfaced via NotificationService).
3. Update `src/Hymnal/Views/MainWindow.axaml`:
   - Replace the placeholder `TextBlock` content area with a two-column `Grid` (sidebar | editor placeholder):
     ```xml
     <Grid ColumnDefinitions="240,*">
       <views:SidebarView Grid.Column="0" DataContext="{Binding WorkspaceViewModel}" />
       <Border Grid.Column="1" Background="{DynamicResource SynthwaveBackgroundBrush}" />
     </Grid>
     ```
   - Add `xmlns:views="clr-namespace:Hymnal.Views"` if not present.
   - Preserve the existing purple top bar `Border`; the new Grid goes in the content row below it.
4. Ensure `src/Hymnal/Views/MainWindow.axaml.cs` still compiles (no changes required unless namespace corrections needed).

Done when: `dotnet build Hymnal.sln --no-incremental` exits 0 and `dotnet test tests/Hymnal.Core.Tests/` exits 0 with all tests passing.

## Inputs

- `src/Hymnal/App.axaml.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal/Views/MainWindow.axaml`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/Views/SidebarView.axaml`
- `src/Hymnal/Infrastructure/AppSettingsStore.cs`
- `src/Hymnal/Infrastructure/FolderPickerService.cs`
- `src/Hymnal.Core/Services/ManuscriptService.cs`

## Expected Output

- `src/Hymnal/App.axaml.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal/Views/MainWindow.axaml`

## Verification

dotnet build Hymnal.sln --no-incremental && dotnet test tests/Hymnal.Core.Tests/
