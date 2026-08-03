---
estimated_steps: 6
estimated_files: 3
skills_used: []
---

# T01: Extracted PathHelper.IsSamePath, fixed WorkspaceViewModel restore to scan authoritative SourceCache and normalize paths, added 9 passing unit tests

Three bugs compounded the restore-on-relaunch defect:
1. Wrong collection scanned: InitAsync iterated _nodes (UI-bound) instead of _model.Nodes.Items (authoritative SourceCache snapshot).
2. Raw string path comparison: == comparison fails on Windows with mixed casing or separator style. Created PathHelper.cs in Hymnal.Core.Common with IsSamePath(string? a, string? b) using Path.GetFullPath + OrdinalIgnoreCase.
3. SelectedNode assigned before OpenChapterAsync: fixed to assign only after successful reopen.
4. Non-canonical path stored on switch: changed to Path.GetFullPath(absolutePath) before persisting.
Added 9 unit tests in Hymnal.Core.Tests/Common/PathHelperTests.cs.

## Inputs

- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal.Core/Infrastructure/AppSettingsStore.cs`
- `tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj`

## Expected Output

- `src/Hymnal.Core/Common/PathHelper.cs`
- `tests/Hymnal.Core.Tests/Common/PathHelperTests.cs`

## Verification

dotnet test tests/Hymnal.Core.Tests --filter PathHelper --nologo
dotnet build src/Hymnal/Hymnal.csproj -c Debug --nologo
dotnet test tests/Hymnal.Core.Tests --nologo
