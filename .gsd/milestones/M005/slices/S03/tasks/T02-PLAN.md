---
estimated_steps: 9
estimated_files: 3
skills_used: []
---

# T02: Wire Workspace rename commands

Planner context: use skills tdd and verify-before-complete. Why: the sidebar needs a ViewModel command surface that translates user rename intent into canonical Core operations, suppresses watchers, reloads the workspace, and preserves selection without doing raw file mutation in the ViewModel.

Do:
1. Add request records if needed, for example `RenameChapterRequest` and `RenamePartRequest`, in `src/Hymnal/ViewModels/WorkspaceViewModel.cs` or a nearby existing request location. Inputs should include the existing relative path and the desired new title or replacement path; keep the command contract testable without Avalonia dialogs.
2. Add `ReactiveCommand` properties for sidebar rename operations. Commands must be gated by `HasWorkspace`, must reject excluded and missing nodes, and must reject blank or invalid rename input before calling Core.
3. Build replacement paths deterministically in the ViewModel: preserve the current extension, slug or normalize the requested title into a safe filename or folder segment, keep chapters in their current folder, and for Part nodes rename the current Part folder while preserving the Part filename unless Core tests establish another consistent rule. Document the chosen path-building rule in comments and tests.
4. Invoke only `_structureService.RenameEntryAsync(BookTxtPath, existingPath, replacementPath)` inside `ExecuteStructuralOperationAsync` or an equivalent watcher-suppressed helper. Do not call `File.Move`, `Directory.Move`, `File.WriteAllText`, or metadata services directly for the structural rename.
5. After success, reload the current workspace and reselect the replacement node where practical. On failure, show a clear notification containing the operation, source path, target path or title, and Core failure message while leaving the current sidebar state unchanged.
6. Add integration tests in `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarRenameTests.cs` using temp workspaces and real Core services. Cover chapter rename preserving UUID plus phase, target, notes, and word-count-history lookup by UUID; Part folder rename updating all child paths and preserving child UUIDs; conflict failure notification with unchanged Book.txt and files; and reload continuity with no duplicate nodes.

Done when: Workspace-level tests prove the sidebar command path consumes Core rename operations and reloads to the renamed paths with existing UUID-backed state still attached.

## Inputs

- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarExclusionTests.cs`
- `src/Hymnal.Core/Services/BookTxtStructureService.cs`

## Expected Output

- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarRenameTests.cs`

## Verification

dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter WorkspaceSidebarRenameTests --verbosity minimal

## Observability Impact

Adds user-visible failure notifications for invalid rename input, Core conflicts, and reload failures while preserving S01 phase-aware Core details.
