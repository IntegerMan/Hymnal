---
id: T02
parent: S05
milestone: M005
key_files:
  - src/Hymnal/ViewModels/CorkboardViewModel.cs
  - tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs
key_decisions:
  - Corkboard DropCardCommand now resolves all drop positions against Workspace.Nodes, then chooses ReorderEntryAsync for same-Part moves and MoveEntryAsync for cross-Part moves followed by workspace reload.
duration: 
verification_result: passed
completed_at: 2026-06-18T13:32:12.319Z
blocker_discovered: false
---

# T02: Implemented Corkboard DropCardCommand orchestration for same-Part reorders, cross-Part MoveEntry moves, empty-Part drops, reloads, selection restoration, and visible failure reporting.

**Implemented Corkboard DropCardCommand orchestration for same-Part reorders, cross-Part MoveEntry moves, empty-Part drops, reloads, selection restoration, and visible failure reporting.**

## What Happened

Replaced the T01 placeholder DropCardCommand with real drop resolution against Workspace.Nodes, including all Book.txt entries rather than only visible chapter cards. The implementation rejects missing, excluded, and self-target drops without mutating the projection; resolves target indices from before/after neighbors or target Part dividers; detects source and target Part ownership; calls ReorderEntryAsync for same-Part moves; calls MoveEntryAsync with a target-folder replacement path for cross-Part moves; wraps structural writes in SuppressFileWatcher; reloads the workspace after successful operations; and restores selection to the old path for same-Part moves or replacement path for cross-Part moves. Tests were expanded to assert same-Part reload behavior plus Core move failure and reload failure surfaces through CorkboardStructuralError and INotificationService.

## Verification

Ran the task-specified focused test command for CorkboardViewModelTests. The command restored/build dependencies, compiled Hymnal.Core, Hymnal, and Hymnal.Core.Tests, and passed all 24 Corkboard ViewModel tests. Existing analyzer/compiler warnings remain unrelated to this task: WorkspaceViewModel CS4014 and xUnit analyzer style warnings in tests.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter CorkboardViewModelTests --verbosity minimal"` | 0 | ✅ pass: 24 CorkboardViewModelTests passed, 0 failed | 6000ms |

## Deviations

Used direct PowerShell/dotnet verification instead of gsd_exec because project memory documents that gsd_exec invokes Windows dotnet.exe through WSL bash in a way that breaks NuGet/package path resolution for this repository.

## Known Issues

Existing warnings remain: WorkspaceViewModel.cs CS4014 and xUnit analyzer style warnings in unrelated/assertion-style test lines. No functional test failures.

## Files Created/Modified

- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs`
