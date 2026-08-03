---
id: T03
parent: S06
milestone: M005
key_files:
  - src/Hymnal/Views/CorkboardView.axaml
  - src/Hymnal/Views/CorkboardView.axaml.cs
  - tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs
key_decisions:
  - Kept CorkboardView code-behind as a thin adapter by extracting deterministic static routing helpers for include and inline-create menu actions instead of moving structural logic out of the ViewModel contract.
  - Updated card-level include-existing routing to use CorkboardViewModel.GetInsertIndexAfterChapter so Book.txt insertion indexes follow canonical all-entry ordering rather than chapter-only projection order.
duration: 
verification_result: passed
completed_at: 2026-06-18T22:14:04.897Z
blocker_discovered: false
---

# T03: Added excluded-card corkboard styling plus thin, testable menu-routing helpers with smoke coverage for include and inline chapter insertion flows.

**Added excluded-card corkboard styling plus thin, testable menu-routing helpers with smoke coverage for include and inline chapter insertion flows.**

## What Happened

Updated `src/Hymnal/Views/CorkboardView.axaml` so excluded cards have a clearer muted/tinted treatment with an explicit EXCLUDED badge and helper text, while preserving the existing context-menu affordances for included and excluded cards. Refactored `src/Hymnal/Views/CorkboardView.axaml.cs` to route card, part, and board insertion/include actions through small deterministic helper methods: board-level new chapter uses `GetBookChapterInsertIndex`, part-level new chapter uses `GetInsertIndexAfterPart`, chapter-level inline insert uses `GetInsertIndexAfterChapter`, excluded-card include resolves to either `PartPath` or the canonical book-level insert index, and card-level include-existing now uses `GetInsertIndexAfterChapter` instead of a chapter-only helper. Extended `tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs` to assert the XAML exposes the include/exclude/new-chapter affordances and excluded styling text, that non-chapter items remain invalid drag sources, and that the new routing helpers produce the expected `IncludeExistingChapterRequest` and inline-create placement data for chapter, part, and board flows without opening dialogs.

## Verification

Ran the targeted Corkboard smoke suite against the updated view, code-behind, and helper tests. The suite passed 7/7 tests, confirming the XAML affordances, excluded-card include routing, canonical insertion index helpers, and drag-source guards for excluded and inline-create items.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `powershell.exe -NoProfile -Command "Set-Location 'C:\Dev\Hymnal'; dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter CorkboardViewSmokeTests --verbosity minimal; exit $LASTEXITCODE"` | 0 | ✅ pass | 7923ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal/Views/CorkboardView.axaml`
- `src/Hymnal/Views/CorkboardView.axaml.cs`
- `tests/Hymnal.Core.Tests/Views/CorkboardViewSmokeTests.cs`
