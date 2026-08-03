---
estimated_steps: 8
estimated_files: 3
skills_used: []
---

# T04: Prove sidebar reorder integration and reload persistence

Why: the slice demo is not true until chapters and Parts can be reordered through the real sidebar/ViewModel/Core path and the resulting Book.txt order survives workspace reload without duplicate nodes. This final task closes the integration proof and guards the existing rename/exclusion surfaces against regression.

Expected skills_used frontmatter: test, verify-before-complete.

Do:
1. Add integration coverage in `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarReorderTests.cs` using real temp workspaces and real Core services: drag-style chapter reorder within a Part updates Book.txt and survives reload; drag-style Part reorder moves the whole Part block and survives reload; excluded projections remain present but are not duplicated after reload; failure from Core produces an error notification and leaves the visible order unchanged.
2. If a small manual UAT checklist already exists for M005, append S04 scenarios there; otherwise add a concise S04 section to an existing test/summary-adjacent markdown only if the repository already has a suitable UAT artifact. Do not invent broader Corkboard or Gantt UAT in this slice.
3. Run the focused suites for BookTxtStructureServiceTests, WorkspaceSidebarReorderTests, and SidebarViewSmokeTests, then run the full solution tests through `Hymnal.slnx` using the native Windows dotnet path if the shell environment needs it.
4. Inspect for the existing non-blocking CS4014 warning if build is run, but do not expand this slice to fix unrelated warning cleanup.

Done when: focused tests and full solution tests pass, Book.txt persistence is asserted after reload for both chapter and Part reorder, no duplicate visible/sidebar nodes are observed, and no structural writes bypass BookTxtStructureService.

## Inputs

- `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarReorderTests.cs`
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`
- `tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/Views/SidebarView.axaml.cs`
- `src/Hymnal.Core/Services/BookTxtStructureService.cs`

## Expected Output

- `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarReorderTests.cs`
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`
- `tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs`

## Verification

powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test Hymnal.slnx --nologo --verbosity minimal"

## Observability Impact

Closes proof with automated assertions on error notifications and reload-visible order, giving future agents concrete failure evidence if reorder state, watcher suppression, or projection merge regresses.
