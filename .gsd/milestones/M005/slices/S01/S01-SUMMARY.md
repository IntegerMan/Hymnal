---
id: S01
parent: M005
milestone: M005
provides:
  - IExclusionManifestService and ExclusionManifestService for downstream excluded-chapter UI.
  - Manifest-aware include/exclude methods on IBookTxtStructureService for sidebar and Corkboard toggles.
  - MoveEntryAsync for sidebar, Corkboard, and Gantt reorder/path-change flows.
  - Automated Core tests proving manifest, rollback, conflict, and UUID-continuity behavior.
requires:
  []
affects:
  - S02: Sidebar can load `.hymnal-data/exclusions.json` and use manifest-aware include/exclude operations.
  - S03: Rename/path-change work must preserve UUIDs using the established Core contract.
  - S04: Sidebar reorder should remain a thin BookTxtStructureService consumer.
  - S05: Corkboard cross-Part moves should call MoveEntryAsync for file movement plus Book.txt order.
  - S06: Corkboard include/exclude and insertion should share S01 include/exclude/order semantics.
  - S07: Gantt reorder should use the same Book.txt structure primitive.
  - S08: Integrated UAT should verify user-facing handling of the explicit failure results.
key_files:
  - src/Hymnal.Core/Models/ExclusionManifest.cs
  - src/Hymnal.Core/Interfaces/IExclusionManifestService.cs
  - src/Hymnal.Core/Services/ExclusionManifestService.cs
  - src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs
  - src/Hymnal.Core/Services/BookTxtStructureService.cs
  - src/Hymnal.Core/Services/ChapterRegistryService.cs
  - src/Hymnal/App.axaml.cs
  - src/Hymnal/ViewModels/WorkspaceViewModel.cs
  - src/Hymnal/ViewModels/CorkboardViewModel.cs
  - tests/Hymnal.Core.Tests/Services/ExclusionManifestServiceTests.cs
  - tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs
key_decisions:
  - Persistent intentional exclusions are stored as schema-versioned JSON in `.hymnal-data/exclusions.json`.
  - Passive manifest load tolerates stale entries without rewriting; user-driven saves prune stale entries.
  - Intentional include/exclude operations update Book.txt and the exclusion manifest through BookTxtStructureService.
  - MoveEntryAsync uses one Core operation for path-changing moves and preserves registry UUID identity or fails.
  - Failure messages are phase-aware and name rollback status when multi-resource operations fail.
patterns_established:
  - Canonical structural edits belong in Hymnal.Core BookTxtStructureService, not in ViewModels or surface-specific services.
  - Path-changing operations should validate target conflicts and registry identity before mutating files.
  - Rollback-aware operations should still return failure even when rollback restores state, so UI can report that the requested edit failed.
  - Manifest mutations use atomic metadata writes and normalized forward-slash relative paths.
observability_surfaces:
  - Core Result.Fail messages identify operation phase such as Book.txt write, manifest save, file move, registry update, rollback attempted, or rollback failed.
  - No runtime logging or telemetry added; this Core-only slice relies on explicit failure results and automated tests.
drill_down_paths:
  - .gsd/milestones/M005/slices/S01/tasks/T01-SUMMARY.md
  - .gsd/milestones/M005/slices/S01/tasks/T02-SUMMARY.md
  - .gsd/milestones/M005/slices/S01/tasks/T03-SUMMARY.md
  - .gsd/milestones/M005/slices/S01/tasks/T04-SUMMARY.md
duration: ""
verification_result: passed
completed_at: 2026-06-18T03:27:17.672Z
blocker_discovered: false
---

# S01: Atomic structure core and exclusion manifest

**Delivered the Core exclusion manifest and canonical Book.txt structure operations for include, exclude, rollback-aware path moves, and UUID-safe registry continuity.**

## What Happened

S01 established the Core multi-resource consistency boundary needed by the later sidebar, Corkboard, and Gantt structural editing slices. The implementation adds a schema-versioned `.hymnal-data/exclusions.json` manifest, an `IExclusionManifestService` persistence contract, and an ExclusionManifestService that normalizes workspace-relative markdown paths, rejects traversal or absolute paths, tolerates stale entries on passive load, and prunes stale entries on the next user-driven save. BookTxtStructureService now owns intentional include and exclude semantics through manifest-aware IncludeExistingEntryAsync, IncludeExistingEntryAfterPartAsync, and ExcludeEntryAsync methods while preserving lower-level Book.txt helpers for non-manifest operations. It also exposes MoveEntryAsync as the canonical path-changing structural operation, coordinating source and target validation, target conflict prevention, filesystem moves, Book.txt order rewrite, chapter-registry UUID continuity, stale exclusion cleanup, and rollback-aware failure messages. WorkspaceViewModel and CorkboardViewModel were wired to use the manifest-aware Core contract for intentional include/remove flows, so downstream UI work has one shared structural path instead of surface-specific writes. The test suite was expanded with exclusion manifest tests and BookTxtStructureService coverage for include/exclude semantics, stale manifests, cross-Part moves, target conflicts, Book.txt write failure rollback, rollback failure messaging, registry failure rollback, ambiguous UUID protection, invalid paths, and reload-style normalized reads.

