# S08: Structural consistency UAT and failure polish

**Goal:** Prove the finished M005 structural-editing surfaces behave as one coherent manuscript-structure system by replaying sidebar, Corkboard, and Gantt edits against one workspace, verifying restart/reload persistence, UUID-backed metadata continuity, watcher suppression, and user-visible failure reporting without adding new structural write paths.
**Demo:** After this, a desktop UAT script performs sidebar, Corkboard, and Gantt structure changes on one workspace, restarts the app, and verifies one consistent manuscript state with no UUID or metadata loss.

## Must-Haves

- The slice is complete only when a cross-surface UAT replay exists and passes against a real temp workspace: sidebar include/exclude, rename, and same-Part/Part reorder; Corkboard reorder, cross-Part move, include/exclude, and new chapter insertion; and Gantt same-Part row reorder all leave one consistent Book.txt, chapter file layout, registry UUID mapping, exclusions manifest, notes/phase/target/history metadata, and fresh reload projection. At least one controlled failure case must be asserted to leave no silent partial state and to expose actionable error copy. A desktop/manual UAT script must describe the same scenario for final human replay. Fresh verification must use native Windows dotnet via PowerShell and the actual `Hymnal.slnx` solution path.

## Proof Level

- This slice proves: Final assembly / UAT proof. Real runtime required: yes, using Avalonia ViewModels and real temp-workspace Core services in tests, plus an app build. Human/UAT required: yes, as a written desktop script for manual replay; automated tests provide the executable gate.

## Integration Closure

Consumes upstream structural surfaces from `src/Hymnal/ViewModels/WorkspaceViewModel.cs`, `src/Hymnal/ViewModels/CorkboardViewModel.cs`, `src/Hymnal/ViewModels/GanttViewModel.cs`, `src/Hymnal/Views/SidebarView.axaml.cs`, `src/Hymnal/Views/CorkboardView.axaml.cs`, `src/Hymnal/Views/GanttView.axaml.cs`, and canonical Core contracts in `src/Hymnal.Core/Services/BookTxtStructureService.cs`. New wiring should be limited to fixes or polish found by integrated replay; no new alternate Book.txt write path is allowed. When all tasks pass, nothing remains before M005 is usable end-to-end except optional human repetition of the documented desktop script.

## Verification

- Inspection surfaces are the UAT replay assertions, `Book.txt`, `.hymnal-data/registry.json`, `.hymnal-data/exclusions.json`, metadata sidecars, surface-specific notification fakes, and `CorkboardViewModel.LastStructuralError`. Failure visibility must include operation/path/message/Book.txt context where available and user-facing notification text for sidebar, Corkboard, and Gantt controlled failures. No secrets or PII are introduced; all test data is local manuscript text in temp workspaces.

## Tasks

- [x] **T01: Add cross-surface UAT replay test** `est:2h`
  Why: S02-S07 proved individual surfaces, but the S08 sketch requires one integrated replay across sidebar, Corkboard, and Gantt on the same workspace with restart persistence and UUID continuity. This task owns R013's final cross-surface replay and R005's Gantt participation.
  - Files: `tests/Hymnal.Core.Tests/ViewModels/StructuralConsistencyUatTests.cs`, `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelIntegrationTests.cs`, `tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs`, `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarRenameTests.cs`
  - Verify: powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter StructuralConsistencyUatTests --verbosity minimal"

- [x] **T02: Fix integrated reload and watcher inconsistencies** `est:2h`
  Why: Prior slices repeatedly noted watcher suppression and reload synchronization as cross-surface risk. The S08 UAT replay should drive any necessary product fixes so successful structural edits do not cause duplicate nodes, stale selections, or re-entrant reloads.
  - Files: `src/Hymnal/ViewModels/WorkspaceViewModel.cs`, `src/Hymnal/ViewModels/CorkboardViewModel.cs`, `src/Hymnal/ViewModels/GanttViewModel.cs`, `src/Hymnal/Views/GanttCanvas.cs`, `tests/Hymnal.Core.Tests/ViewModels/StructuralConsistencyUatTests.cs`, `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarReorderTests.cs`, `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelIntegrationTests.cs`, `tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs`
  - Verify: powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter StructuralConsistencyUatTests --verbosity minimal"

- [x] **T03: Polish controlled failure visibility across surfaces** `est:1.5h`
  Why: The sketch explicitly requires at least one simulated or controlled failure case plus final error copy polish. Prior slices already have Core rollback messages, sidebar notifications, and Corkboard `LastStructuralError`; S08 must verify that integrated failure states are user-visible and non-destructive across the real surfaces.
  - Files: `tests/Hymnal.Core.Tests/ViewModels/StructuralConsistencyUatTests.cs`, `src/Hymnal/ViewModels/WorkspaceViewModel.cs`, `src/Hymnal/ViewModels/CorkboardViewModel.cs`, `src/Hymnal/ViewModels/GanttViewModel.cs`, `src/Hymnal.Core/Services/BookTxtStructureService.cs`
  - Verify: powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter StructuralConsistencyUatTests --verbosity minimal"

- [x] **T04: Document desktop UAT script and close verification** `est:1h`
  Why: Automated ViewModel/Core replay is necessary but the sketch specifically calls for a desktop UAT script that an author or reviewer can perform across sidebar, Corkboard, and Gantt after app restart. This task captures the exact manual scenario and runs the full closeout gate.
  - Files: `docs/uat/M005-S08-structural-consistency.md`, `tests/Hymnal.Core.Tests/ViewModels/StructuralConsistencyUatTests.cs`, `src/Hymnal/ViewModels/WorkspaceViewModel.cs`, `src/Hymnal/ViewModels/CorkboardViewModel.cs`, `src/Hymnal/ViewModels/GanttViewModel.cs`
  - Verify: powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test Hymnal.slnx --nologo --verbosity minimal"

## Files Likely Touched

- tests/Hymnal.Core.Tests/ViewModels/StructuralConsistencyUatTests.cs
- tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelIntegrationTests.cs
- tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs
- tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarRenameTests.cs
- src/Hymnal/ViewModels/WorkspaceViewModel.cs
- src/Hymnal/ViewModels/CorkboardViewModel.cs
- src/Hymnal/ViewModels/GanttViewModel.cs
- src/Hymnal/Views/GanttCanvas.cs
- tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarReorderTests.cs
- src/Hymnal.Core/Services/BookTxtStructureService.cs
- docs/uat/M005-S08-structural-consistency.md
