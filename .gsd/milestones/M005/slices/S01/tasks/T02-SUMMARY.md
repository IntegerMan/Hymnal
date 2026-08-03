---
id: T02
parent: S01
milestone: M005
key_files:
  - src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs
  - src/Hymnal.Core/Services/BookTxtStructureService.cs
  - src/Hymnal/App.axaml.cs
  - tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs
key_decisions:
  - Intentional include/exclude operations update Book.txt first and the exclusion manifest second; Book.txt write failures leave the manifest untouched, while manifest save failures report the post-Book.txt-write phase explicitly.
duration: 
verification_result: mixed
completed_at: 2026-06-18T03:09:19.876Z
blocker_discovered: false
---

# T02: Added intentional include/exclude Book.txt structure operations backed by the exclusion manifest.

**Added intentional include/exclude Book.txt structure operations backed by the exclusion manifest.**

## What Happened

Extended IBookTxtStructureService with intentional include and exclude operations while preserving existing AddExistingEntryAsync, AddExistingEntryAfterPartAsync, and RemoveEntryAsync as non-manifest primitives. BookTxtStructureService now consumes IExclusionManifestService, removes included entries from Book.txt before saving exclusions, and inserts excluded files into Book.txt before removing manifest entries. Failure messages identify whether the operation failed during Book.txt validation/write or during manifest save after Book.txt write. Registered IExclusionManifestService in Avalonia DI so production resolution works. Added focused Core tests for exclude, include at index, include after Part, duplicate prevention, nonexistent targets, invalid indices, missing Part paths, outside-workspace paths, stale manifest pruning, Book.txt write failure ordering, manifest save failure ordering, and compatibility with the broader existing test suite.

## Verification

Ran the requested IncludeExclude filter. gsd_exec could not locate dotnet in its bash lane, matching the known environment gotcha, so the same command was rerun through the regular shell and passed 13/13 tests. Also ran the full Core test project and the actual repository solution file Hymnal.slnx; both passed 292/292 tests. The documented Hymnal.sln command failed because this checkout contains Hymnal.slnx rather than Hymnal.sln.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter IncludeExclude (via gsd_exec bash lane)` | 127 | ❌ environment failure: dotnet not found in gsd_exec bash lane | 4633ms |
| 2 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter IncludeExclude` | 0 | ✅ pass: 13/13 IncludeExclude tests | 12000ms |
| 3 | `dotnet test Hymnal.sln` | 1 | ❌ repo mismatch: Hymnal.sln does not exist; checkout uses Hymnal.slnx | 1000ms |
| 4 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj` | 0 | ✅ pass: 292/292 tests | 10000ms |
| 5 | `dotnet test Hymnal.slnx` | 0 | ✅ pass: 292/292 tests | 9000ms |

## Deviations

The task verification command was run successfully through the regular shell because gsd_exec's bash lane could not find dotnet. Full-solution verification used Hymnal.slnx because Hymnal.sln is not present in this checkout.

## Known Issues

The project instructions still reference Hymnal.sln, but the repository currently contains Hymnal.slnx. gsd_exec cannot run dotnet in this environment.

## Files Created/Modified

- `src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs`
- `src/Hymnal.Core/Services/BookTxtStructureService.cs`
- `src/Hymnal/App.axaml.cs`
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`
