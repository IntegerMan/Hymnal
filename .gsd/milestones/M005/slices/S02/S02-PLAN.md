# S02: Sidebar excluded chapters and include toggle

**Goal:** Project intentionally excluded manuscript markdown files into the sidebar as excluded chapter nodes and let authors include or exclude them through WorkspaceViewModel commands that reuse the S01 BookTxtStructureService reload path.
**Demo:** After this, a chapter absent from Book.txt appears in the sidebar with excluded styling, can be included or excluded, and remains correct after reload.

## Must-Haves

- After completing this slice, a markdown chapter absent from Book.txt but present in `.hymnal-data/exclusions.json` is visible in the sidebar with excluded styling, is not treated as a missing Book.txt entry, can be included back into Book.txt from the sidebar, can be excluded from the sidebar, and remains correct after workspace reload. The implementation must stay inside the S02 sketch: no rename, drag-reorder, Corkboard insertion, or Gantt behavior beyond preserving existing callers.

## Proof Level

- This slice proves: Integration proof. Real runtime required: no desktop runtime required, but tests must exercise WorkspaceViewModel plus real Core services over temp workspaces. Human/UAT required: no; visual styling is covered by a smoke/source-level assertion plus ViewModel state tests.

## Integration Closure

Consumes S01 `IExclusionManifestService`, `IOrphanFileDiscoveryService`, and manifest-aware `IBookTxtStructureService.IncludeExistingEntryAsync`, `IncludeExistingEntryAfterPartAsync`, and `ExcludeEntryAsync`. New wiring is the WorkspaceViewModel projection and sidebar context-menu include/exclude surface. Remaining milestone work after this slice: sidebar rename, sidebar reorder, Corkboard structural editing, Gantt reorder, and integrated UAT.

## Verification

- Runtime signals remain user-visible Result failures through existing `INotificationService.ShowError`; no telemetry is introduced. Inspection surfaces are `WorkspaceViewModel.Nodes` / `VisibleNodes`, `.hymnal-data/exclusions.json`, and Book.txt content in tests. Failure visibility must preserve S01 phase-aware Core error messages when include/exclude fails.

## Tasks

- [x] **T01: Add excluded sidebar projection tests** `est:1.5h`
  Executor skills to load in task-plan frontmatter: `tdd`, `verify-before-complete`.
  - Files: `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarExclusionTests.cs`, `tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj`
  - Verify: dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter WorkspaceSidebarExclusionTests

- [x] **T02: Project excluded nodes in WorkspaceViewModel** `est:2.5h`
  Executor skills to load in task-plan frontmatter: `tdd`, `verify-before-complete`.
  - Files: `src/Hymnal/ViewModels/WorkspaceViewModel.cs`, `src/Hymnal/App.axaml.cs`, `src/Hymnal.Core/Models/ChapterNode.cs`, `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarExclusionTests.cs`
  - Verify: dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter WorkspaceSidebarExclusionTests

- [x] **T03: Wire sidebar include and exclude commands** `est:2h`
  Executor skills to load in task-plan frontmatter: `tdd`, `verify-before-complete`.
  - Files: `src/Hymnal/ViewModels/WorkspaceViewModel.cs`, `src/Hymnal/Views/SidebarView.axaml`, `src/Hymnal/Views/SidebarView.axaml.cs`, `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarExclusionTests.cs`
  - Verify: dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter WorkspaceSidebarExclusionTests

- [x] **T04: Add excluded styling and integration smoke coverage** `est:1.5h`
  Executor skills to load in task-plan frontmatter: `tdd`, `verify-before-complete`.
  - Files: `src/Hymnal/Views/SidebarView.axaml`, `src/Hymnal/Views/SidebarView.axaml.cs`, `src/Hymnal/Views/Converters/NodeKindAndMissingToForegroundConverter.cs`, `tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs`, `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarExclusionTests.cs`
  - Verify: dotnet test Hymnal.slnx --nologo

## Files Likely Touched

- tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarExclusionTests.cs
- tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj
- src/Hymnal/ViewModels/WorkspaceViewModel.cs
- src/Hymnal/App.axaml.cs
- src/Hymnal.Core/Models/ChapterNode.cs
- src/Hymnal/Views/SidebarView.axaml
- src/Hymnal/Views/SidebarView.axaml.cs
- src/Hymnal/Views/Converters/NodeKindAndMissingToForegroundConverter.cs
- tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs
