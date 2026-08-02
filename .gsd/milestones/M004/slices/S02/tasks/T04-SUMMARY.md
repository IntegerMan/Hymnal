---
id: T04
parent: S02
milestone: M004
key_files:
  - src/Hymnal/Views/SupplementalDocsView.axaml
  - src/Hymnal/Views/SupplementalDocsView.axaml.cs
  - src/Hymnal/Views/MainWindow.axaml
  - tests/Hymnal.Core.Tests/Views/SupplementalDocsViewSmokeTests.cs
key_decisions:
  - Kept supplemental docs in a dedicated SupplementalDocsView bound to SupplementalDocsViewModel rather than mixing doc nodes into the chapter SidebarView or WorkspaceViewModel.Nodes.
  - Used ViewModel/service integration plus AXAML wiring assertions for automated smoke coverage to avoid introducing a global Avalonia.Headless test fixture that destabilizes existing tests.
duration: 
verification_result: mixed
completed_at: 2026-06-04T04:54:08.267Z
blocker_discovered: false
---

# T04: Added a distinct DOCS sidebar section wired to SupplementalDocsViewModel with create-folder, create-file, and file-open interactions plus smoke coverage for create-open-save-reopen behavior.

**Added a distinct DOCS sidebar section wired to SupplementalDocsViewModel with create-folder, create-file, and file-open interactions plus smoke coverage for create-open-save-reopen behavior.**

## What Happened

Implemented a new SupplementalDocsView AXAML/code-behind pair for the DOCS sidebar section. The view exposes + Folder and + File affordances, prompts using the same lightweight code-behind dialog pattern as corkboard interactions, routes all mutations through SupplementalDocsViewModel commands, and opens selected/double-clicked file nodes through OpenDocCommand while folder nodes remain non-editor targets. Updated MainWindow.axaml so the expanded left pane has separate CHAPTERS and DOCS sections: the existing SidebarView still receives only WorkspaceViewModel, while the new SupplementalDocsView receives SupplementalDocsViewModel. Added smoke/integration coverage that verifies AXAML wiring for MainWindow and SupplementalDocsView, verifies command routing exists in code-behind, and exercises create folder, create file, editor open identity, atomic save, reconstructed docs ViewModel refresh, and reopen with content intact.

## Verification

Ran focused DOCS smoke tests successfully, built the Avalonia app successfully, and ran the required full solution test command. `dotnet test Hymnal.sln` currently fails in unrelated pre-existing Gantt/Corkboard tests; the Gantt failures reproduce when running `dotnet test Hymnal.sln --filter "FullyQualifiedName~GanttViewModelTests"` without relying on the DOCS changes. Manual desktop UI smoke was not run because this execution environment is headless/non-interactive; the automated create-open-save-reopen path covers the slice demo's persistence behavior through the same services and editor save path.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test Hymnal.sln --filter SupplementalDocsViewSmokeTests` | 0 | ✅ pass | 660ms |
| 2 | `dotnet build src/Hymnal/Hymnal.csproj && dotnet test Hymnal.sln --filter SupplementalDocsViewSmokeTests` | 0 | ✅ pass | 12000ms |
| 3 | `dotnet test Hymnal.sln` | 1 | ❌ fail - unrelated pre-existing Gantt/Corkboard test failures | 11000ms |
| 4 | `dotnet test Hymnal.sln --filter "FullyQualifiedName~GanttViewModelTests"` | 1 | ❌ fail - isolated unrelated Gantt tests reproduce independently | 11000ms |

## Deviations

MainWindow Window construction was not kept in the automated smoke test because initializing Avalonia.Headless globally in this shared xUnit suite perturbed existing scheduler-sensitive tests. The final test verifies MainWindow AXAML wiring from the tracked file and exercises DOCS behavior through ViewModels/services instead. Manual desktop smoke was not possible in this headless environment.

## Known Issues

`dotnet test Hymnal.sln` fails in existing GanttCanvasTests, GanttViewModelTests, CorkboardViewSmokeTests, and CorkboardProjectionTests unrelated to the DOCS sidebar changes; Gantt failures reproduce with a focused Gantt filter. The new focused DOCS smoke tests pass.

## Files Created/Modified

- `src/Hymnal/Views/SupplementalDocsView.axaml`
- `src/Hymnal/Views/SupplementalDocsView.axaml.cs`
- `src/Hymnal/Views/MainWindow.axaml`
- `tests/Hymnal.Core.Tests/Views/SupplementalDocsViewSmokeTests.cs`
