---
id: T04
parent: S01
milestone: M005
key_files:
  - src/Hymnal/ViewModels/WorkspaceViewModel.cs
  - src/Hymnal/ViewModels/CorkboardViewModel.cs
  - tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs
key_decisions:
  - UI intentional include/remove Book.txt structure operations must use the manifest-aware IncludeExistingEntry*/ExcludeEntryAsync methods rather than raw AddExistingEntry*/RemoveEntryAsync helpers.
duration: 
verification_result: mixed
completed_at: 2026-06-18T03:22:58.755Z
blocker_discovered: false
---

# T04: Closed the Core Book.txt structure contract by wiring UI include/remove flows through manifest-aware methods and adding a broad Core contract test for exclude, include, move, registry, manifest, and reload reads.

**Closed the Core Book.txt structure contract by wiring UI include/remove flows through manifest-aware methods and adding a broad Core contract test for exclude, include, move, registry, manifest, and reload reads.**

## What Happened

Reviewed the Core structure service contract, Avalonia DI composition, WorkspaceViewModel, CorkboardViewModel, and existing BookTxtStructureService tests. App.axaml.cs already compiles against the updated BookTxtStructureService constructor because IMetadataStore, IExclusionManifestService, and ChapterRegistryService are all registered, so no DI change was needed. Updated WorkspaceViewModel and CorkboardViewModel so intentional include operations call IncludeExistingEntryAsync/IncludeExistingEntryAfterPartAsync and intentional remove-from-book operations call ExcludeEntryAsync, preserving UI behavior while ensuring the exclusion manifest is updated through the public Core contract. Added a broad integration-style Core test that creates a realistic Part-based workspace with Book.txt, existing exclusions, chapter registry UUID state, then exercises ExcludeEntryAsync, IncludeExistingEntryAfterPartAsync, MoveEntryAsync, and ReadNormalizedEntriesAsync. The test verifies final Book.txt ordering, moved file state, registry UUID continuity, manifest pruning/removal behavior, and reload-style normalized reads. Also ran a diagnostic scan confirming the touched tests do not reference .gsd, .planning, or .audits paths and the ViewModels no longer contain legacy AddExistingEntry/RemoveEntry structural calls.

## Verification

Ran diagnostics for stale UI calls and planning-directory references, then ran the full Core test project and desktop app build. gsd_exec could not run dotnet verification because it hit the known project environment issue where Windows dotnet through the gsd_exec bash lane fails NuGet restore with `Value cannot be null. (Parameter 'path1')`; the same required verification commands were run through the regular shell and passed. `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo` passed 301 tests with 0 failures. `dotnet build src/Hymnal/Hymnal.csproj --nologo` succeeded with 0 warnings and 0 errors.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `python3 diagnostic scan for legacy ViewModel AddExistingEntry/RemoveEntry calls and .gsd/.planning/.audits references in touched tests` | 0 | ✅ pass | 4234ms |
| 2 | `cmd.exe /c "dotnet test tests\\Hymnal.Core.Tests\\Hymnal.Core.Tests.csproj --nologo" via gsd_exec` | 1 | ⚠️ known gsd_exec/dotnet environment failure before tests | 3645ms |
| 3 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo` | 0 | ✅ pass — 301 passed, 0 failed | 5000ms |
| 4 | `dotnet build src/Hymnal/Hymnal.csproj --nologo` | 0 | ✅ pass — build succeeded with 0 warnings, 0 errors | 2800ms |

## Deviations

The task requested verification via `dotnet test ... && dotnet build ...`; the gsd_exec lane failed with the known NuGet path issue, so the same checks were rerun successfully through the regular shell. App.axaml.cs was reviewed but not modified because existing DI registrations already satisfy the updated constructor.

## Known Issues

The known gsd_exec/dotnet environment issue remains: invoking Windows dotnet through the gsd_exec bash lane fails restore with `Value cannot be null. (Parameter 'path1')`. Regular shell dotnet verification passes.

## Files Created/Modified

- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/ViewModels/CorkboardViewModel.cs`
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`
