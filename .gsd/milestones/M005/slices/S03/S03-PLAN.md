# S03: Sidebar rename with UUID continuity

**Goal:** Sidebar rename affordances route chapter and Part renames through the canonical BookTxtStructureService path, updating filesystem paths, Book.txt entries, display headings, and registry-backed metadata continuity with visible conflict failures.
**Demo:** After this, renaming a chapter or Part from the sidebar updates files and Book.txt, reloads the workspace, and preserves notes, phases, targets, and history under the same chapter UUID.

## Must-Haves

- Must-haves: sidebar exposes rename for included present chapters and Part nodes only; rename calls the canonical BookTxtStructureService path and no View or ViewModel performs direct structural file mutation; chapter rename moves the markdown file, rewrites Book.txt, updates display heading, reloads workspace, and preserves the same registry UUID with notes, phase data, targets, and word-count history still reachable by UUID; Part folder rename moves the folder, rewrites affected Book.txt entries, reloads all child nodes under the new paths, and preserves child UUID metadata continuity; conflicts and invalid paths fail before mutation or roll back with user-visible phase-aware errors. Active requirement advanced: R013. The preloaded R010 Primary Owner mapping appears outside the approved S03 sketch and should be documented, not implemented, unless separately replanned.

## Proof Level

- This slice proves: Integration proof: Core contract tests plus WorkspaceViewModel integration tests plus Sidebar smoke tests; real runtime required for app build, human UAT deferred to S08.

## Integration Closure

Upstream consumed: S01 BookTxtStructureService, MoveEntryAsync rollback patterns, ChapterRegistryService UUID continuity, metadata sidecars, S02 ChapterNode.IsExcluded projection and sidebar include/exclude command split. New wiring: WorkspaceViewModel rename commands and SidebarView rename context-menu affordance. Remaining milestone work: S04-S07 reorder and Corkboard or Gantt structural consumers, then S08 integrated desktop UAT.

## Verification

- Runtime signals are Result.Fail messages and notification errors rather than telemetry. Inspection surfaces are Book.txt, renamed files or folders, `.hymnal-data/registry.json`, metadata sidecar files keyed by UUID, and WorkspaceViewModel notification errors. Failure visibility must include operation phase, source path, target path, and rollback status. No secrets or PII are introduced.

## Tasks

- [x] **T01: Implement canonical rename contracts** `est:3h`
  Planner context: use skills tdd and verify-before-complete. Why: S03 must not add sidebar-only file I/O. The Core service must be the single consistency boundary for both a single chapter file rename and a Part folder rename before the UI is wired. Active requirement supported: R013. Assumption: the preloaded R010 mapping to M005/S03 is stale or erroneous because AI editorial work is outside the approved S03 sketch; do not implement AI editorial here.
  - Files: `src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs`, `src/Hymnal.Core/Services/BookTxtStructureService.cs`, `src/Hymnal.Core/Services/ChapterRegistryService.cs`, `src/Hymnal.Core/Services/BookTxtParser.cs`, `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`
  - Verify: dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter BookTxtStructureServiceTests --verbosity minimal

- [x] **T02: Wire Workspace rename commands** `est:3h`
  Planner context: use skills tdd and verify-before-complete. Why: the sidebar needs a ViewModel command surface that translates user rename intent into canonical Core operations, suppresses watchers, reloads the workspace, and preserves selection without doing raw file mutation in the ViewModel.
  - Files: `src/Hymnal/ViewModels/WorkspaceViewModel.cs`, `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarRenameTests.cs`, `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarExclusionTests.cs`
  - Verify: dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter WorkspaceSidebarRenameTests --verbosity minimal

- [x] **T03: Add sidebar rename affordances** `est:2h`
  Planner context: use skills tdd and verify-before-complete. Why: the slice demo requires rename affordances from the sidebar, not only hidden ViewModel commands. The View should collect a new title or path, call WorkspaceViewModel commands, and keep business logic out of code-behind.
  - Files: `src/Hymnal/Views/SidebarView.axaml`, `src/Hymnal/Views/SidebarView.axaml.cs`, `tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs`
  - Verify: dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter SidebarViewSmokeTests --verbosity minimal

- [x] **T04: Verify rename integration closure** `est:1h`
  Planner context: use skills verify-before-complete. Why: S03 touches Core, ViewModel, and Avalonia view wiring. The final task must prove the integrated slice, not only isolated tests.
  - Files: `src/Hymnal.Core/Services/BookTxtStructureService.cs`, `src/Hymnal/ViewModels/WorkspaceViewModel.cs`, `src/Hymnal/Views/SidebarView.axaml`, `src/Hymnal/Views/SidebarView.axaml.cs`, `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`, `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarRenameTests.cs`, `tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs`
  - Verify: dotnet test Hymnal.slnx --nologo --verbosity minimal

## Files Likely Touched

- src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs
- src/Hymnal.Core/Services/BookTxtStructureService.cs
- src/Hymnal.Core/Services/ChapterRegistryService.cs
- src/Hymnal.Core/Services/BookTxtParser.cs
- tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs
- src/Hymnal/ViewModels/WorkspaceViewModel.cs
- tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarRenameTests.cs
- tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarExclusionTests.cs
- src/Hymnal/Views/SidebarView.axaml
- src/Hymnal/Views/SidebarView.axaml.cs
- tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs
