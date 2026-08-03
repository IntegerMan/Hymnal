# S01: Atomic structure core and exclusion manifest

**Goal:** Deliver the Core storage and operation contract for persistent exclusions, atomic Book.txt structural writes, rollback-aware cross-Part file moves, and UUID-safe path-changing operations.
**Demo:** After this, automated Core tests can exclude and include files, move a chapter across Part folders, force a Book.txt write failure, and prove the filesystem and Book.txt recover without silent partial state.

## Must-Haves

- `.hymnal-data/exclusions.json` is schema-versioned, forward-slash normalized, load-tolerant of missing files, and pruned on the next user-driven save.
- Exclude and include operations route through `IBookTxtStructureService`, update Book.txt and the exclusion manifest atomically enough for caller-visible consistency, and do not persist random orphan files as intentional exclusions.
- Path-changing moves fail on target conflicts, coordinate filesystem move plus Book.txt rewrite, attempt rollback after Book.txt failure, and return explicit phase-aware failures even when rollback succeeds.
- UUID-backed registry identity is preserved for unambiguous path changes or the operation fails before silently creating a new identity.
- Core tests exercise success paths, stale exclusions, include/exclude semantics, cross-Part move success, target conflict failure, write failure rollback, rollback failure messaging, and UUID continuity.
- Threat Surface Q3: user-controlled relative paths reach filesystem writes; implementation must constrain paths to workspace-relative normalized markdown entries and reject traversal or absolute paths. No secrets or network data are involved.
- Requirement Impact Q4: covers R013 primary for canonical structural editing foundations and supports existing manuscript persistence promises around Book.txt, registry metadata, and atomic writes.
- Failure Modes Q5: filesystem move errors, atomic write errors, malformed manifest JSON, target collisions, stale paths, and registry ambiguity must become `Result.Fail` messages with enough path and phase detail for later UI notifications.
- Load Profile Q6: operations are local filesystem reads and writes over Book.txt plus small JSON metadata; expected manuscripts are small to moderate, but tests should avoid algorithms that repeatedly rewrite per chapter beyond the one structural operation.
- Negative Tests Q7: include missing files, duplicate entries, target conflict, malformed manifest, failed Book.txt write after move, failed rollback, traversal or absolute paths, and ambiguous UUID continuity.

## Proof Level

- This slice proves: Contract and integration proof through Core unit tests plus app compile check. Real desktop runtime and UAT are deferred to later UI slices.

## Integration Closure

This slice closes the Core multi-resource consistency boundary consumed by later sidebar, Corkboard, and Gantt slices. It intentionally does not add UI commands; later slices must call the new `IBookTxtStructureService` methods and use existing workspace reload or watcher suppression orchestration.

## Verification

- Failure observability is via explicit `Result.Fail` messages that name operation, affected path, and failure phase such as manifest save, file move, Book.txt write, registry update, rollback attempted, or rollback failed. No runtime logging or telemetry is required for this Core-only slice.

## Tasks

- [x] **T01: Add exclusion manifest persistence** `est:2h`
  Expected executor skills: tdd, verify-before-complete.
  - Files: `src/Hymnal.Core/Models/ExclusionManifest.cs`, `src/Hymnal.Core/Interfaces/IExclusionManifestService.cs`, `src/Hymnal.Core/Services/ExclusionManifestService.cs`, `tests/Hymnal.Core.Tests/Services/ExclusionManifestServiceTests.cs`
  - Verify: dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter ExclusionManifestServiceTests

- [x] **T02: Wire include and exclude structure operations** `est:2h`
  Expected executor skills: tdd, verify-before-complete.
  - Files: `src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs`, `src/Hymnal.Core/Services/BookTxtStructureService.cs`, `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`
  - Verify: dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter IncludeExclude

- [x] **T03: Add rollback aware path moves** `est:3h`
  Expected executor skills: tdd, verify-before-complete.
  - Files: `src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs`, `src/Hymnal.Core/Services/BookTxtStructureService.cs`, `src/Hymnal.Core/Services/ChapterRegistryService.cs`, `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`
  - Verify: dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter PathMove

- [x] **T04: Close Core contract integration** `est:1h`
  Expected executor skills: verify-before-complete.
  - Files: `src/Hymnal/App.axaml.cs`, `src/Hymnal/ViewModels/WorkspaceViewModel.cs`, `src/Hymnal/ViewModels/CorkboardViewModel.cs`, `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`
  - Verify: dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj && dotnet build src/Hymnal/Hymnal.csproj

## Files Likely Touched

- src/Hymnal.Core/Models/ExclusionManifest.cs
- src/Hymnal.Core/Interfaces/IExclusionManifestService.cs
- src/Hymnal.Core/Services/ExclusionManifestService.cs
- tests/Hymnal.Core.Tests/Services/ExclusionManifestServiceTests.cs
- src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs
- src/Hymnal.Core/Services/BookTxtStructureService.cs
- tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs
- src/Hymnal.Core/Services/ChapterRegistryService.cs
- src/Hymnal/App.axaml.cs
- src/Hymnal/ViewModels/WorkspaceViewModel.cs
- src/Hymnal/ViewModels/CorkboardViewModel.cs
