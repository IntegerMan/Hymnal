---
id: S02
parent: M005
milestone: M005
provides:
  - A stable `ChapterNode.IsExcluded` projection surface for downstream sidebar, Corkboard, and Gantt structural editing.
  - Verified include/exclude sidebar commands that reuse the canonical S01 BookTxtStructureService reload path.
  - Smoke and integration coverage proving exclusions manifest, Book.txt, and sidebar state remain synchronized across reload.
requires:
  - slice: S01
    provides: Exclusion manifest service, orphan discovery, and canonical BookTxtStructureService include/exclude operations consumed by the sidebar workflow.
affects:
  []
key_files:
  - src/Hymnal/ViewModels/WorkspaceViewModel.cs
  - src/Hymnal.Core/Models/ChapterNode.cs
  - src/Hymnal.Core/Services/ManuscriptService.cs
  - src/Hymnal/Views/SidebarView.axaml
  - src/Hymnal/Views/SidebarView.axaml.cs
  - src/Hymnal/Views/Converters/NodeKindConverters.cs
  - src/Hymnal/Views/Converters/StatusToBrushConverter.cs
  - tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarExclusionTests.cs
  - tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs
key_decisions:
  - Project manifest-backed excluded files in WorkspaceViewModel after active Book.txt load and registry reconciliation so excluded sidebar nodes stay visible without receiving new registry UUIDs or affecting manuscript totals.
  - Treat excluded sidebar rows as UI-only structural projections: include uses current visible ordering to compute Book.txt insertion, missing entries use a dedicated remove path, and excluded nodes cannot open the editor.
  - Represent excluded state through the existing shared sidebar converters so XAML can distinguish excluded, missing, part, and normal nodes without adding extra styling-only view-model surface.
patterns_established:
  - Sidebar structural state now has three explicit modes: included present, excluded projected, and missing stale entry, each with its own command path and visual treatment.
  - Book.txt remains the source of truth for active manuscript structure while `.hymnal-data/exclusions.json` is projected into the sidebar only after active nodes are known.
observability_surfaces:
  - none
drill_down_paths:
  - .gsd/milestones/M005/slices/S02/tasks/T01-SUMMARY.md
  - .gsd/milestones/M005/slices/S02/tasks/T02-SUMMARY.md
  - .gsd/milestones/M005/slices/S02/tasks/T03-SUMMARY.md
  - .gsd/milestones/M005/slices/S02/tasks/T04-SUMMARY.md
duration: ""
verification_result: passed
completed_at: 2026-06-18T08:15:02.584Z
blocker_discovered: false
---

# S02: Sidebar excluded chapters and include toggle

**Projected manifest-excluded markdown files into the sidebar as non-editor excluded nodes, added include/exclude sidebar commands, and verified Book.txt plus exclusions manifest stay correct across reload.**

## What Happened

S02 turned the S01 exclusion manifest and structure services into a sidebar-visible author workflow. WorkspaceViewModel now loads active Book.txt nodes first, reconciles registry state only for active chapters, then projects manifest-backed orphan markdown files into the sidebar as ChapterNode instances with IsExcluded = true. Those excluded nodes stay visible without creating fresh registry UUIDs, without affecting book totals or status rollups, and without being treated as missing Book.txt entries. The sidebar command surface now distinguishes three structural states: included present chapters can be excluded from Book.txt, excluded projected chapters can be included back into Book.txt at the current visible position, and stale missing Book.txt entries can be removed without polluting exclusions.json. TrySwitchChapterAsync was hardened so excluded nodes do not open in the editor and instead preserve the current selection. The view layer added dedicated excluded styling and conditional context-menu wiring so excluded rows show a muted/italic treatment plus an excluded badge while only exposing Include in book. Smoke coverage and integration tests exercise projection ordering, reload stability, include/exclude manifest updates, missing-entry removal, failure-notification preservation, and excluded-selection no-open behavior over temp workspaces with the real Core services.

## Verification

Closeout verification passed on the current tree with fresh native .NET runs from PowerShell: (1) `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --nologo --filter WorkspaceSidebarExclusionTests --verbosity minimal` passed with 8/8 tests green, covering excluded-node projection, reload persistence, include/exclude behavior, missing-entry removal, and failure-path notifications; (2) `dotnet test Hymnal.slnx --nologo --verbosity minimal` passed with 311/311 tests green; and (3) `dotnet build src/Hymnal/Hymnal.csproj --nologo` succeeded with 0 warnings and 0 errors. During closure, `gsd_exec` reproed the known MEM008 WSL/Windows NuGet `path1` failure for direct dotnet execution, so the actual proof runs used native PowerShell-hosted dotnet commands. A transient parallel rerun lock on `Hymnal.Core.dll` was resolved by rerunning the targeted suite sequentially; the final sequential verification passed cleanly.

## Requirements Advanced

- R013 — Advanced the manuscript structural-editing foundation by surfacing excluded chapters in the UI and wiring persisted include/exclude operations through the canonical Book.txt structure path.

## Requirements Validated

None.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

Closeout verification was run sequentially after one parallel shell run hit a transient `VBCSCompiler` file lock on `src/Hymnal.Core/obj/Debug/net10.0/Hymnal.Core.dll`; no product change was required.

## Known Limitations

None.

## Follow-ups

S03 and later slices can rely on `ChapterNode.IsExcluded`, the sidebar include/remove command split, and the WorkspaceViewModel projection boundary when adding rename, reorder, Corkboard, and Gantt structural editing.

## Files Created/Modified

- `src/Hymnal/ViewModels/WorkspaceViewModel.cs` — Projected excluded sidebar nodes after active-manuscript load, added include/remove command paths, and prevented excluded selection from opening the editor.
- `src/Hymnal.Core/Models/ChapterNode.cs` — Added persistent `IsExcluded` state on chapter nodes for sidebar projection.
- `src/Hymnal.Core/Services/ManuscriptService.cs` — Restricted manuscript loading to active Book.txt nodes so exclusion projection occurs at the WorkspaceViewModel boundary.
- `src/Hymnal/Views/SidebarView.axaml` — Added excluded-row badge/styling and conditional include/exclude/remove context-menu bindings.
- `src/Hymnal/Views/SidebarView.axaml.cs` — Guarded sidebar actions so they appear only for supported node states.
- `src/Hymnal/Views/Converters/NodeKindConverters.cs` — Returned dedicated presentation state for excluded chapters.
- `src/Hymnal/Views/Converters/StatusToBrushConverter.cs` — Added bool-to-font-style support used by excluded sidebar styling.
- `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarExclusionTests.cs` — Added temp-workspace integration tests for projection ordering, reload persistence, include/exclude operations, missing-entry removal, and notification-preserving failure paths.
- `tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs` — Added smoke coverage for excluded sidebar bindings and converter behavior.
