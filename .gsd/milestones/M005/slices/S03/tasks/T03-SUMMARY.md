---
id: T03
parent: S03
milestone: M005
key_files:
  - src/Hymnal/Views/SidebarView.axaml
  - src/Hymnal/Views/SidebarView.axaml.cs
  - tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs
key_decisions:
  - Sidebar rename uses a small code-behind modal text prompt, while all rename validation, path building, filesystem mutation, Book.txt updates, and workspace reload remain delegated to WorkspaceViewModel/Core rename commands.
  - Rename menu visibility is centralized in `SidebarView.CanRenameFromSidebar`, and the context menu toggles the rename item at open time so excluded or missing nodes never present the affordance.
duration: 
verification_result: passed
completed_at: 2026-06-18T12:05:48.595Z
blocker_discovered: false
---

# T03: Added sidebar `Rename…` context-menu affordances with a minimal prompt that delegates chapter and Part renames to the existing canonical WorkspaceViewModel commands.

**Added sidebar `Rename…` context-menu affordances with a minimal prompt that delegates chapter and Part renames to the existing canonical WorkspaceViewModel commands.**

## What Happened

Updated `src/Hymnal/Views/SidebarView.axaml` to declare a `Rename…` context-menu action and a paired separator ahead of the existing chapter actions. Wired `src/Hymnal/Views/SidebarView.axaml.cs` to expose a testable `CanRenameFromSidebar` predicate, toggle rename visibility when a row context menu opens, and show a small owner-centered text prompt prefilled from `Node.Title`. The prompt supports practical keyboard flow via default/cancel buttons and initial text selection; on confirmation it routes Part rows to `WorkspaceViewModel.RenamePartCommand` and chapter rows to `WorkspaceViewModel.RenameChapterCommand`, keeping all structural rename logic in the existing ViewModel/Core path. Extended `tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs` to assert the rename affordance is declared in XAML and that the rename-visibility predicate is true for included present chapter/Part nodes and false for excluded or missing nodes.

## Verification

Verified the sidebar rename affordance through the required targeted smoke suite: `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter SidebarViewSmokeTests --verbosity minimal` passed with 7/7 tests. The tests cover both XAML declaration of the `Rename…` menu item and the view-layer visibility rule that allows rename only for included, present chapter/Part nodes while hiding it for excluded or missing rows.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter SidebarViewSmokeTests --verbosity minimal` | 0 | ✅ pass | 5631ms |

## Deviations

Used the host shell `bash` tool for the required `dotnet test` verification after `gsd_exec` failed with `/bin/bash: dotnet: command not found` in this session's WSL-backed sandbox.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal/Views/SidebarView.axaml`
- `src/Hymnal/Views/SidebarView.axaml.cs`
- `tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs`
