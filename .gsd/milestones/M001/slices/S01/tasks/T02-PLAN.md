---
estimated_steps: 1
estimated_files: 8
skills_used: []
---

# T02: Established Hymnal.Core boundary with Result&lt;T&gt;, Unit, three service interfaces, and 4 passing xUnit tests — no Avalonia reference in Core

Why: Hymnal.Core is the zero-UI contract layer that all downstream slices depend on. Establishing it now with compile-enforced boundaries (no Avalonia reference) lets S02-S04 executor agents trust the interface contracts exist. Do: (1) Delete the template-generated Class1.cs from src/Hymnal.Core/. (2) Create src/Hymnal.Core/Common/Result.cs — readonly record struct Result<T>(T? Value, string? Error, bool IsSuccess) with static Ok(T value) and Fail(string error) factory methods. (3) Create src/Hymnal.Core/Common/Unit.cs — readonly struct Unit with static readonly Unit Default singleton. (4) Create src/Hymnal.Core/Interfaces/INotificationService.cs — interface with ShowError(string message), ShowInfo(string message), ShowSuccess(string message). (5) Create src/Hymnal.Core/Interfaces/ICredentialStore.cs — interface stub: Task StoreAsync(string key, string value), Task<string?> RetrieveAsync(string key), Task DeleteAsync(string key). (6) Create src/Hymnal.Core/Interfaces/IAppSettingsStore.cs — interface stub: Task<T?> GetAsync<T>(string key), Task SetAsync<T>(string key, T value) (implemented in S02). (7) Delete the template-generated UnitTest1.cs from tests/Hymnal.Core.Tests/. (8) Create tests/Hymnal.Core.Tests/Common/ResultTests.cs — xUnit tests covering: Result<int>.Ok(42).IsSuccess == true, Result<int>.Ok(42).Value == 42, Result<int>.Fail("err").IsSuccess == false, Result<int>.Fail("err").Error == "err". (9) Create fixture directories: tests/Hymnal.Core.Tests/Fixtures/SampleManuscripts/simple-book/ and multi-part-book/ (both with placeholder Book.txt — one line each: "chapter-one.md" and "part-one/chapter-one.md"). (10) Verify Hymnal.Core has no Avalonia packages: dotnet list src/Hymnal.Core/ package must show no Avalonia entries. Done when: dotnet test tests/Hymnal.Core.Tests/ exits 0 and all ResultTests pass.

## Inputs

- `src/Hymnal.Core/Hymnal.Core.csproj`
- `tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj`

## Expected Output

- `src/Hymnal.Core/Common/Result.cs`
- `src/Hymnal.Core/Common/Unit.cs`
- `src/Hymnal.Core/Interfaces/INotificationService.cs`
- `src/Hymnal.Core/Interfaces/ICredentialStore.cs`
- `src/Hymnal.Core/Interfaces/IAppSettingsStore.cs`
- `tests/Hymnal.Core.Tests/Common/ResultTests.cs`
- `tests/Hymnal.Core.Tests/Fixtures/SampleManuscripts/simple-book/Book.txt`
- `tests/Hymnal.Core.Tests/Fixtures/SampleManuscripts/multi-part-book/Book.txt`

## Verification

dotnet test tests/Hymnal.Core.Tests/ --no-build
