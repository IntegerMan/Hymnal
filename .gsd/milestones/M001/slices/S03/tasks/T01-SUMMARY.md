---
id: T01
parent: S03
milestone: M001
key_files:
  - src/Hymnal.Core/Models/ManuscriptModel.cs
  - src/Hymnal.Core/Services/ManuscriptService.cs
  - src/Hymnal.Core/Interfaces/IMetadataStore.cs
  - src/Hymnal.Core/Infrastructure/MetadataStore.cs
  - tests/Hymnal.Core.Tests/Infrastructure/MetadataStoreTests.cs
  - src/Hymnal/App.axaml.cs
key_decisions:
  - MetadataStore temp file placed in same directory as target so File.Move is intra-volume (atomic on POSIX, near-atomic on Windows NTFS)
  - Best-effort temp-file cleanup on exception prevents stale .tmp files accumulating on failed writes
duration: 
verification_result: passed
completed_at: 2026-05-29T15:48:35.157Z
blocker_discovered: false
---

# T01: Added WorkspaceRoot/ManuscriptRoot to ManuscriptModel, wired SetRoots in ManuscriptService, and delivered IMetadataStore/MetadataStore atomic-write implementation with 3/3 passing unit tests registered in DI.

**Added WorkspaceRoot/ManuscriptRoot to ManuscriptModel, wired SetRoots in ManuscriptService, and delivered IMetadataStore/MetadataStore atomic-write implementation with 3/3 passing unit tests registered in DI.**

## What Happened

Reviewed all input files before editing. ManuscriptModel received two auto-properties (WorkspaceRoot, ManuscriptRoot) and a SetRoots(workspaceRoot, manuscriptRoot) method. ManuscriptService.LoadWorkspaceAsync now calls model.SetRoots(folderPath, manuscriptRoot) immediately after model.Load(nodes), using the local variable already computed when Book.txt was located — no logic change needed. IMetadataStore.cs declares a single WriteTextAtomicAsync method. MetadataStore.cs implements it: ensures parent directory exists, writes to a temp file in the same directory (Path.GetRandomFileName), then File.Move with overwrite:true; on exception it best-effort deletes the temp file. Three xUnit tests (creates file, creates parent directory, overwrites existing) all pass. App.axaml.cs gained AddSingleton&lt;IMetadataStore, MetadataStore&gt; under the S03 comment block — namespaces were already imported.

## Verification

Ran: dotnet test tests/Hymnal.Core.Tests/ --filter "MetadataStore" — exit code 0, 3 passed, 0 failed, 0 skipped, 459 ms. Build output confirms Hymnal.Core.dll and Hymnal.Core.Tests.dll compiled successfully.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test tests/Hymnal.Core.Tests/ --filter "MetadataStore"` | 0 | ✅ pass — 3 passed, 0 failed | 13000ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal.Core/Models/ManuscriptModel.cs`
- `src/Hymnal.Core/Services/ManuscriptService.cs`
- `src/Hymnal.Core/Interfaces/IMetadataStore.cs`
- `src/Hymnal.Core/Infrastructure/MetadataStore.cs`
- `tests/Hymnal.Core.Tests/Infrastructure/MetadataStoreTests.cs`
- `src/Hymnal/App.axaml.cs`
