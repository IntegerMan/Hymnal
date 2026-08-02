---
id: T01
parent: S05
milestone: M001
key_files:
  - src/Hymnal.Core/Common/PathHelper.cs
  - src/Hymnal/ViewModels/WorkspaceViewModel.cs
  - tests/Hymnal.Core.Tests/Common/PathHelperTests.cs
key_decisions:
  - Used 'using PathHelper = Hymnal.Core.Common.PathHelper;' alias instead of full namespace import to avoid Unit type ambiguity with System.Reactive.Unit
  - SelectedNode assignment moved inside try block after OpenChapterAsync succeeds to prevent sidebar-selected/editor-blank mismatch on restore failure
duration: 
verification_result: passed
completed_at: 2026-05-30T03:00:01.274Z
blocker_discovered: false
---

# T01: Extracted PathHelper.IsSamePath, fixed WorkspaceViewModel restore to scan authoritative SourceCache and normalize paths, added 9 passing unit tests

**Extracted PathHelper.IsSamePath, fixed WorkspaceViewModel restore to scan authoritative SourceCache and normalize paths, added 9 passing unit tests**

## What Happened

Three compounding bugs caused the restore-on-relaunch defect. First, InitAsync iterated _nodes (the UI-bound ObservableCollectionExtended) instead of _model.Nodes.Items (the authoritative SourceCache snapshot), making restore timing-dependent. Second, raw == string comparison between stored and candidate path fails on Windows with mixed casing or separator style from the folder picker. Third, SelectedNode was assigned before OpenChapterAsync succeeded, leaving the sidebar selected while the editor was blank on failure. Created PathHelper.cs in Hymnal.Core.Common with IsSamePath(string? a, string? b) using Path.GetFullPath + OrdinalIgnoreCase. Updated InitAsync to scan _model.Nodes.Items and use PathHelper.IsSamePath. Moved SelectedNode assignment inside the try block after OpenChapterAsync succeeds. Changed TrySwitchChapterAsync to store Path.GetFullPath(absolutePath). Used a type alias (using PathHelper = Hymnal.Core.Common.PathHelper) to avoid Unit type ambiguity. Added 9 unit tests covering same-path cases, case insensitivity, cross-separator normalization, relative segment collapse, and null/empty inputs.

## Verification

dotnet test tests/Hymnal.Core.Tests --filter PathHelper --nologo: 9 passed. dotnet build src/Hymnal/Hymnal.csproj -c Debug --nologo: 0 errors. dotnet test tests/Hymnal.Core.Tests --nologo: 31 passed, 0 failed.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test tests/Hymnal.Core.Tests --filter PathHelper --nologo` | 0 | ✅ pass | 10000ms |
| 2 | `dotnet build src/Hymnal/Hymnal.csproj -c Debug --nologo` | 0 | ✅ pass | 6000ms |
| 3 | `dotnet test tests/Hymnal.Core.Tests --nologo` | 0 | ✅ pass — 31 passed, 0 failed | 4000ms |

## Deviations

Used a type alias (using PathHelper = Hymnal.Core.Common.PathHelper;) instead of a full namespace import (using Hymnal.Core.Common;) to resolve Unit type ambiguity. Added a 5th theory case (null/empty inputs) beyond the 4 required test cases.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal.Core/Common/PathHelper.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `tests/Hymnal.Core.Tests/Common/PathHelperTests.cs`
