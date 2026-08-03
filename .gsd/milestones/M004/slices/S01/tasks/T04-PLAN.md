---
estimated_steps: 12
estimated_files: 5
skills_used: []
---

# T04: Wire Plan mode into DI, shell navigation, and full-board chrome

Expected executor skills: tdd, verify-before-complete.

Why: The existing shell has `ShellMode.Plan` but the PLAN tab is disabled and only Write/Manage surfaces are visible. The corkboard needs a first-class shell mode that hides side panes like Manage while preserving Write and Manage behavior.

Do:
1. Register `IBookTxtStructureService`, `BookTxtStructureService`, and singleton `CorkboardViewModel` in `App.axaml.cs` after Workspace/Editor dependencies are available and before `MainWindowViewModel` construction.
2. Inject `CorkboardViewModel` into `MainWindowViewModel`; add `SelectPlanCommand`, `IsCorkboardVisible`, and property-change notifications when `ActiveMode` changes. Keep `SelectResearchCommand` disabled and keep `ShellMode.Plan` as the existing enum value.
3. Subscribe to `CorkboardViewModel.OpenChapterRequested` in `MainWindowViewModel`: set `WorkspaceViewModel.SelectedNode` to the requested chapter, then activate `ShellMode.Write`. This uses the existing WorkspaceViewModel save-before-switch/open logic instead of a parallel editor path.
4. Update `MainWindow.axaml.cs` chrome logic so Plan mode collapses left/right columns and splitters the same way Manage mode does. Avoid the earlier RowDefinition binding gotcha by keeping column-width logic in code-behind.
5. Add `MainWindowPlanModeTests` for command availability, Plan visibility, Write/Manage regressions, open-request switches back to Write and selects the chapter, and chrome helper behavior if extractable/testable. Update converter tests only if the Plan enum converter path lacks coverage.

Failure Modes (Q5): if open-request handling sees a missing or non-chapter item, it should no-op or notify without leaving Plan mode in a half-open state; command exceptions surface through existing ReactiveCommand exception handling.
Load Profile (Q6): shell mode changes are constant-time property changes; no workspace reload or projection rebuild should happen simply from switching tabs.
Negative Tests (Q7): Plan command with no workspace, open request for Part item, switching Write→Plan→Manage→Write, and selected card opening while editor dirty is delegated to WorkspaceViewModel.
Done when: Plan mode is enabled at the view-model/DI layer, has a visible boolean for the AXAML view, and does not regress Write/Manage mode visibility.

## Inputs

- `src/Hymnal/ViewModels/ShellMode.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal/Views/MainWindow.axaml.cs`
- `src/Hymnal/App.axaml.cs`
- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `src/Hymnal.Core/Services/BookTxtStructureService.cs`

## Expected Output

- `src/Hymnal/App.axaml.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal/Views/MainWindow.axaml.cs`
- `tests/Hymnal.Core.Tests/ViewModels/MainWindowPlanModeTests.cs`

## Verification

dotnet test Hymnal.sln --filter FullyQualifiedName~MainWindowPlanModeTests

## Observability Impact

Shell state remains inspectable via `ActiveMode`, `IsCorkboardVisible`, `IsEditorVisible`, and `IsGanttVisible`; Plan-mode failures are localized to command wiring rather than view rendering.
