---
estimated_steps: 11
estimated_files: 3
skills_used: []
---

# T01: Add atomic Book.txt structural edit service

Expected executor skills: tdd, verify-before-complete.

Why: The corkboard's structural actions must modify Book.txt atomically and explicitly, instead of scattering raw File writes through ViewModels. This also reconciles the pulled-forward structural-editing scope with the existing parser and metadata-store atomic-write pattern.

Do:
1. Add `IBookTxtStructureService` in Core with operations for reading normalized Book.txt entries, reordering an existing path to a new index, renaming/replacing a path entry, adding an existing path at an index or after a Part, creating a new chapter file plus Book.txt entry, removing a path from Book.txt without deleting the file, and deleting a chapter file only when explicitly requested.
2. Implement `BookTxtStructureService` using `IMetadataStore.WriteTextAtomicAsync` for Book.txt writes. Preserve non-empty line ordering, normalize paths to forward slashes, reject paths outside the manuscript root, and never move files during cross-Part reorder.
3. Apply decisions D019-D021: Exclude/Remove from Book removes only the Book.txt line; Include adds an existing/new file path; Delete removes the markdown file and the Book.txt line and is reserved for a confirmed caller.
4. Add unit tests with temp workspaces covering reorder within and across Part dividers, keeping empty Part lines, create-new-chapter, include existing file, rename path entry, remove without file deletion, delete with file deletion, missing Book.txt, duplicate path rejection, and path traversal rejection.

Failure Modes (Q5): if Book.txt is missing/unreadable return a path-rich failure and do not write; if atomic write fails leave original content intact; if a requested chapter path is malformed/outside the manuscript root reject before touching disk.
Load Profile (Q6): operations are O(number of Book.txt lines) and intended for manuscripts around 100+ chapters; the first breakpoint is UI gesture frequency, so service calls should be single write operations without background loops.
Negative Tests (Q7): empty Book.txt, duplicate chapter lines, nonexistent include path, path traversal (`../`), delete missing file, and reorder unknown path.
Done when: the structural service is covered by tests and no ViewModel needs direct raw Book.txt writes for S01 structural operations.

## Inputs

- `src/Hymnal.Core/Interfaces/IMetadataStore.cs`
- `src/Hymnal.Core/Models/ChapterNode.cs`
- `src/Hymnal.Core/Services/BookTxtParser.cs`
- `tests/Hymnal.Core.Tests/Services/BookTxtParserTests.cs`
- `tests/Hymnal.Core.Tests/Infrastructure/MetadataStoreTests.cs`

## Expected Output

- `src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs`
- `src/Hymnal.Core/Services/BookTxtStructureService.cs`
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`

## Verification

dotnet test Hymnal.sln --filter FullyQualifiedName~BookTxtStructureServiceTests

## Observability Impact

The service returns Result-style failures or throws only unexpected exceptions with path-rich messages; callers can surface the failed operation and path.
