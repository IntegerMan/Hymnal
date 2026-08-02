---
id: T01
parent: S04
milestone: M001
key_files:
  - src/Hymnal.Core/Interfaces/INotesService.cs
  - src/Hymnal.Core/Infrastructure/NotesService.cs
  - src/Hymnal/ViewModels/WorkspaceViewModel.cs
  - tests/Hymnal.Core.Tests/Infrastructure/NotesServiceTests.cs
key_decisions:
  - INotesService.DeriveNotesPath implemented as a static interface member (C# 8 feature) so both the interface and the concrete class can expose the helper without duplication
  - NotesService.LoadAsync uses File.Exists guard rather than catch FileNotFoundException for clarity
duration: 
verification_result: passed
completed_at: 2026-05-29T20:13:10.302Z
blocker_discovered: false
---

# T01: Added INotesService/NotesService for per-chapter note persistence and exposed WorkspaceRoot seam on WorkspaceViewModel

**Added INotesService/NotesService for per-chapter note persistence and exposed WorkspaceRoot seam on WorkspaceViewModel**

## What Happened

Created INotesService interface with LoadAsync, SaveAsync, and a static DeriveNotesPath helper (using interface static members, .NET 8+). Implemented NotesService delegating writes to IMetadataStore for atomic safety and reading directly from disk (returns "" when file missing). Added WorkspaceRoot property to WorkspaceViewModel forwarding _model?.WorkspaceRoot. Wrote 5 xUnit tests covering: missing-file empty return, save-via-store, load existing, DeriveNotesPath slash replacement, and DeriveNotesPath directory placement.

## Verification

dotnet test tests/Hymnal.Core.Tests --filter "NotesService" --nologo → 5 passed, 0 failed. dotnet build src/Hymnal/Hymnal.csproj -c Debug --nologo → 0 errors, 0 warnings.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test tests/Hymnal.Core.Tests --filter "NotesService" --nologo` | 0 | ✅ pass | 15000ms |
| 2 | `dotnet build src/Hymnal/Hymnal.csproj -c Debug --nologo` | 0 | ✅ pass | 8750ms |

## Deviations

Added a 5th test (LoadAsync_ExistingFile_ReturnsContent) beyond the 4 specified — covers the positive read path for completeness.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal.Core/Interfaces/INotesService.cs`
- `src/Hymnal.Core/Infrastructure/NotesService.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `tests/Hymnal.Core.Tests/Infrastructure/NotesServiceTests.cs`