## Verification

Closeout verification used the required gsd_exec lane first. `cmd.exe /c "dotnet test tests\\Hymnal.Core.Tests\\Hymnal.Core.Tests.csproj --nologo"` and `cmd.exe /c "dotnet build src\\Hymnal\\Hymnal.csproj --nologo"` both failed in gsd_exec with the known Windows-dotnet-through-bash NuGet issue `Value cannot be null. (Parameter 'path1')`, matching existing project memory MEM008. Because this closure environment cannot execute dotnet reliably, completion relies on the task-session evidence recorded in task summaries: T02 passed 13/13 IncludeExclude tests and 292/292 full-solution tests via `Hymnal.slnx`; T03 passed 8 PathMove tests and 300 full-solution tests via `Hymnal.slnx`; T04 passed `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo` with 301 passed, 0 failed, and `dotnet build src/Hymnal/Hymnal.csproj --nologo` with 0 warnings and 0 errors. A closeout source-level gsd_exec verification also passed 26/26 checks confirming the expected files, service contracts, DI registration, manifest persistence markers, path validation markers, move/rollback/registry markers, manifest-aware ViewModel calls, and required test coverage markers are present, with no planning-directory references in touched tests.

## Requirements Advanced

- R013 — Established the canonical Core structure service foundations that Corkboard structural editing will consume for exclusion state, Book.txt writes, cross-Part file movement, and UUID continuity.

## Requirements Validated

None.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

The closeout gsd_exec lane could not directly execute dotnet verification because Windows dotnet invoked through bash fails NuGet restore with `Value cannot be null. (Parameter 'path1')`. This is a known environment limitation; task summaries contain passing normal-shell dotnet evidence. The checkout uses `Hymnal.slnx` rather than the documented `Hymnal.sln` for solution-level test verification.

## Known Limitations

No desktop UI affordances are delivered in this slice beyond wiring existing include/remove flows to the new Core contract. No repair UI or recovery-state file is added for unresolved structural failures; failures are returned as explicit Result messages for later UI handling.

## Follow-ups

S02 should consume the exclusion manifest to show intentionally excluded chapters in the sidebar. S03-S07 should route rename, reorder, Corkboard, and Gantt structural edits through BookTxtStructureService rather than adding alternate write paths. S08 should perform integrated desktop restart UAT and polish user-facing failure messages.

## Files Created/Modified

- `src/Hymnal.Core/Models/ExclusionManifest.cs` — Added schema-versioned exclusion manifest model.
- `src/Hymnal.Core/Interfaces/IExclusionManifestService.cs` — Added Core contract for loading, saving, including, and excluding manifest paths.
- `src/Hymnal.Core/Services/ExclusionManifestService.cs` — Implemented normalized atomic exclusions persistence with stale tolerance and validation.
- `src/Hymnal.Core/Interfaces/IBookTxtStructureService.cs` — Extended structure contract with manifest-aware include/exclude and rollback-aware move operations.
- `src/Hymnal.Core/Services/BookTxtStructureService.cs` — Implemented include/exclude semantics and MoveEntryAsync coordination across files, Book.txt, registry, manifest, and rollback.
- `src/Hymnal.Core/Services/ChapterRegistryService.cs` — Supported UUID-safe path-changing registry behavior consumed by structural moves.
- `src/Hymnal/App.axaml.cs` — Ensured DI composition supports the exclusion manifest service and updated structure service dependencies.
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs` — Routed intentional workspace include/remove flows through manifest-aware structure service methods.
- `src/Hymnal/ViewModels/CorkboardViewModel.cs` — Routed Corkboard include/remove flows through manifest-aware structure service methods.
- `tests/Hymnal.Core.Tests/Services/ExclusionManifestServiceTests.cs` — Added focused manifest persistence, validation, stale-entry, and failure tests.
- `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs` — Added include/exclude, path move, rollback, registry continuity, and integrated contract tests.
