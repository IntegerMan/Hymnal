---
estimated_steps: 24
estimated_files: 3
skills_used: []
---

# T03: IFolderPickerService interface and AppSettingsStore (atomic JSON settings store) implemented with 3 passing tests

Why: AppSettingsStore persists last-opened workspace path across launches. IFolderPickerService is a thin Core interface that lets WorkspaceViewModel trigger a folder picker without taking an Avalonia dependency in Core.

Do:
1. Create `src/Hymnal.Core/Interfaces/IFolderPickerService.cs`:
   ```csharp
   namespace Hymnal.Core.Interfaces;
   public interface IFolderPickerService
   {
       Task<string?> PickFolderAsync();
   }
   ```
2. Create `src/Hymnal/Infrastructure/AppSettingsStore.cs` implementing `IAppSettingsStore`:
   - Storage path: Windows → `%APPDATA%/Hymnal/settings.json`; Linux → `~/.config/hymnal/settings.json`. Use `Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)` (maps to `~/.config` on Linux via .NET runtime).
   - JSON format: flat dictionary `{ "schemaVersion": 1, "values": { "key": <value> } }` with `JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }`.
   - `GetAsync<T>(string key)`: reads file (returns default if not found), deserializes, returns value at key or default.
   - `SetAsync<T>(string key, T value)`: reads current dict (or empty), upserts key, serializes to temp file in same directory, then `File.Move(temp, target, overwrite: true)` — atomic write-temp-rename, never `File.WriteAllText` directly.
   - Ensure parent directory is created before writing.
3. Create `tests/Hymnal.Core.Tests/Infrastructure/AppSettingsStoreTests.cs`:
   - Uses `Path.GetTempPath()` + a unique subdirectory per test for isolation (not the real APPDATA path).
   - Override the storage path via a constructor overload `AppSettingsStore(string settingsPath)` (package-private for tests).
   - `RoundTrip_StringValue`: Set then Get → same value
   - `RoundTrip_NullableType_MissingKey`: Get on empty store → returns null/default
   - `AtomicWrite_DirectoryCreated`: store to non-existent dir → file is created
   - Clean up temp dir in test teardown.

Done when: `dotnet test tests/Hymnal.Core.Tests/ --filter AppSettingsStoreTests` exits 0.

## Inputs

- `src/Hymnal.Core/Interfaces/IAppSettingsStore.cs`
- `src/Hymnal.Core/Common/Result.cs`

## Expected Output

- `src/Hymnal.Core/Interfaces/IFolderPickerService.cs`
- `src/Hymnal/Infrastructure/AppSettingsStore.cs`
- `tests/Hymnal.Core.Tests/Infrastructure/AppSettingsStoreTests.cs`

## Verification

dotnet test tests/Hymnal.Core.Tests/ --filter AppSettingsStoreTests
