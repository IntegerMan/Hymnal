---
estimated_steps: 10
estimated_files: 2
skills_used: []
---

# T02: Complete inline chapter insertion semantics around chapters and Parts

Skills expected: tdd, design-an-interface, verify-before-complete.

Why: The sketch specifically requires new chapter insertion between cards and around Part dividers. Prior Corkboard scaffolding includes BeginInlineCreate, CommitInlineCreateAsync, CreateChapterCommand, and index helpers; this task should make their order semantics exact, test-backed, and aligned with the same all-Book.txt-entry indexing used by S05 drag reorder.

Do:
1. Add ViewModel tests for BeginInlineCreate inserting a temporary InlineCreateItemViewModel at the correct visual position when the Book.txt index is between two chapter cards.
2. Add tests for GetInsertIndexAfterChapter and CommitInlineCreateAsync so a new chapter inserted between two existing chapters calls CreateNewChapterAsync with the expected all-entry Book.txt index and generated markdown content.
3. Add tests for Part boundaries: inserting before the first Part using the board-level new chapter action/index helper; inserting immediately after a Part divider into an empty Part; and inserting after the last chapter inside a non-empty Part without crossing into the next Part.
4. Verify generated paths use the owning Part folder prefix, including nested Part paths, and reject or surface duplicate path conflicts via the Core Result.Fail path rather than preemptively writing files in the ViewModel.
5. If slug generation can collide with an existing file, preserve Core as the source of truth for rejection and ensure LastStructuralError names Create chapter and the proposed path.
6. Do not expand scope into rich chapter templates, manual filename editing beyond existing advanced dialog behavior, or desktop UAT; this slice only needs insertion order, Core routing, and reload persistence.

Done when: Focused tests prove inline insertion computes correct Book.txt indexes for between-card and Part-adjacent cases, delegates creation to IBookTxtStructureService.CreateNewChapterAsync, reloads on success, and exposes duplicate/conflict failures.

## Inputs

- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs`
- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs`
- `.gsd/milestones/M005/slices/S05/S05-SUMMARY.md`

## Expected Output

- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/CorkboardViewModelTests.cs`

## Verification

dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter CorkboardViewModelTests --verbosity minimal

## Observability Impact

Adds explicit failure-state coverage for Create chapter invalid/duplicate path results through LastStructuralError.
