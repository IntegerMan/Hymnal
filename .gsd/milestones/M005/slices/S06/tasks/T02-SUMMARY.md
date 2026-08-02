---
id: T02
parent: S06
milestone: M005
key_files:
  - src/Hymnal/ViewModels/CorkboardViewModel.cs
  - tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs
key_decisions:
  - Anchored inline-create placement from the prior canonical Book.txt node kind so inserts at index 0, after Parts, and before the next Part render at the correct visual slot.
  - Kept duplicate-path and invalid-create rejection in `IBookTxtStructureService.CreateNewChapterAsync`, with CorkboardViewModel surfacing the proposed path through `LastStructuralError` rather than pre-writing files.
duration: 
verification_result: passed
completed_at: 2026-06-18T22:05:10.336Z
blocker_discovered: false
---

# T02: Added exact inline Corkboard chapter insertion ordering with Part-aware path generation and failure-state coverage.

**Added exact inline Corkboard chapter insertion ordering with Part-aware path generation and failure-state coverage.**

## What Happened

Added focused CorkboardViewModel coverage for inline chapter insertion between chapter cards, before the first Part, immediately after an empty Part divider, and after the last chapter in a Part without crossing into the next Part. Added commit-path tests proving `CommitInlineCreateAsync` delegates to `IBookTxtStructureService.CreateNewChapterAsync` with all-entry Book.txt indexes, generated markdown content, and Part-owned folder prefixes including nested Parts, plus failure coverage that preserves Core as the source of truth for duplicate-path rejection while surfacing `LastStructuralError` as `Create chapter` with the proposed path. Updated `CorkboardViewModel` to resolve inline placeholder placement from the canonical Book.txt node sequence and node kind instead of scanning backward across projected items, which fixed incorrect insertion at index 0 and around `EmptyPartHint` rows.

## Verification

Verified the focused Corkboard view-model suite with `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter CorkboardViewModelTests --verbosity minimal`, which passed 39/39 tests. The assertions cover inline placeholder placement at between-card and Part-adjacent boundaries, `GetInsertIndexAfterChapter` all-entry indexing, `CommitInlineCreateAsync` delegation to `CreateNewChapterAsync`, reload on success, nested Part folder prefixes, and duplicate/conflict failures surfacing through `LastStructuralError`.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter CorkboardViewModelTests --verbosity minimal` | 0 | ✅ pass — 39/39 CorkboardViewModelTests passed | 120000ms |

## Deviations

Used the direct shell `bash` tool for the final test run after `gsd_exec` executed under `/bin/bash` without `dotnet` on PATH in this environment.

## Known Issues

Focused verification still emits pre-existing warnings in unrelated areas, including WorkspaceViewModel CS4014 and existing xUnit analyzer suggestions; no test failures remain.

## Files Created/Modified

- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs`
