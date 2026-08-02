---
estimated_steps: 8
estimated_files: 2
skills_used: []
---

# T01: Make Core reorder Part blocks atomically in Book.txt

Why: existing BookTxtStructureService.ReorderEntryAsync can move a single Book.txt entry, which is enough for chapter line reorder but not enough for dragging a Part divider; a Part reorder must move the Part divider plus its following chapter entries as one contiguous block so the sidebar cannot split a Part from its contents. This advances R013 by preserving canonical Book.txt structure semantics for Part reorder before UI code consumes it.

Expected skills_used frontmatter: tdd, verify-before-complete.

Do:
1. Add failing tests in `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs` for moving a Part block before another Part block, moving a Part block after another Part block, preserving all child chapter entries and blank-line style, rejecting out-of-range indexes, and ensuring the final Book.txt has no duplicate or missing entries.
2. Update `src/Hymnal.Core/Services/BookTxtStructureService.cs` so `ReorderEntryAsync` detects Part divider entries using the established `{class: part}` validation path and moves the contiguous Part block from the source Part until the next Part divider or end of Book.txt.
3. Preserve existing chapter single-entry reorder behavior and existing error messages where possible; for Part-block-specific failures, return explicit Result.Fail messages naming the source Part, requested index, and Book.txt validation phase.
4. Do not add a sidebar-specific write path and do not move chapter files; this task is only about Book.txt line/block order through the canonical structure service.

Done when: Core tests prove a dragged Part moves together with its children, chapter reorder behavior still passes, and invalid Part reorder requests fail before writing Book.txt.

## Inputs

- `src/Hymnal.Core/Services/BookTxtStructureService.cs`
- `src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs`
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`

## Expected Output

- `src/Hymnal.Core/Services/BookTxtStructureService.cs`
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`

## Verification

powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter BookTxtStructureServiceTests --verbosity minimal"

## Observability Impact

Strengthens Core Result.Fail diagnostics for illegal Part reorder requests so later UI and tests can report phase-aware failures instead of opaque invalid Book.txt state.
