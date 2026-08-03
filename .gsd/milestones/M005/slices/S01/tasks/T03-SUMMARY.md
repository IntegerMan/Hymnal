---
id: T03
parent: S01
milestone: M005
key_files:
  - src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs
  - src/Hymnal.Core/Services/BookTxtStructureService.cs
  - tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs
key_decisions:
  - MoveEntryAsync uses indexed final order semantics for later UI callers.
  - Path-changing moves use preserve-or-fail chapter-registry reconciliation to avoid silent UUID churn when registry state is absent, duplicated, or conflicting.
  - Rollback failure messages explicitly name source path, target path, failure phase, and whether rollback restored manuscript state.
duration: 
verification_result: mixed
completed_at: 2026-06-18T03:17:45.978Z
blocker_discovered: false
---

# T03: Added rollback-aware chapter path moves that preserve registry UUIDs, update Book.txt ordering, clean stale exclusions, and restore state on write failures.

**Added rollback-aware chapter path moves that preserve registry UUIDs, update Book.txt ordering, clean stale exclusions, and restore state on write failures.**

## What Happened

Added `IBookTxtStructureService.MoveEntryAsync` and implemented it in `BookTxtStructureService` as a single Core operation for path-changing moves. The method validates normalized source and replacement paths, rejects case-only moves, requires source file existence, rejects target file or Book.txt-entry conflicts, pre-validates chapter-registry identity with preserve-or-fail semantics, moves the file, rewrites Book.txt at the requested final index, saves the registry with the same UUID attached to the replacement path, and removes any stale exclusion for the replacement path. If Book.txt writing fails after the file move, it attempts to move the file back and reports whether rollback restored state. If registry saving fails after Book.txt was rewritten, it attempts both file rollback and Book.txt restoration, then returns a phase-aware failure either way. Added focused PathMove tests for cross-Part success, target conflict without writes, Book.txt failure rollback, rollback failure messaging, registry failure rollback, ambiguous registry identity, invalid replacement paths, and missing source files.

## Verification

Ran the required filtered Core test command. `gsd_exec` could not directly run `dotnet` in its bash environment and the Windows-dotnet-through-bash retry hit the known NuGet path issue, so verification was completed through the normal Windows shell. `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter PathMove` passed with 8 PathMove tests. Full solution verification using the available `Hymnal.slnx` also passed with 300 tests. The documented `Hymnal.sln` command failed because this checkout contains `Hymnal.slnx`, not `Hymnal.sln`.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter PathMove (via gsd_exec bash)` | 127 | ❌ fail - dotnet not found in gsd_exec bash environment | 4262ms |
| 2 | `cmd.exe /c "dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter PathMove" (via gsd_exec bash)` | 1 | ❌ fail - known NuGet path issue through WSL bash | 2786ms |
| 3 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter PathMove` | 0 | ✅ pass - 8 PathMove tests passed | 8000ms |
| 4 | `dotnet test Hymnal.sln` | 1 | ❌ fail - documented solution filename not present in checkout | 1000ms |
| 5 | `dotnet test Hymnal.slnx` | 0 | ✅ pass - full suite passed, 300 tests | 9000ms |

## Deviations

The repository contains `Hymnal.slnx` rather than the documented `Hymnal.sln`; full-suite verification used `dotnet test Hymnal.slnx`. `gsd_exec` could not complete .NET verification in this environment due the known bash/Windows dotnet path issue, so the passing verification commands were run with the standard shell tool.

## Known Issues

Existing xUnit analyzer warnings remain in CorkboardViewModelTests about using Assert.Equal for collection size; not introduced by this task.

## Files Created/Modified

- `src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs`
- `src/Hymnal.Core/Services/BookTxtStructureService.cs`
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`
