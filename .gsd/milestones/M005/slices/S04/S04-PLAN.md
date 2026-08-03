# S04: Sidebar drag reorder for chapters and Parts

**Goal:** Deliver sidebar drag reorder for included chapters and Parts by building on the existing drag wiring and WorkspaceViewModel reorder orchestration, while making legal moves explicit: included chapters can be reordered within their current Part section, included Part dividers can be reordered as whole Part blocks, excluded or missing nodes cannot be dragged or targeted, and unsupported cross-Part chapter movement is rejected with a user-visible error rather than producing a Book.txt structure that Core rejects opaquely.
**Demo:** After this, dragging chapters or Parts in the sidebar updates Book.txt and the new order survives reload without duplicate nodes.

## Must-Haves

- Included chapter rows can be reordered from the sidebar within their current Part section, update `Book.txt`, and survive workspace reload.
- Included Part rows can be dragged relative to other Part rows, moving the Part divider and its child chapter entries as one Book.txt block, and survive workspace reload.
- Excluded and missing rows cannot be dragged or targeted; unsupported cross-Part chapter movement is rejected before Core mutation with a user-visible error.
- All successful structural writes route through `BookTxtStructureService.ReorderEntryAsync` under WorkspaceViewModel watcher suppression; no view or ViewModel writes Book.txt directly.
- Automated tests assert no duplicate sidebar nodes after reorder/reload and preserve existing include/exclude and rename behavior.

## Proof Level

- This slice proves: Integration proof. Real runtime required: no desktop automation required for slice closeout, but tests must exercise Core + WorkspaceViewModel + sidebar gesture predicates with temp workspaces. Human/UAT required: deferred to S08 integrated desktop UAT.

## Integration Closure

Upstream surfaces consumed: `BookTxtStructureService.ReorderEntryAsync`, `WorkspaceViewModel.ReorderChapterCommand`/reorder orchestration, `ChapterNode.IsExcluded`, sidebar drag handlers, and prior S01-S03 watcher suppression/reload patterns. New wiring introduced: Part-aware sidebar drag predicates and ViewModel legal-move validation feeding the canonical Core reorder path. Remaining milestone work: Corkboard cross-Part file moves, Corkboard include/insert, Gantt row reorder, and S08 integrated desktop UAT.

## Verification

- Failure visibility remains Result/notification based rather than telemetry based. The plan requires explicit illegal-drop predicates in the view, notification assertions in ViewModel tests, and phase-aware Core Result.Fail messages for invalid Part reorder requests; no secrets or external services are touched.

## Tasks

- [x] **T01: Make Core reorder Part blocks atomically in Book.txt** `est:2h`
  Why: existing BookTxtStructureService.ReorderEntryAsync can move a single Book.txt entry, which is enough for chapter line reorder but not enough for dragging a Part divider; a Part reorder must move the Part divider plus its following chapter entries as one contiguous block so the sidebar cannot split a Part from its contents. This advances R013 by preserving canonical Book.txt structure semantics for Part reorder before UI code consumes it.
  - Files: `src/Hymnal.Core/Services/BookTxtStructureService.cs`, `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`
  - Verify: powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter BookTxtStructureServiceTests --verbosity minimal"

- [x] **T02: Route sidebar reorder commands through explicit legal move validation** `est:3h`
  Why: WorkspaceViewModel already exposes ReorderChapterCommand and watcher suppression, but the current path assumes chapter-only reorder and resolves indexes against visible nodes that can include excluded projections. S04 needs a sidebar structural command that accepts chapter and Part drag intents, rejects unsupported drops before Core mutation, suppresses watchers, calls only BookTxtStructureService, and reloads/synchronizes the workspace without duplicate nodes. This advances R013 by making sidebar structural reorder a thin, validated consumer of the canonical Book.txt path.
  - Files: `src/Hymnal/ViewModels/WorkspaceViewModel.cs`, `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarReorderTests.cs`, `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarRenameTests.cs`
  - Verify: powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter WorkspaceSidebarReorderTests --verbosity minimal"

- [x] **T03: Enable sidebar drag gestures for eligible chapters and Parts only** `est:2h`
  Why: the SidebarView currently has drag/drop wiring but only starts drags for chapters and does not expose testable legal-drop predicates. S04 needs the view to allow Part drags, suppress illegal visual drop affordances, and send only valid source/target intent to WorkspaceViewModel so UI gestures cannot generate malformed structure requests.
  - Files: `src/Hymnal/Views/SidebarView.axaml.cs`, `src/Hymnal/Views/SidebarView.axaml`, `tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs`
  - Verify: powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test tests\Hymnal.Core.Tests\Hymnal.Core.Tests.csproj --nologo --filter SidebarViewSmokeTests --verbosity minimal"

- [x] **T04: Prove sidebar reorder integration and reload persistence** `est:2h`
  Why: the slice demo is not true until chapters and Parts can be reordered through the real sidebar/ViewModel/Core path and the resulting Book.txt order survives workspace reload without duplicate nodes. This final task closes the integration proof and guards the existing rename/exclusion surfaces against regression.
  - Files: `tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarReorderTests.cs`, `tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs`, `tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs`
  - Verify: powershell.exe -NoProfile -Command "& 'C:\Program Files\dotnet\dotnet.exe' test Hymnal.slnx --nologo --verbosity minimal"

## Files Likely Touched

- src/Hymnal.Core/Services/BookTxtStructureService.cs
- tests/Hymnal.Core.Tests/Services/BookTxtStructureServiceTests.cs
- src/Hymnal/ViewModels/WorkspaceViewModel.cs
- tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarReorderTests.cs
- tests/Hymnal.Core.Tests/ViewModels/WorkspaceSidebarRenameTests.cs
- src/Hymnal/Views/SidebarView.axaml.cs
- src/Hymnal/Views/SidebarView.axaml
- tests/Hymnal.Core.Tests/Views/SidebarViewSmokeTests.cs
