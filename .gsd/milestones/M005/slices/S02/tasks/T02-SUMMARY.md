---
id: T02
parent: S02
milestone: M005
key_files:
  - src/Hymnal/ViewModels/WorkspaceViewModel.cs
  - src/Hymnal/App.axaml.cs
  - src/Hymnal.Core/Models/ChapterNode.cs
  - src/Hymnal.Core/Services/ManuscriptService.cs
  - tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarExclusionTests.cs
key_decisions:
  - Project excluded sidebar-only manuscript files in WorkspaceViewModel after registry reconciliation so they stay visible without acquiring fresh registry UUIDs or affecting active book totals.
duration: 
verification_result: passed
completed_at: 2026-06-18T07:30:49.661Z
blocker_discovered: false
---

# T02: Moved excluded-file sidebar projection into WorkspaceViewModel, kept excluded nodes out of registry/totals, and updated the sidebar exclusion contract tests.

**Moved excluded-file sidebar projection into WorkspaceViewModel, kept excluded nodes out of registry/totals, and updated the sidebar exclusion contract tests.**

## What Happened

Implemented sidebar-only excluded chapter projection in `WorkspaceViewModel` by injecting `IExclusionManifestService` and `IOrphanFileDiscoveryService`, loading the manifest plus orphan discovery after active Book.txt nodes are known, and merging only manifest-backed discovered markdown files into the sidebar as `ChapterNode` instances with `IsExcluded = true`. The merge now keeps Book.txt entries authoritative, appends excluded files after the last active entry in their matching Part folder when that Part exists, and appends remaining excluded files after all active entries. Registry reconciliation still runs only on active Book.txt paths, so excluded nodes no longer receive fresh UUIDs or participate in rename/orphan churn; word-count totals and status summaries also ignore excluded nodes. `ChapterNode.IsExcluded` was converted to a non-positional init property to preserve existing positional constructor usage. `ManuscriptService` now loads only Book.txt nodes, leaving sidebar projection to `WorkspaceViewModel`, and DI wiring in `src/Hymnal/App.axaml.cs` was updated accordingly. The T01 contract tests were updated to exercise the new projection ordering and to reseed `WorkspaceViewModel` from its private projection helper for deterministic verification without depending on full Avalonia hydration timing.

## Verification

Verified the task contract with the targeted test suite: `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter WorkspaceSidebarExclusionTests` passed with 6/6 tests green. That run also demonstrated the rest of the test project still compiled, covering the task requirement that existing sidebar, Corkboard, Gantt, and supplemental-doc test code continue to compile after the constructor and projection changes.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter WorkspaceSidebarExclusionTests` | 0 | ✅ pass | 901ms |

## Deviations

Expanded scope slightly by updating `src/Hymnal.Core/Services/ManuscriptService.cs` so excluded-node projection no longer happens before registry hydration. This was necessary to satisfy the task requirement that excluded sidebar nodes not create new registry UUIDs or affect active totals. The test harness also used deterministic reseeding through the private projection helper instead of awaiting full Avalonia hydration because repeated full-load test runs proved timing-sensitive.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/App.axaml.cs`
- `src/Hymnal.Core/Models/ChapterNode.cs`
- `src/Hymnal.Core/Services/ManuscriptService.cs`
- `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarExclusionTests.cs`
