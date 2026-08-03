---
id: T01
parent: S01
milestone: M004
key_files:
  - src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs
  - src/Hymnal.Core/Services/BookTxtStructureService.cs
  - tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs
key_decisions:
  - D019-D021 semantics were implemented as planned: Remove/Exclude only edits Book.txt, Include can add an existing or newly created chapter path, and Delete is the only destructive file-removal path and requires explicit caller intent.
  - Book.txt edits mutate raw lines rather than a rebuilt chapter model so blank lines and part separators are preserved exactly during reorder and insert operations.
duration: 
verification_result: passed
completed_at: 2026-06-04T00:57:52.974Z
blocker_discovered: false
---

# T01: Added an atomic Book.txt structural edit service with normalized reorder, include, rename, create, remove, and delete operations.

**Added an atomic Book.txt structural edit service with normalized reorder, include, rename, create, remove, and delete operations.**

## What Happened

Implemented `IBookTxtStructureService` and `BookTxtStructureService` in Hymnal.Core to centralize all structural Book.txt edits behind a single atomic-write seam. The service now loads and validates normalized Book.txt entries, rejects malformed or traversal paths outside the manuscript root, detects duplicate entries, and performs raw-line Book.txt mutations so blank separators between parts are preserved exactly as authored. Supported operations include reordering existing entries, renaming/replacing a path entry, adding an existing file at an index, adding after a Part divider, creating a new chapter file plus Book.txt entry, removing an entry without deleting the file, and deleting a chapter file only on explicit request. Delete/create paths include best-effort rollback so Book.txt content is restored or the new file is cleaned up if the follow-on step fails. Added focused temp-workspace tests covering within-part reorder, cross-part reorder with blank-line preservation, include at index, include after part, create-new-chapter, rename, remove without file deletion, delete with file deletion, missing Book.txt, duplicate entries, traversal rejection, missing include path, unknown reorder target, missing delete target, and atomic write failure leaving the original Book.txt intact.

## Verification

Ran `dotnet test Hymnal.sln --filter FullyQualifiedName~BookTxtStructureServiceTests --no-restore -v minimal` through PowerShell. The focused test suite passed: 15 tests total, 15 passed, 0 failed. Earlier restore-based attempts hit the known environment NuGet null-path issue, so verification used the no-restore path.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `powershell -NoLogo -NoProfile -Command '$t = Measure-Command { dotnet test Hymnal.sln --filter FullyQualifiedName~BookTxtStructureServiceTests --no-restore -v minimal | Out-Null }; if ($LASTEXITCODE -eq 0) { Write-Output "TEST_PASS" }; Write-Output ("DURATION_MS=" + [int]$t.TotalMilliseconds); exit $LASTEXITCODE'` | 0 | ✅ pass | 5614ms |

## Deviations

Used `--no-restore` for verification because the environment's restore path hits the known NuGet null-path issue; implementation scope remained unchanged.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs`
- `src/Hymnal.Core/Services/BookTxtStructureService.cs`
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`
