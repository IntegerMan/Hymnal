---
estimated_steps: 7
estimated_files: 4
skills_used: []
---

# T01: Add exclusion manifest persistence

Expected executor skills: tdd, verify-before-complete.

Why: Later UI slices need excluded chapters to survive reload, but S01 must separate intentional exclusions from random orphan markdown files. This task creates the Core manifest persistence seam before wiring Book.txt operations into it.

Do: Add a schema-versioned exclusion manifest model and service in Core. Persist under `.hymnal-data/exclusions.json` through `IMetadataStore.WriteTextAtomicAsync()`. Normalize paths to forward slashes, compare case-insensitively, reject absolute or traversal paths, and tolerate missing or malformed manifests with explicit `Result` failures where appropriate. Loading should ignore stale entries whose files no longer exist without writing during passive load; saving after a mutation should prune stale entries. Keep `OrphanFileDiscoveryService` behavior as discovery only and do not auto-adopt orphans into the manifest.

Done when: Unit tests prove round-trip save/load, duplicate normalization, stale-entry tolerance, mutation-time pruning, malformed JSON handling, invalid path rejection, and that orphan discovery remains independent of intentional exclusions.

Failure modes: malformed JSON returns a recoverable failure; atomic save failure returns failure without partially mutating in-memory caller state; missing stale files are omitted from load results and pruned only on save.
Load profile: one small JSON file per workspace, O(n) over excluded paths.
Negative tests: empty manifest, duplicate case variants, missing file, absolute path, `..` traversal, malformed JSON.

## Inputs

- `src/Hymnal.Core/Interfaces/IMetadataStore.cs`
- `src/Hymnal.Core/Infrastructure/MetadataStore.cs`
- `src/Hymnal.Core/Services/OrphanFileDiscoveryService.cs`
- `src/Hymnal.Core/Common/Result.cs`
- `src/Hymnal.Core/Common/Unit.cs`

## Expected Output

- `src/Hymnal.Core/Models/ExclusionManifest.cs`
- `src/Hymnal.Core/Interfaces/IExclusionManifestService.cs`
- `src/Hymnal.Core/Services/ExclusionManifestService.cs`
- `tests/Hymnal.Core.Tests/Services/ExclusionManifestServiceTests.cs`

## Verification

dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter ExclusionManifestServiceTests

## Observability Impact

Adds failure messages for manifest load/save, invalid paths, and stale pruning so later UI code can surface actionable exclusion-state errors.
