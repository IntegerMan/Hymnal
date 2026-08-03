---
id: T04
parent: S01
milestone: M004
key_files:
  - src/Hymnal/App.axaml.cs
  - src/Hymnal/ViewModels/MainWindowViewModel.cs
  - src/Hymnal/Views/MainWindow.axaml
  - src/Hymnal/Views/MainWindow.axaml.cs
  - src/Hymnal/Views/CorkboardView.axaml
  - src/Hymnal/Views/CorkboardView.axaml.cs
  - tests/Hymnal.Core.Tests/ViewModels/MainWindowPlanModeTests.cs
  - tests/Hymnal.Core.Tests/Views/ShellModeConvertersTests.cs
key_decisions:
  - Plan is a first-class center-panel surface with its own visibility flag and command.
  - Plan and Manage share the same hidden-chrome layout behavior in the shell.
  - The corkboard surface is rendered by a dedicated view rather than repurposing the editor or gantt views.
duration: 
verification_result: passed
completed_at: 2026-06-04T03:36:20.137Z
blocker_discovered: false
---

# T04: Wired Plan mode into the shell with a dedicated corkboard surface, Plan tab command, and Manage-style hidden chrome.

**Wired Plan mode into the shell with a dedicated corkboard surface, Plan tab command, and Manage-style hidden chrome.**

## What Happened

Implemented Plan as a first-class center-panel mode in the shell: MainWindowViewModel now exposes SelectPlanCommand and IsCorkboardVisible, raises visibility notifications when ActiveMode changes, and still routes corkboard open requests back to WorkspaceViewModel before returning to Write. The shell XAML now enables the PLAN tab and renders a dedicated CorkboardView, while MainWindow code-behind collapses the side panes for both Plan and Manage so the center surface gets the full board. I also moved the BookTxtStructureService registration next to the corkboard wiring in App.axaml.cs so the DI graph clearly reflects the new plan-mode surface, and I added/updated tests for mode switching, open-card routing, and the Plan enum converter path.

## Verification

Filtered verification passed: `dotnet test Hymnal.sln --filter FullyQualifiedName~MainWindowPlanModeTests` completed successfully with 3 tests passing, confirming Plan-mode command toggling, Write/Manage/Plan transitions, and corkboard open-to-Write behavior. The updated ShellMode converter test now also covers the Plan enum value.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `pwsh -NoLogo -Command "Set-Location 'C:/Dev/Hymnal'; dotnet test Hymnal.sln --filter FullyQualifiedName~MainWindowPlanModeTests"` | 0 | ✅ pass | 9000ms |

## Deviations

Added a new `CorkboardView` XAML/code-behind scaffold because the repo did not yet contain a plan surface view to host the corkboard items; this was necessary to make the shell-visible Plan mode real. Also reordered `App.axaml.cs` service registration so the corkboard structure service sits with the corkboard view-model wiring.

## Known Issues

None known after the filtered Plan-mode test pass.

## Files Created/Modified

- `src/Hymnal/App.axaml.cs`
- `src/Hymnal/ViewModels/MainWindowViewModel.cs`
- `src/Hymnal/Views/MainWindow.axaml`
- `src/Hymnal/Views/MainWindow.axaml.cs`
- `src/Hymnal/Views/CorkboardView.axaml`
- `src/Hymnal/Views/CorkboardView.axaml.cs`
- `tests/Hymnal.Core.Tests/ViewModels/MainWindowPlanModeTests.cs`
- `tests/Hymnal.Core.Tests/Views/ShellModeConvertersTests.cs`
