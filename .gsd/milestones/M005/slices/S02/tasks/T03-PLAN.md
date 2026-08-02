---
estimated_steps: 9
estimated_files: 4
skills_used: []
---

# T03: Wire sidebar include and exclude commands

Executor skills to load in task-plan frontmatter: `tdd`, `verify-before-complete`.

Why: S01 already supplied manifest-aware Core include/exclude methods, but S02 must expose them through the sidebar and WorkspaceViewModel so user intent follows the same structural edit path as later Corkboard/Gantt consumers.

Do:
1. Add a WorkspaceViewModel command for including an excluded sidebar chapter. Prefer a request type that can include at a specific index or after a Part, reusing the existing `IncludeExistingFileCommand` implementation if that is the least divergent path.
2. Update `RemoveFromBookCommand`/exclude behavior so excluding an included chapter calls `IBookTxtStructureService.ExcludeEntryAsync` and reloads the workspace exactly as S01 established.
3. Ensure including an excluded node calls `IncludeExistingEntryAsync` or `IncludeExistingEntryAfterPartAsync`, removes the manifest entry via Core, writes Book.txt, suppresses file watchers during the structural operation, and reloads the workspace through `ReloadWorkspaceAsync`.
4. Keep command availability safe: Part nodes remain non-selectable/non-includable, missing Book.txt entries continue to use the existing remove path, and excluded nodes cannot be opened as normal chapters unless the existing selection behavior intentionally supports opening the file. If excluded selection is disabled, cover it in tests.
5. Update or add tests to verify include/exclude commands mutate both Book.txt and `.hymnal-data/exclusions.json`, preserve failure messages from Core, and do not create duplicate sidebar nodes after reload.

Done when: include and exclude command tests pass using real BookTxtStructureService and ExclusionManifestService over temp workspaces.

## Inputs

- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/Views/SidebarView.axaml`
- `src/Hymnal/Views/SidebarView.axaml.cs`
- `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarExclusionTests.cs`
- `src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs`

## Expected Output

- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/Views/SidebarView.axaml`
- `src/Hymnal/Views/SidebarView.axaml.cs`
- `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarExclusionTests.cs`

## Verification

dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter WorkspaceSidebarExclusionTests

## Observability Impact

Include/exclude command failures must call `INotificationService.ShowError` with the Core Result error text so a future agent can distinguish UI wiring issues from Book.txt/manifest write failures.
