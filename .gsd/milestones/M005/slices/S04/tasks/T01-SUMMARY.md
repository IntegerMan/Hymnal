---
id: T01
parent: S04
milestone: M005
key_files:
  - src/Hymnal.Core/Services/BookTxtStructureService.cs
  - tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs
key_decisions:
  - Kept the existing ReorderEntryAsync interface and dispatches Part divider paths to a block move based on the established `{class: part}` validation path.
duration: 
verification_result: passed
completed_at: 2026-06-18T12:29:09.351Z
blocker_discovered: false
---

# T01: Added atomic Part-block reorder support to BookTxtStructureService while preserving chapter single-entry reorder behavior.

**Added atomic Part-block reorder support to BookTxtStructureService while preserving chapter single-entry reorder behavior.**

## What Happened

Implemented a Part-aware branch inside `BookTxtStructureService.ReorderEntryAsync`: source entries that validate as `{class: part}` now move as a contiguous raw Book.txt block from the Part divider through its child entries up to the next Part divider or end of file. The helper rejects out-of-range Part reorder indexes and rejects targets that would insert a Part block into the middle of another Part's chapter entries, returning phase-aware `Result.Fail` messages that name the source Part, requested index, and Book.txt validation/write phase. Existing chapter reorder behavior remains on the original single-line path. Added focused Core tests for moving Part blocks before and after other Part blocks, preserving child entries and blank-line style, rejecting invalid Part-block targets and indexes before writing, and verifying reload order has no duplicates or missing entries.

## Verification

Ran the task-specified targeted Core test command for `BookTxtStructureServiceTests`; it completed successfully with exit code 0. This covers the new Part-block reorder tests plus the existing chapter reorder, add/include/exclude/move/rename/remove/delete Book.txt structure tests in that class.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter BookTxtStructureServiceTests --verbosity minimal"` | 0 | ✅ pass | 1154ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal.Core/Services/BookTxtStructureService.cs`
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`
