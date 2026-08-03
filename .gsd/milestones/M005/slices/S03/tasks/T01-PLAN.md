---
estimated_steps: 9
estimated_files: 5
skills_used: []
---

# T01: Implement canonical rename contracts

Planner context: use skills tdd and verify-before-complete. Why: S03 must not add sidebar-only file I/O. The Core service must be the single consistency boundary for both a single chapter file rename and a Part folder rename before the UI is wired. Active requirement supported: R013. Assumption: the preloaded R010 mapping to M005/S03 is stale or erroneous because AI editorial work is outside the approved S03 sketch; do not implement AI editorial here.

Do:
1. Add focused Core tests in `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs` for chapter rename and Part folder rename. Cover: file path changes, Book.txt rewritten, first `# ` heading updated for display title, target conflict rejects before mutation, missing source rejects, case-only rename rejects consistently, and registry UUID continuity after a fresh `ManuscriptService.LoadWorkspaceAsync` style reload.
2. Extend `src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs` only as needed, but prefer keeping `RenameEntryAsync(bookTxtPath, existingPath, replacementPath)` as the canonical public operation consumed by UI surfaces. Existing Corkboard references must continue compiling.
3. Update `src/Hymnal.Core/Services/BookTxtStructureService.cs` so `RenameEntryAsync` performs rollback-aware filesystem mutation instead of only rewriting Book.txt. For a Chapter entry, move the markdown file to the replacement relative path and update the one Book.txt line. For a Part entry, treat the containing folder as the rename unit: move the folder, update the Part file line plus every active Book.txt entry under that folder prefix, and reconcile registry current paths for all moved entries.
4. Preserve notes, phases, targets, and word-count history by preserving UUIDs in `ChapterRegistryService` records rather than rewriting sidecar data. Tests should seed registry and representative sidecar service data under the original UUID and prove the same UUID is still associated with the replacement path after reload.
5. Use the S01 failure-message pattern: phase-aware `Result.Fail` strings naming path validation, conflict validation, file move, Book.txt write, registry update, and rollback attempted or rollback failed. Do not silently recover as success after rollback.
6. Keep path normalization forward-slash based, reject absolute or traversal paths via existing normalization, reject target conflicts before moving anything, and keep exclusions manifest cleanup consistent with S01 behavior.

Done when: Core tests demonstrate chapter rename, Part folder rename, conflicts, and metadata UUID continuity without any ViewModel or View direct file writes.

## Inputs

- `src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs`
- `src/Hymnal.Core/Services/BookTxtStructureService.cs`
- `src/Hymnal.Core/Services/ChapterRegistryService.cs`
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`

## Expected Output

- `src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs`
- `src/Hymnal.Core/Services/BookTxtStructureService.cs`
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`

## Verification

dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter BookTxtStructureServiceTests --verbosity minimal

## Observability Impact

Improves Core failure visibility by returning phase-aware Result errors for rename validation, filesystem move, Book.txt write, registry update, and rollback status.
