# S06: Corkboard inclusion toggle and chapter insertion

**Goal:** Route Corkboard card-level include/exclude and inline chapter insertion through the canonical BookTxtStructureService path, then reload the workspace so Book.txt order, exclusion styling, and restart persistence match the visual Corkboard result.
**Demo:** After this, a Corkboard card can be included or excluded and a new chapter can be inserted between cards with persisted Book.txt order.

## Must-Haves

- Demo: In the Corkboard, an included chapter card can be excluded, the resulting excluded card is styled as excluded and can be included again, and a new chapter can be inserted between existing chapter cards or adjacent to Part dividers with persisted Book.txt order after workspace reload/restart.
- Must-haves:
- R013 is advanced for the two remaining Corkboard structural capabilities in this slice: inclusion toggle and new chapter insertion.
- Include/exclude operations use IBookTxtStructureService.IncludeExistingEntryAsync, IncludeExistingEntryAfterPartAsync, and ExcludeEntryAsync; no Corkboard-only Book.txt or exclusions.json write path is added.
- New chapter insertion uses IBookTxtStructureService.CreateNewChapterAsync and WorkspaceViewModel.ReloadCurrentWorkspaceAsync; it must not hand-edit Book.txt in the view or view-model.
- Verification covers insertion between chapters, insertion before/after or inside Part ranges, excluded card styling/action wiring, duplicate/conflict failure visibility, and reload/restart persistence over a real temp workspace.
- Excluded cards remain non-draggable/non-openable projection items, while included cards remain the only draggable structural move sources.
- Threat surface:
- Abuse: user-provided chapter titles and file paths can attempt duplicate paths, traversal, absolute paths, or malformed markdown names; Core validation and Result.Fail paths must reject these without partial Book.txt/file/manifest divergence.
- Data exposure: no secrets or PII are introduced; only local manuscript file paths and chapter titles are displayed.
- Input trust: Corkboard dialogs and inline create text are untrusted local user input reaching filesystem and Book.txt writes through Core services.
- Requirement impact:
- Requirements touched: R013 primary; R005 only insofar as visible workspace reload and notifications must remain coherent.
- Re-verify: S02 include/exclude manifest semantics, S05 Corkboard drag/drop source eligibility, Book.txt order persistence, and UI failure reporting.
- Decisions revisited: D023 remains in force; no divergent structural write path is planned.
- Verification:
- dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter CorkboardViewModelTests --verbosity minimal
- dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter CorkboardViewSmokeTests --verbosity minimal
- dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter CorkboardViewModelIntegrationTests --verbosity minimal
- dotnet test Hymnal.slnx --nologo --verbosity minimal
- dotnet build src/Hymnal/Hymnal.csproj --nologo
- Assumptions and reconciliation:
- Prior slices already introduced CorkboardViewModel include/create commands and inline create UI scaffolding; this slice should harden and complete those paths rather than replacing them.
- MEM008 means gsd_exec may not be able to run dotnet reliably; executor verification should use the normal native shell available in execute-task and record fresh stdout in task summaries.
- Combined Avalonia/xUnit filters have been flaky; if a combined Corkboard integration filter hangs, executor should run focused classes/tests individually and record equivalent pass evidence.

## Proof Level

- This slice proves: This slice proves integration behavior over ViewModel, XAML smoke, Core services, and real temp-workspace reload persistence. Real runtime required: yes, via Avalonia ViewModel/test host and filesystem temp workspaces. Human/UAT required: no; desktop pointer replay remains deferred to S08 final assembly.

## Integration Closure

Upstream surfaces consumed: `src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs`, `src/Hymnal.Core/Services/BookTxtStructureService.cs`, `src/Hymnal/ViewModels/WorkspaceViewModel.cs`, S02 excluded-node projection, and S05 Corkboard drag/drop/reload patterns.
New wiring introduced in this slice: Corkboard include/exclude and inline-create commands/buttons/menu handlers are made complete and test-backed against the canonical structure service plus reload path.
What remains before M005 is end-to-end usable: S07 Gantt row drag reorder and S08 desktop cross-surface UAT/failure polish.

## Verification

- Runtime signals: CorkboardViewModel.LastStructuralError and INotificationService.ShowError remain the diagnostic surface for invalid include/create/exclude operations and reload failures.
- Inspection surfaces: Corkboard Items projection, Book.txt contents, `.hymnal-data/exclusions.json`, chapter markdown files, and task integration tests over real temp workspaces.
- Failure visibility: duplicate path, invalid path, Core Result.Fail, and reload failure must leave LastStructuralError populated with operation/path/message and avoid silent partial UI state.
- Redaction constraints: no secrets; avoid logging manuscript contents beyond local paths/titles needed for diagnostics.

## Tasks

- [x] **T01: Harden Corkboard include and exclude command contracts** `est:1h`
  Skills expected: tdd, verify-before-complete.
  - Files: `src/Hymnal/ViewModels/CorkboardViewModel.cs`, `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs`
  - Verify: dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter CorkboardViewModelTests --verbosity minimal

- [x] **T02: Complete inline chapter insertion semantics around chapters and Parts** `est:1.5h`
  Skills expected: tdd, design-an-interface, verify-before-complete.
  - Files: `src/Hymnal/ViewModels/CorkboardViewModel.cs`, `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs`
  - Verify: dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter CorkboardViewModelTests --verbosity minimal

- [x] **T03: Wire Corkboard menus and excluded styling smoke coverage** `est:1h`
  Skills expected: verify-before-complete.
  - Files: `src/Hymnal/Views/CorkboardView.axaml`, `src/Hymnal/Views/CorkboardView.axaml.cs`, `tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs`
  - Verify: dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter CorkboardViewSmokeTests --verbosity minimal

- [x] **T04: Prove real workspace persistence for Corkboard inclusion and insertion** `est:2h`
  Skills expected: tdd, verify-before-complete.
  - Files: `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelIntegrationTests.cs`, `src/Hymnal/ViewModels/CorkboardViewModel.cs`, `src/Hymnal.Core/Services/BookTxtStructureService.cs`
  - Verify: dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter CorkboardViewModelIntegrationTests --verbosity minimal

- [x] **T05: Run slice-wide regression and document verification evidence** `est:45m`
  Skills expected: verify-before-complete.
  - Files: `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs`, `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelIntegrationTests.cs`, `tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs`, `src/Hymnal/ViewModels/CorkboardViewModel.cs`, `src/Hymnal/Views/CorkboardView.axaml`, `src/Hymnal/Views/CorkboardView.axaml.cs`
  - Verify: dotnet test Hymnal.slnx --nologo --verbosity minimal

## Files Likely Touched

- src/Hymnal/ViewModels/CorkboardViewModel.cs
- tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs
- src/Hymnal/Views/CorkboardView.axaml
- src/Hymnal/Views/CorkboardView.axaml.cs
- tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs
- tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelIntegrationTests.cs
- src/Hymnal.Core/Services/BookTxtStructureService.cs
