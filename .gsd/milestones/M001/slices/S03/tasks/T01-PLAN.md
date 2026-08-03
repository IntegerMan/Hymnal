---
estimated_steps: 9
estimated_files: 6
skills_used: []
---

# T01: Added WorkspaceRoot/ManuscriptRoot to ManuscriptModel, wired SetRoots in ManuscriptService, and delivered IMetadataStore/MetadataStore atomic-write implementation with 3/3 passing unit tests registered in DI.

Why: ManuscriptService already computes manuscriptRoot when locating Book.txt but discards it before returning. Without root paths in the model, EditorViewModel cannot resolve absolute chapter file paths for load/save/watch operations regardless of whether Book.txt is at workspace root or in manuscript/. IMetadataStore/MetadataStore provides the reusable atomic text-write seam used by chapter saves in S03 and notes persistence in S04.

Do:
1. Add `WorkspaceRoot` (string) and `ManuscriptRoot` (string) auto-properties to `ManuscriptModel`. Add a `void SetRoots(string workspaceRoot, string manuscriptRoot)` method so ManuscriptService can populate them after loading nodes.
2. Update `ManuscriptService.LoadWorkspaceAsync` to call `model.SetRoots(folderPath, manuscriptRoot)` before returning `Result.Ok(model)`. folderPath is already the method parameter; manuscriptRoot is the local variable already computed when Book.txt is found.
3. Create `src/Hymnal.Core/Interfaces/IMetadataStore.cs` with a single method: `Task WriteTextAtomicAsync(string absolutePath, string content)`.
4. Create `src/Hymnal.Core/Infrastructure/MetadataStore.cs` implementing IMetadataStore: (a) ensure parent directory exists via `Directory.CreateDirectory(dir)`, (b) write content to a temp file in the same directory as the target via `Path.Combine(dir, Path.GetRandomFileName())`, (c) `File.Move(tempPath, absolutePath, overwrite: true)` to complete atomically. Use `File.WriteAllTextAsync` for the temp write.
5. Create `tests/Hymnal.Core.Tests/Infrastructure/MetadataStoreTests.cs` with three xUnit test methods using a temp directory (Path.GetTempPath + Path.GetRandomFileName): `WriteTextAtomicAsync_CreatesFile`, `WriteTextAtomicAsync_CreatesParentDirectory` (target inside a non-existent subdirectory), `WriteTextAtomicAsync_OverwritesExistingFile`.
6. Register `IMetadataStore` and `MetadataStore` as singleton in `src/Hymnal/App.axaml.cs`.

Done when: MetadataStoreTests pass. ManuscriptModel has WorkspaceRoot and ManuscriptRoot properties. ManuscriptService sets them. App.axaml.cs registers IMetadataStore/MetadataStore.

## Inputs

- `src/Hymnal.Core/Models/ManuscriptModel.cs`
- `src/Hymnal.Core/Services/ManuscriptService.cs`
- `src/Hymnal.Core/Infrastructure/AppSettingsStore.cs`
- `src/Hymnal/App.axaml.cs`

## Expected Output

- `src/Hymnal.Core/Models/ManuscriptModel.cs`
- `src/Hymnal.Core/Services/ManuscriptService.cs`
- `src/Hymnal.Core/Interfaces/IMetadataStore.cs`
- `src/Hymnal.Core/Infrastructure/MetadataStore.cs`
- `tests/Hymnal.Core.Tests/Infrastructure/MetadataStoreTests.cs`
- `src/Hymnal/App.axaml.cs`

## Verification

dotnet test tests/Hymnal.Core.Tests/ --filter "MetadataStore"

## Observability Impact

MetadataStore uses standard file I/O with no internal logging; any IOExceptions propagate as task results to the caller (EditorViewModel) which routes them through INotificationService.
