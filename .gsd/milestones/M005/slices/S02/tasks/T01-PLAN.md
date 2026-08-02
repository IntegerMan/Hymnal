---
estimated_steps: 10
estimated_files: 2
skills_used: []
---

# T01: Add excluded sidebar projection tests

Executor skills to load in task-plan frontmatter: `tdd`, `verify-before-complete`.

Why: S02 is a user-facing projection of S01 manifest state, so tests must first pin the expected contract before wiring UI behavior. The sketch promises excluded files appear in the sidebar and survive reload; this task defines that observable contract at the WorkspaceViewModel level with real temp-workspace files.

Do:
1. Add a new test file under `tests/Hymnal.Core.Tests/ViewModels/` for WorkspaceViewModel sidebar exclusion behavior, using the existing test project patterns and hand-rolled fakes for picker/settings/notifications as needed.
2. Build temp workspaces with `Book.txt`, included `.md` chapters, excluded `.md` files, and `.hymnal-data/exclusions.json` entries created through `ExclusionManifestService` where practical.
3. Assert that `LoadWorkspaceAsync`/workspace binding yields both included and intentionally excluded chapter nodes in order, with excluded nodes distinguishable by a new public state (for example `ChapterNode.IsExcluded` or equivalent ViewModel property), and that ordinary unmanifested orphan files are not projected as excluded chapters.
4. Assert that excluded nodes reappear after reloading a fresh WorkspaceViewModel for the same temp workspace.
5. Add a command-level test shape for including an excluded node and excluding an included node through WorkspaceViewModel commands. It may start failing until T02 implements commands, but the assertions must be real and must verify Book.txt and exclusions manifest state, not just method calls.
6. Include failure-path assertions that an include/exclude failure surfaces a notification and does not silently remove the sidebar state.

Done when: tests clearly express the S02 contract and fail for missing implementation rather than compile errors unrelated to the intended API.

## Inputs

- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/ViewModels/ChapterViewModel.cs`
- `src/Hymnal.Core/Models/ChapterNode.cs`
- `src/Hymnal.Core/Interfaces/IExclusionManifestService.cs`
- `src/Hymnal.Core/Interfaces/IOrphanFileDiscoveryService.cs`
- `tests/Hymnal.Core.Tests/Services/ExclusionManifestServiceTests.cs`
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`

## Expected Output

- `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarExclusionTests.cs`

## Verification

dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter WorkspaceSidebarExclusionTests

## Observability Impact

Tests must capture notification text for include/exclude failures so future agents can localize whether Core rejected the operation or WorkspaceViewModel swallowed the error.
