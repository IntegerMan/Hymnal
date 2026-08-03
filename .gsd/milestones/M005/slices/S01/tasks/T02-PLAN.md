---
estimated_steps: 7
estimated_files: 3
skills_used: []
---

# T02: Wire include and exclude structure operations

Expected executor skills: tdd, verify-before-complete.

Why: Book.txt remains the ordered include list, while `exclusions.json` records intentional removals. Later sidebar and Corkboard toggles need a single Core contract that updates both consistently.

Do: Extend `IBookTxtStructureService` and `BookTxtStructureService` with explicit include and exclude operations that consume `IExclusionManifestService`. Excluding an included entry removes it from Book.txt and adds the normalized path to the exclusion manifest. Including an excluded existing file inserts it into Book.txt at the requested index or after a specified Part and removes it from the manifest. Keep existing `AddExistingEntryAsync`, `AddExistingEntryAfterPartAsync`, and `RemoveEntryAsync` behavior compatible, either by delegating from new operations or keeping old methods as non-manifest primitives with clear tests. Reject nonexistent include targets, duplicate Book.txt entries, invalid indices, and paths outside the workspace. Do not persist arbitrary orphans unless the caller invokes the intentional include or exclude operation.

Done when: Core tests prove exclude, include at index, include after Part, duplicate prevention, nonexistent target failure, stale manifest pruning after include or exclude, and backwards-compatible existing reorder and create tests.

Failure modes: Book.txt write failure does not silently update the manifest as if exclusion succeeded; manifest save failure returns an explicit failure after Book.txt state is considered and tests document the chosen order.
Load profile: one Book.txt rewrite plus one small manifest write per include or exclude operation.
Negative tests: duplicate path, missing file, invalid index, Part path not found, manifest save failure, Book.txt write failure.

## Inputs

- `src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs`
- `src/Hymnal.Core/Services/BookTxtStructureService.cs`
- `src/Hymnal.Core/Services/ExclusionManifestService.cs`
- `src/Hymnal.Core/Interfaces/IExclusionManifestService.cs`
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`

## Expected Output

- `src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs`
- `src/Hymnal.Core/Services/BookTxtStructureService.cs`
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`

## Verification

dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter IncludeExclude

## Observability Impact

Extends structure-operation failures with manifest versus Book.txt phase detail for later notification text.
