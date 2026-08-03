---
estimated_steps: 21
estimated_files: 4
skills_used: []
---

# T01: Added INotesService/NotesService for per-chapter note persistence and exposed WorkspaceRoot seam on WorkspaceViewModel

Why: Notes persistence requires a load/save abstraction and a public workspace-root seam that NotesViewModel can consume. Currently WorkspaceViewModel does not expose WorkspaceRoot and IMetadataStore has no read API.

Do:
1. Create `src/Hymnal.Core/Interfaces/INotesService.cs` with:
   ```csharp
   Task<string> LoadAsync(string absoluteNotesPath);
   Task SaveAsync(string absoluteNotesPath, string content);
   static string DeriveNotesPath(string workspaceRoot, string chapterRelativePath);
   ```
   `DeriveNotesPath` = `Path.Combine(workspaceRoot, ".hymnal-data", "notes", chapterRelativePath.Replace('/', '_').Replace('\\', '_'))`

2. Create `src/Hymnal.Core/Infrastructure/NotesService.cs` implementing `INotesService`:
   - `LoadAsync`: if file missing return `""` (catch FileNotFoundException / check File.Exists); else return `File.ReadAllTextAsync`
   - `SaveAsync`: delegates to `IMetadataStore.WriteTextAtomicAsync`
   - Static helper `DeriveNotesPath` forwarded from interface default or duplicated

3. Expose `WorkspaceRoot` seam: add `public string WorkspaceRoot => _model?.WorkspaceRoot ?? string.Empty;` property to `WorkspaceViewModel` (so NotesViewModel can read it without taking a ManuscriptModel dependency).

4. Create `tests/Hymnal.Core.Tests/Infrastructure/NotesServiceTests.cs` with tests:
   - `LoadAsync_MissingFile_ReturnsEmptyString`
   - `SaveAsync_CreatesFileViaMetadataStore`
   - `DeriveNotesPath_ReplacesSlashesWithUnderscores`
   - `DeriveNotesPath_PlacesUnderHymnalDataNotes`
   Use a real MetadataStore with a temp directory (no mocks needed — MetadataStore has no external deps).

Done when: `dotnet test tests/Hymnal.Core.Tests --filter "NotesService"` exits 0 with ≥4 tests passing.

## Inputs

- `src/Hymnal.Core/Interfaces/IMetadataStore.cs`
- `src/Hymnal.Core/Infrastructure/MetadataStore.cs`
- `src/Hymnal.Core/Models/ChapterNode.cs`
- `src/Hymnal.Core/Models/ManuscriptModel.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `tests/Hymnal.Core.Tests/Infrastructure/MetadataStoreTests.cs`

## Expected Output

- `src/Hymnal.Core/Interfaces/INotesService.cs`
- `src/Hymnal.Core/Infrastructure/NotesService.cs`
- `tests/Hymnal.Core.Tests/Infrastructure/NotesServiceTests.cs`

## Verification

dotnet test tests/Hymnal.Core.Tests --filter "NotesService" --nologo
