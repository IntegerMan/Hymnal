---
estimated_steps: 7
estimated_files: 4
skills_used: []
---

# T03: Add rollback aware path moves

Expected executor skills: tdd, verify-before-complete.

Why: Cross-Part Corkboard moves and sidebar path changes must be one Core operation from the caller perspective. Moving the file, rewriting Book.txt, and preserving registry identity separately would allow data loss or silent UUID churn.

Do: Add a dedicated path-changing method to `IBookTxtStructureService` and implement it in `BookTxtStructureService`. The method should accept workspace root or derive it safely from Book.txt, existing relative path, replacement relative path, and target order semantics needed by later UI surfaces. It must validate normalized relative paths, require the source file to exist, fail if the target file already exists, move the markdown file, rewrite Book.txt to replacement path and requested order, update or reconcile registry metadata so the same UUID remains attached to the moved chapter, and remove any stale exclusion for the included replacement. If Book.txt or registry update fails after the file move, attempt to move the file back; still return failure even if rollback succeeds. If rollback fails, return a recoverable failure naming source path, target path, and phase. Prefer preserve-or-fail for UUID continuity when registry state is ambiguous.

Done when: Tests prove cross-Part move success, target conflict failure without writes, Book.txt write failure with successful rollback and explicit failure, simulated rollback failure messaging, exclusion cleanup after move, and UUID continuity across the path change.

Failure modes: source missing, target exists, file move fails, Book.txt write fails after move, registry update fails after Book.txt rewrite, rollback succeeds, rollback fails.
Load profile: one file move, one Book.txt rewrite, one registry JSON write, and optional manifest save per path change.
Negative tests: source missing, target collision, invalid path, Book.txt write failure, rollback failure, ambiguous registry identity, case-only path changes if unsupported by filesystem.

## Inputs

- `src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs`
- `src/Hymnal.Core/Services/BookTxtStructureService.cs`
- `src/Hymnal.Core/Services/ChapterRegistryService.cs`
- `src/Hymnal.Core/Services/ExclusionManifestService.cs`
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`

## Expected Output

- `src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs`
- `src/Hymnal.Core/Services/BookTxtStructureService.cs`
- `src/Hymnal.Core/Services/ChapterRegistryService.cs`
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`

## Verification

dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter PathMove

## Observability Impact

Adds phase-aware rollback failure details in `Result.Fail` messages, including whether rollback was attempted and whether manuscript state was restored.
