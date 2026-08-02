---
estimated_steps: 9
estimated_files: 3
skills_used: []
---

# T02: Route sidebar reorder commands through explicit legal move validation

Why: WorkspaceViewModel already exposes ReorderChapterCommand and watcher suppression, but the current path assumes chapter-only reorder and resolves indexes against visible nodes that can include excluded projections. S04 needs a sidebar structural command that accepts chapter and Part drag intents, rejects unsupported drops before Core mutation, suppresses watchers, calls only BookTxtStructureService, and reloads/synchronizes the workspace without duplicate nodes. This advances R013 by making sidebar structural reorder a thin, validated consumer of the canonical Book.txt path.

Expected skills_used frontmatter: tdd, verify-before-complete.

Do:
1. Add or extend `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarReorderTests.cs` to cover: included chapter reorder within the same Part; included Part reorder as a whole block; rejecting excluded source nodes; rejecting missing source nodes; rejecting Part drops onto chapter rows; rejecting cross-Part chapter movement with a user-visible notification; and reload/order continuity with no duplicate VisibleNodes after command completion.
2. Refactor `src/Hymnal/ViewModels/WorkspaceViewModel.cs` from chapter-only reorder resolution to a sidebar reorder resolver that works from active Book.txt nodes only and ignores excluded projections for index calculation.
3. Keep legal moves explicit: chapter source and chapter target are allowed only when both resolve to the same containing Part folder/section; Part source and Part target are allowed only against Part targets and move a whole Part block; excluded/missing nodes cannot be source or target; cross-Part chapter movement is left for S05 Corkboard path-changing moves and must show an error rather than calling Core.
4. Keep `using var _ = _manuscriptService.SuppressFileWatcher();` around the Core mutation. After success, synchronize via `ReloadCurrentWorkspaceAsync()` or a hardened `ReorderNodesAsync()` that has tests proving no duplicate nodes and correct excluded projection merge; prefer full reload if lightweight reorder cannot prove Part-block correctness.
5. Reuse `ReorderCardRequest` only if it remains semantically clear for sidebar requests; if a small sidebar-specific request record is introduced, update call sites and tests but keep all file/Book.txt mutation in Core.

Done when: ViewModel tests prove legal sidebar reorder requests call BookTxtStructureService once with the expected path/index, illegal requests do not call Core and raise notifications, watcher suppression is used, and a reload preserves the new order without duplicate nodes.

## Inputs

- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarRenameTests.cs`
- `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarExclusionTests.cs`

## Expected Output

- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarReorderTests.cs`

## Verification

powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter WorkspaceSidebarReorderTests --verbosity minimal"

## Observability Impact

Adds user-visible notification coverage for illegal sidebar drops and Core reorder failures, including source/target context, so future agents can distinguish unsupported gestures from failed filesystem or Book.txt mutations.
