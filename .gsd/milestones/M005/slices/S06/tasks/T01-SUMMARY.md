---
id: T01
parent: S06
milestone: M005
key_files:
  - src/Hymnal/ViewModels/CorkboardViewModel.cs
  - tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs
key_decisions:
  - Kept CorkboardViewModel as a thin consumer of IBookTxtStructureService and asserted canonical IncludeExisting/Exclude methods instead of legacy add/remove paths.
duration: 
verification_result: passed
completed_at: 2026-06-18T19:05:22.225Z
blocker_discovered: false
---

# T01: Hardened Corkboard include/exclude command contracts with focused tests and fixed stale selection after successful exclusion.

**Hardened Corkboard include/exclude command contracts with focused tests and fixed stale selection after successful exclusion.**

## What Happened

Added focused CorkboardViewModel tests covering card-level exclusion through ExcludeEntryAsync, inclusion by explicit index through IncludeExistingEntryAsync, inclusion after a Part through IncludeExistingEntryAfterPartAsync, local invalid include rejection, and Core failure surfacing for both include and exclude. The test fake now distinguishes legacy AddExisting/Remove methods from canonical IncludeExisting/Exclude methods, and the workspace spy can mutate projected nodes during reload to model successful structural changes. Verification initially exposed that successful removal could leave a stale selected CardViewModel because selection restoration ran before the throttled projection rebuild; RemoveFromBookAsync now passes an explicit empty selection override on successful exclusion, and RestoreSelection clears the current selection when a requested path is absent while preserving the pending path for future restoration.

## Verification

Ran the focused task verification command: dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter CorkboardViewModelTests --verbosity minimal. Final run passed 31/31 CorkboardViewModelTests. Build/test emitted only pre-existing analyzer/compiler warnings unrelated to this task.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter CorkboardViewModelTests --verbosity minimal` | 0 | ✅ pass — 31/31 CorkboardViewModelTests passed | 7483ms |

## Deviations

None.

## Known Issues

Focused test run still reports existing warnings: WorkspaceViewModel CS4014 and xUnit analyzer suggestions in existing tests; no new blocking failures.

## Files Created/Modified

- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs`
