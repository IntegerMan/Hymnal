---
id: T01
parent: S03
milestone: M005
key_files:
  - src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs
  - src/Hymnal.Core/Services/BookTxtStructureService.cs
  - src/Hymnal.Core/Services/ChapterRegistryService.cs
  - tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs
key_decisions:
  - Kept `RenameEntryAsync(bookTxtPath, existingPath, replacementPath)` as the canonical rename surface and treated validation of the already-landed implementation as sufficient to close the task.
duration: 
verification_result: passed
completed_at: 2026-06-18T12:15:09.324Z
blocker_discovered: false
---

# T01: Validated the existing rollback-aware BookTxtStructureService rename implementation and focused Core tests for chapter and Part folder UUID-preserving renames.

**Validated the existing rollback-aware BookTxtStructureService rename implementation and focused Core tests for chapter and Part folder UUID-preserving renames.**

## What Happened

Inspected `src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs`, `src/Hymnal.Core/Services/BookTxtStructureService.cs`, `src/Hymnal.Core/Services/ChapterRegistryService.cs`, and `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`. The code already implemented the task contract: `RenameEntryAsync` remains the canonical public surface, chapter renames move the file, rewrite the Book.txt entry, update the first `# ` heading, and preserve chapter-registry identity; Part renames treat the containing folder as the rename unit, rewrite descendant Book.txt entries under the moved prefix, update the Part heading, and reconcile registry current paths for all moved entries. The test suite already covers chapter rename success, Part folder rename success, target conflict rejection before mutation, missing source rejection, case-only rename rejection, rollback-aware failure messaging, and UUID-backed sidecar continuity after a `ManuscriptService.LoadWorkspaceAsync` reload. No source edits were required because the repository state already satisfied the inlined task plan.

## Verification

Ran the focused BookTxtStructureService test suite through the local Windows .NET runtime using the task-specified filter. The suite passed with 42/42 tests green, covering the canonical rename behavior, rollback paths, Book.txt rewrites, heading rewrites, registry continuity, and reload-based metadata preservation. An initial `gsd_exec` attempt failed because the WSL bash environment did not expose `dotnet`; verification was then completed with an explicit `dotnet.exe` path from the repo root.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `"/c/Program Files/dotnet/dotnet.exe" test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter BookTxtStructureServiceTests --verbosity minimal` | 0 | ✅ pass | 6367ms |

## Deviations

No code changes were needed because the planned implementation and tests were already present; the task was completed by validating the existing implementation against the task contract. Verification used an explicit Windows `dotnet.exe` path instead of `gsd_exec` because the local WSL bash environment lacked `dotnet`.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs`
- `src/Hymnal.Core/Services/BookTxtStructureService.cs`
- `src/Hymnal.Core/Services/ChapterRegistryService.cs`
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`
