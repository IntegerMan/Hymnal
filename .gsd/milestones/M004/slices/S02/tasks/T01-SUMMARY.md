---
id: T01
parent: S02
milestone: M004
key_files:
  - src/Hymnal.Core/Models/SupplementalDocNode.cs
  - src/Hymnal.Core/Interfaces/ISupplementalDocsService.cs
  - src/Hymnal.Core/Services/SupplementalDocsService.cs
  - tests/Hymnal.Core.Tests/Services/SupplementalDocsServiceTests.cs
key_decisions:
  - Supplemental docs use separate SupplementalDocNode/SupplementalDocNodeKind types and do not expand manuscript ChapterNode/NodeKind.
  - Create-file operations delegate initial content writes to IMetadataStore.WriteTextAtomicAsync to preserve the shared atomic save abstraction.
duration: 
verification_result: passed
completed_at: 2026-06-04T04:22:51.577Z
blocker_discovered: false
---

# T01: Added an independently testable Core supplemental docs tree service rooted at `.hymnal-data/docs/` with safe create/load behavior.

**Added an independently testable Core supplemental docs tree service rooted at `.hymnal-data/docs/` with safe create/load behavior.**

## What Happened

Created `SupplementalDocNode` and `SupplementalDocNodeKind` to represent docs sidebar tree items without extending manuscript `ChapterNode` or `NodeKind`. Added `ISupplementalDocsService` with load, create-folder, and create-file operations, plus `SupplementalDocsService` in Core Services. The service resolves docs under `<workspace>/.hymnal-data/docs`, creates the docs root on load/create, projects deterministic folder-first/file-second trees with case-insensitive ordering, skips reparse-point entries while loading, validates parent-relative paths and leaf names, rejects traversal/rooted paths, and returns `Result.Fail` messages for expected validation/filesystem failures. File creation uses `IMetadataStore.WriteTextAtomicAsync` for initial content so docs start on the same atomic-write path used by editor saves. Added focused xUnit coverage for empty root loading, nested projection and sorting, folder/file creation, traversal/rooted path rejection, and reload persistence with intact content.

## Verification

Ran the focused test suite with `dotnet test Hymnal.sln --filter SupplementalDocsServiceTests`; it built the solution and passed 9/9 SupplementalDocsService tests. I also attempted the required gsd_exec lane first, but the bash subprocess cannot run `dotnet` directly and reproduces the known WSL `dotnet.exe` NuGet `path1` issue, so the accepted verification evidence is the direct harness command with real test output and exit code 0.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test Hymnal.sln --filter SupplementalDocsServiceTests` | 0 | ✅ pass (9 tests passed) | 5311ms |

## Deviations

The task referenced `src/Hymnal.Core/Infrastructure/NotesService.cs` for nearby service patterns but expected `src/Hymnal.Core/Services/SupplementalDocsService.cs`; the supplemental docs implementation follows the expected output path under Services.

## Known Issues

The gsd_exec bash lane cannot provide usable .NET verification in this environment: `dotnet` is not on PATH, and `/mnt/c/Program Files/dotnet/dotnet.exe` hits the known NuGet `Value cannot be null. (Parameter 'path1')` restore issue. Direct harness `dotnet test` succeeds.

## Files Created/Modified

- `src/Hymnal.Core/Models/SupplementalDocNode.cs`
- `src/Hymnal.Core/Interfaces/ISupplementalDocsService.cs`
- `src/Hymnal.Core/Services/SupplementalDocsService.cs`
- `tests/Hymnal.Core.Tests/Services/SupplementalDocsServiceTests.cs`
