---
estimated_steps: 8
estimated_files: 7
skills_used: []
---

# T04: Verify rename integration closure

Planner context: use skills verify-before-complete. Why: S03 touches Core, ViewModel, and Avalonia view wiring. The final task must prove the integrated slice, not only isolated tests.

Do:
1. Run the targeted Core, Workspace, and Sidebar smoke test filters from prior tasks, then run the full solution test suite using the project’s working solution file. Prefer native shell dotnet execution; be aware of MEM008 if using gsd_exec through WSL bash.
2. Run a desktop app build for `src/Hymnal/Hymnal.csproj` to catch XAML compile and DI issues.
3. Inspect the final diff for prohibited View or ViewModel direct structural file writes. View code-behind may open UI and execute commands only; WorkspaceViewModel may calculate replacement paths and invoke `IBookTxtStructureService`; Core owns file moves, Book.txt writes, registry reconciliation, and rollback.
4. Confirm `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarRenameTests.cs` includes assertions for chapter rename, Part folder rename, conflict handling, and registry continuity through reload, matching the approved sketch.
5. If verification reveals the preloaded R010 ownership mapping, document in the task summary that R010 was not advanced because it is outside the approved S03 sketch; R013 is the requirement advanced by this slice.

Done when: full tests and app build pass, and the implementation matches the canonical structure service boundary with no silent partial-state path.

## Inputs

- `src/Hymnal.Core/Services/BookTxtStructureService.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/Views/SidebarView.axaml`
- `src/Hymnal/Views/SidebarView.axaml.cs`
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`
- `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarRenameTests.cs`
- `tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs`

## Expected Output

- `src/Hymnal.Core/Services/BookTxtStructureService.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/Views/SidebarView.axaml`
- `src/Hymnal/Views/SidebarView.axaml.cs`
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`
- `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarRenameTests.cs`
- `tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs`

## Verification

dotnet test Hymnal.slnx --nologo --verbosity minimal

## Observability Impact

Confirms observable proof surfaces: automated tests for conflict rollback and user-visible notifications for rename failures.
