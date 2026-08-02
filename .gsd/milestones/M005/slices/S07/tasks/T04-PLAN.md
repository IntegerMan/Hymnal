---
estimated_steps: 4
estimated_files: 3
skills_used: []
---

# T04: Prove Gantt reorder persistence and canonical path

Task plan frontmatter: estimated_steps: 8; estimated_files: 2; skills_used: [tdd, verify-before-complete]

Why: S07 is only complete if a Gantt row move updates `Book.txt`, survives reload, and is demonstrably routed through the same canonical structure path as sidebar and Corkboard. This task closes the integration proof and guards against a second write path.

Do: Add an integration-style test fixture, preferably in `GanttViewModelTests` or a new focused test file, that opens a temp manuscript with at least two Parts and multiple chapters in one Part, constructs the real `WorkspaceViewModel` dependencies used by existing workspace reorder tests, then invokes the Gantt reorder API from T01. Assert: the resulting `Book.txt` order changes for a legal within-Part chapter move; a fresh workspace reload/Gantt projection reflects the new row order; cross-Part chapter attempts are rejected by the shared workspace path with a user-visible error and unchanged `Book.txt`; book and Part rows cannot trigger mutation; and no test references or instantiates `BookTxtStructureService` from `GanttViewModel`. Include a source-level guard if useful to assert `GanttViewModel.cs` does not contain `IBookTxtStructureService` or direct `File.Write*` calls. Run focused Gantt tests and then a full solution test/build through native Windows dotnet. Remember the known `gsd_exec`/WSL dotnet restore failure; use native `powershell.exe`/`C:\Program Files\dotnet\dotnet.exe` commands for task evidence.

Done when: focused tests prove legal Gantt reorder persistence, illegal move failure visibility, reload consistency, and no alternate write path; full solution verification passes or any environment-only failure is documented with exact output.

## Inputs

- `src/Hymnal/ViewModels/GanttViewModel.cs`
- `src/Hymnal/Views/GanttCanvas.cs`
- `src/Hymnal/Views/GanttView.axaml.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarReorderTests.cs`
- `tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs`

## Expected Output

- `tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs`
- `tests/Hymnal.Core.Tests/Views/GanttCanvasTests.cs`
- `tests/Hymnal.Core.Tests/Views/GanttViewSmokeTests.cs`

## Verification

powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test Hymnal.slnx --nologo --verbosity minimal"

## Observability Impact

Improves proof surfaces rather than runtime telemetry: failures are visible as existing WorkspaceViewModel notifications, unchanged Book.txt assertions, and reload-persistence tests.
