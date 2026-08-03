---
estimated_steps: 1
estimated_files: 8
skills_used: []
---

# T03: Wire Git toolbar and commit dialog into MainWindow

Executor skills_used frontmatter: tdd, verify-before-complete. Why: the slice demo requires a user-facing toolbar group and commit dialog, not just service/ViewModel contracts. Do: register `IProcessRunner`, `IGitService`, and `GitPanelViewModel` in `App.axaml.cs` while preserving the existing ordering rule that `EditorViewModel` is created before `WorkspaceViewModel`; inject `GitPanelViewModel` into `MainWindowViewModel` and expose it as a property. Add a top-right Git toolbar group in `MainWindow.axaml` that is hidden when `GitPanelViewModel.IsVisible` is false, displays branch and uncommitted count when visible, and offers a Commit button. Add a small `GitCommitDialog` view/code-behind (or an equivalent Avalonia dialog surface) with a message TextBox prefilled from `GitPanelViewModel.CreateDefaultCommitMessage()`, plus Commit only, Commit & Push, and Cancel actions. Keep dialog orchestration in `MainWindow.axaml.cs` or view code-behind so ViewModels remain testable; on Commit only or Commit & Push, call the `GitPanelViewModel` operation and let it handle notifications and refresh. Do not add a second notification system or bypass `INotificationService`. Done when MainWindow ViewModel and smoke tests confirm DI construction, toolbar hidden/visible binding, branch/count text binding, default dialog message, and both dialog actions reaching the ViewModel.

## Inputs

- `src/Hymnal/App.axaml.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal/Views/MainWindow.axaml`
- `src/Hymnal/Views/MainWindow.axaml.cs`
- `src/Hymnal/ViewModels/GitPanelViewModel.cs`
- `tests/Hymnal.Core.Tests/Views/SupplementalDocsViewSmokeTests.cs`

## Expected Output

- `src/Hymnal/App.axaml.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal/Views/MainWindow.axaml`
- `src/Hymnal/Views/MainWindow.axaml.cs`
- `src/Hymnal/Views/GitCommitDialog.axaml`
- `src/Hymnal/Views/GitCommitDialog.axaml.cs`
- `tests/Hymnal.Core.Tests/ViewModels/MainWindowGitPanelTests.cs`
- `tests/Hymnal.Core.Tests/Views/MainWindowGitPanelSmokeTests.cs`

## Verification

dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter FullyQualifiedName~MainWindowGitPanel

## Observability Impact

Surfaces service/ViewModel diagnostics to the author through visible branch/count state and the existing toast/banner notification path. Keeps dialog action routing inspectable in smoke tests.
