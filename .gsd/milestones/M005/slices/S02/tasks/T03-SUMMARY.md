---
id: T03
parent: S02
milestone: M005
key_files:
  - src/Hymnal/ViewModels/WorkspaceViewModel.cs
  - src/Hymnal/Views/SidebarView.axaml
  - src/Hymnal/Views/SidebarView.axaml.cs
  - tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarExclusionTests.cs
key_decisions:
  - Excluded sidebar nodes are treated as UI-only projections: the sidebar include command computes the Book.txt insertion index from the node's current sidebar position among non-excluded nodes, preserving visible ordering after reload.
  - Missing Book.txt entries now use a dedicated sidebar remove path (`RemoveEntryAsync`) instead of the exclusion path so stale entries are removed without creating manifest exclusions.
  - Excluded sidebar nodes are selection-safe: `TrySwitchChapterAsync` restores the previous selection instead of opening excluded files in the editor.
duration: 
verification_result: passed
completed_at: 2026-06-18T07:40:32.661Z
blocker_discovered: false
---

# T03: Added sidebar include/exclude command wiring for excluded and missing nodes, plus selection-safe excluded chapter behavior.

**Added sidebar include/exclude command wiring for excluded and missing nodes, plus selection-safe excluded chapter behavior.**

## What Happened

Updated `WorkspaceViewModel` to expose two sidebar-specific structural commands: `IncludeExcludedChapterCommand` resolves an excluded node back into an `IncludeExistingChapterRequest` using its current sidebar position, while `RemoveMissingEntryCommand` preserves the legacy stale-entry removal path through `RemoveEntryAsync`. `RemoveFromBookCommand` continues to exclude included chapters through `ExcludeEntryAsync`. I also hardened `TrySwitchChapterAsync` so excluded nodes cannot be opened in the editor and instead restore the prior selection. In the sidebar view, the context menu now routes excluded rows to a new `Include in book` action and routes missing rows to the dedicated remove command, while code-behind guards keep chapter-only actions off unsupported node states. The sidebar exclusion test suite was expanded to cover the new include command, missing-entry removal without manifest pollution, failure-message preservation, duplicate-free reloads, and excluded-selection no-open behavior using real `BookTxtStructureService` and `ExclusionManifestService` temp workspaces.

## Verification

Ran the targeted sidebar exclusion suite with `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter WorkspaceSidebarExclusionTests`. All 8 tests passed, covering include/exclude manifest updates, missing-entry removal, failure notifications, excluded-node reload stability, and excluded selection behavior.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter WorkspaceSidebarExclusionTests` | 0 | ✅ pass | 6750ms |

## Deviations

None.

## Known Issues

Targeted verification passes. The test run still reports pre-existing xUnit analyzer warnings in unrelated test files (`CorkboardViewModelTests.cs`) and one existing analyzer suggestion in `WorkspaceSidebarExclusionTests.cs` that does not affect behavior.

## Files Created/Modified

- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/Views/SidebarView.axaml`
- `src/Hymnal/Views/SidebarView.axaml.cs`
- `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarExclusionTests.cs`
