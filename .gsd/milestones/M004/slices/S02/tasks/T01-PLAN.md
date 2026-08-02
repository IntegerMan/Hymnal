---
estimated_steps: 10
estimated_files: 4
skills_used: []
---

# T01: Implement supplemental docs tree service with filesystem-safe create and load behavior

Why: R007 needs a separate docs tree rooted at `.hymnal-data/docs/` and D018 explicitly forbids expanding `ChapterNode` or `NodeKind` for docs. This task establishes the Core contract and test coverage before UI wiring. Executor skills frontmatter: `bmad-quick-dev`, `tdd`, `verify-before-complete`.

Do:
1. Add a Core model for supplemental docs, e.g. `SupplementalDocNode`, with enough state for a tree/sidebar item: display name, relative path under docs root using forward slashes, absolute path or resolvable path, node kind/file-vs-folder, children, and stable key.
2. Add an interface/service pair following existing Core patterns, e.g. `ISupplementalDocsService` in `src/Hymnal.Core/Interfaces/` and `SupplementalDocsService` in `src/Hymnal.Core/Infrastructure/` or `src/Hymnal.Core/Services/` consistent with nearby filesystem services.
3. Implement docs root resolution as `<workspaceRoot>/.hymnal-data/docs`; create the root when loading/creating but never scan outside it.
4. Implement load/tree projection for nested folders and files, sorted folders first then files, with deterministic case-insensitive ordering where appropriate.
5. Implement create-folder and create-file operations that validate non-empty names, reject path traversal and rooted paths, keep all writes under docs root, create parent folders as needed, and return `Result<T>`/`Result<Unit>` rather than throwing for expected failures.
6. For creating files, use the existing `IMetadataStore.WriteTextAtomicAsync` path for initial content so the docs lifecycle starts on the same atomic-write abstraction used by editor saves.
7. Add unit tests in `tests/Hymnal.Core.Tests/Services/SupplementalDocsServiceTests.cs` covering load empty root, nested folder/file projection, create folder, create file, traversal rejection, and idempotent reload preserving tree visibility.

Done when: the Core docs service is independently testable, all docs paths remain under `.hymnal-data/docs/`, no docs concepts are added to manuscript `ChapterNode`, and service failures are expressed as `Result.Fail` messages that the UI layer can display.

## Inputs

- `src/Hymnal.Core/Common/Result.cs`
- `src/Hymnal.Core/Common/PathHelper.cs`
- `src/Hymnal.Core/Interfaces/IMetadataStore.cs`
- `src/Hymnal.Core/Infrastructure/MetadataStore.cs`
- `src/Hymnal.Core/Interfaces/INotesService.cs`
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`

## Expected Output

- `src/Hymnal.Core/Models/SupplementalDocNode.cs`
- `src/Hymnal.Core/Interfaces/ISupplementalDocsService.cs`
- `src/Hymnal.Core/Services/SupplementalDocsService.cs`
- `tests/Hymnal.Core.Tests/Services/SupplementalDocsServiceTests.cs`

## Verification

dotnet test Hymnal.sln --filter SupplementalDocsServiceTests

## Observability Impact

Adds clear Result failure messages for invalid doc paths and filesystem failures so the ViewModel can surface exact user-facing errors instead of generic crashes.
