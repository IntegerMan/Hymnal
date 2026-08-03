---
id: T01
parent: S01
milestone: M005
key_files:
  - src/Hymnal.Core/Models/ExclusionManifest.cs
  - src/Hymnal.Core/Interfaces/IExclusionManifestService.cs
  - src/Hymnal.Core/Services/ExclusionManifestService.cs
  - tests/Hymnal.Core.Tests/Services/ExclusionManifestServiceTests.cs
key_decisions:
  - Use schema-versioned JSON at `.hymnal-data/exclusions.json` for intentional exclusions.
  - Keep exclusion persistence stateless; return Result failures instead of throwing for malformed JSON, invalid paths, and save failures.
  - Passive load omits stale entries without rewriting; save/mutations prune stale entries.
duration: 
verification_result: mixed
completed_at: 2026-06-18T03:00:01.000Z
blocker_discovered: false
---

# T01: Added schema-versioned exclusion manifest persistence with atomic saves, path normalization, stale pruning, and focused Core tests.

**Added schema-versioned exclusion manifest persistence with atomic saves, path normalization, stale pruning, and focused Core tests.**

## What Happened

Created `ExclusionManifest`, `IExclusionManifestService`, and `ExclusionManifestService` in Core. The service persists `.hymnal-data/exclusions.json` via `IMetadataStore.WriteTextAtomicAsync()`, normalizes paths to forward slashes, rejects absolute and traversal paths, de-duplicates case variants, omits stale entries during passive load without rewriting, and prunes stale entries during save or exclude/include mutations. Added unit tests covering round-trip persistence, empty and missing manifests, duplicate normalization, stale-entry load tolerance, mutation-time pruning, malformed JSON failure, invalid paths, atomic save failure without caller manifest mutation, include removal, and OrphanFileDiscoveryService independence.

## Verification

Attempted the required focused verification command `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter ExclusionManifestServiceTests` through the available Windows dotnet host. Test execution was blocked before compilation by the known harness/NuGet environment failure `Value cannot be null. (Parameter 'path1')`. A source-level verification script then confirmed all four expected files exist and contain the service/test coverage markers for the required behavior, with 11 Fact tests and 1 Theory test present.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter ExclusionManifestServiceTests` | 1 | ❌ blocked by harness NuGet path1 failure before tests ran | 3229ms |
| 2 | `cmd.exe /C "dotnet --info"` | 1 | ⚠️ diagnostic confirms dotnet host available but environment reports errors | 617ms |
| 3 | `node source-level verification of exclusion manifest files and test coverage markers` | 0 | ✅ pass | 283ms |

## Deviations

The planned dotnet test command could not complete because the execution harness cannot run this project through Windows dotnet/NuGet from the current shell; this matches the injected project gotcha. No product-scope deviations.

## Known Issues

Focused .NET tests were not executable in this harness due to NuGet restore/assets failure: `Value cannot be null. (Parameter 'path1')`. The code and tests should be run in a normal developer shell outside this WSL-to-Windows dotnet invocation issue.

## Files Created/Modified

- `src/Hymnal.Core/Models/ExclusionManifest.cs`
- `src/Hymnal.Core/Interfaces/IExclusionManifestService.cs`
- `src/Hymnal.Core/Services/ExclusionManifestService.cs`
- `tests/Hymnal.Core.Tests/Services/ExclusionManifestServiceTests.cs`
