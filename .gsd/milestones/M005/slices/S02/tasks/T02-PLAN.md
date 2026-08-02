---
estimated_steps: 11
estimated_files: 4
skills_used: []
---

# T02: Project excluded nodes in WorkspaceViewModel

Executor skills to load in task-plan frontmatter: `tdd`, `verify-before-complete`.

Why: The sidebar is bound to WorkspaceViewModel `VisibleNodes`; excluded chapters cannot appear until WorkspaceViewModel merges active Book.txt nodes with S01 manifest and orphan discovery. This is the core state-management task for S02.

Do:
1. Inject `IExclusionManifestService` and `IOrphanFileDiscoveryService` into `WorkspaceViewModel` and update DI registration in `src/Hymnal/App.axaml.cs`; keep existing constructor call sites/tests compiling.
2. During workspace hydration, load active Book.txt nodes as today, load the exclusion manifest, run orphan discovery against active Book.txt entries, and create excluded chapter projection nodes only for manifest paths that correspond to discovered manuscript markdown files.
3. Mark excluded projection nodes with explicit state (preferred: extend `ChapterNode` with a non-positional `bool IsExcluded { get; init; }` to avoid breaking existing positional constructor usage). Excluded nodes must remain `NodeKind.Chapter`, `IsMissing == false`, and non-active for word-count/status totals unless a test justifies otherwise.
4. Preserve existing Part collapse behavior and visible-node ordering. Place excluded nodes in a predictable sidebar location: after the active Book.txt nodes, grouped by detected Part folder if the current tree has that Part, otherwise after the book entries. Document the chosen ordering in code comments and tests.
5. Reconcile registry/phase/target hydration so excluded projected nodes do not create new UUIDs or orphan churn unless they already have registry entries; S03 owns UUID continuity for rename, so do not expand scope.
6. Keep unmanifested orphan discovery available to existing Corkboard flows; S02 should not make every orphan appear in the sidebar.
7. Make reload after structural edits use the same existing `ReloadWorkspaceAsync` path so included/excluded state refreshes from Book.txt plus manifest.

Done when: the WorkspaceViewModel tests from T01 pass for projection and reload, and existing sidebar, Corkboard, Gantt, and supplemental-doc tests still compile.

## Inputs

- `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarExclusionTests.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/App.axaml.cs`
- `src/Hymnal.Core/Models/ChapterNode.cs`
- `src/Hymnal.Core/Services/OrphanFileDiscoveryService.cs`
- `src/Hymnal.Core/Services/ExclusionManifestService.cs`

## Expected Output

- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/App.axaml.cs`
- `src/Hymnal.Core/Models/ChapterNode.cs`
- `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarExclusionTests.cs`

## Verification

dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter WorkspaceSidebarExclusionTests

## Observability Impact

Failure state remains inspectable through captured notifications and the persisted Book.txt/exclusions files; projection code should not catch and suppress manifest/orphan failures without a notification.
