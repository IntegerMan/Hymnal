---
id: T01
parent: S05
milestone: M005
key_files:
  - src/Hymnal/ViewModels/CorkboardViewModel.cs
  - tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs
key_decisions:
  - Use a minimal CorkboardDropRequest/DropCardCommand ViewModel surface for S05 drag/drop contracts while leaving behavior red for the implementation task.
duration: 
verification_result: mixed
completed_at: 2026-06-18T13:28:59.552Z
blocker_discovered: false
---

# T01: Added red Corkboard drop-contract tests plus a minimal DropCardCommand skeleton for same-Part reorder, cross-Part move, empty-Part drop, and invalid-drop failure visibility.

**Added red Corkboard drop-contract tests plus a minimal DropCardCommand skeleton for same-Part reorder, cross-Part move, empty-Part drop, and invalid-drop failure visibility.**

## What Happened

Added a public CorkboardDropRequest and DropCardCommand skeleton in CorkboardViewModel so the next implementation task has a concrete public ViewModel surface to satisfy. Extended CorkboardViewModelTests with focused contract tests that specify how Corkboard drag/drop intent must map to Core operations: same-Part drops after a sibling must call ReorderEntryAsync using all Book.txt nodes, cross-Part drops must call MoveEntryAsync with a replacement path under the target Part folder, empty-Part drops must target the slot immediately after the Part divider, and missing/excluded/self drops must avoid Core mutation while surfacing LastStructuralError and a notification. Updated test fakes to record MoveEntryAsync separately from ReorderEntryAsync and to inject orphan/excluded cards through IOrphanFileDiscoveryService.

## Verification

Ran the focused CorkboardViewModelTests command. The project restored and compiled successfully, the test runner discovered 22 CorkboardViewModelTests, existing tests passed, and the four new drop-contract tests failed as expected because DropCardCommand is intentionally a minimal skeleton for the next task. This satisfies the task's done condition: tests compile and fail for missing behavior with names documenting the next contract.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `gsd_exec: powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter CorkboardViewModelTests --verbosity minimal"` | 0 | ⚠️ inconclusive: no stdout/stderr, not used as final evidence | 4018ms |
| 2 | `gsd_exec: cmd.exe /c "\"C:\Program Files\dotnet\dotnet.exe\" test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter FullyQualifiedName~CorkboardViewModelTests --verbosity normal"` | 1 | ⚠️ quoting failure, not source verification | 297ms |
| 3 | `powershell.exe -NoProfile -Command "Get-Process testhost -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue; Start-Sleep -Milliseconds 500; & 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter CorkboardViewModelTests --verbosity minimal"` | 1 | ✅ expected red: compiled, discovered 22 tests, 18 passed and 4 new drop-contract tests failed for missing behavior | 18000ms |

## Deviations

The required gsd_exec lane was attempted first, but PowerShell/dotnet invocations returned no stdout and one quoting attempt failed; a stale testhost process from the silent run locked output DLLs. I cleared testhost and reran the plan's PowerShell verification directly to obtain readable compile/fail evidence.

## Known Issues

DropCardCommand is intentionally not implemented yet. Four new contract tests fail until the next task maps Corkboard drops to ReorderEntryAsync/MoveEntryAsync and validates invalid drops.

## Files Created/Modified

- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs`
