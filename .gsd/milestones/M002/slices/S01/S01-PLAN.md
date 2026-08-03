# S01: Chapter Registry and Status Lifecycle

**Goal:** Introduce UUID-keyed chapter identity (chapter-registry.json) and per-chapter status/phase-date persistence (phases.json), expose a ChapterViewModel wrapper across the sidebar, and render a coloured status dot with an inline status-change flyout — all surviving chapter renames and app restarts.
**Demo:** Open workspace: each chapter in sidebar shows a coloured dot (grey = Outlining, sky = Drafting, violet = Editing, amber = Polishing, pink = Reviewing, emerald = Done). Change a chapter status to Drafting: dot updates immediately; phase start date pre-fills with today. Close and reopen the app: dot and phase date survive the restart. Rename the .md file and update Book.txt, relaunch: the UUID-keyed status is still intact. dotnet test --filter ChapterRegistry and --filter PhaseData both exit 0.

## Must-Haves

- Open workspace: each chapter in sidebar shows a coloured status dot (grey = Outlining by default). Part header rows have no dot.
- Click a chapter's dot, select Drafting: dot colour changes immediately; phase start date pre-fills with today.
- Close and reopen app: dot colour and phase start date survive the restart.
- Rename the .md file, update Book.txt, relaunch: UUID-keyed status is still intact.
- dotnet test --filter ChapterRegistryServiceTests exits 0; dotnet test --filter PhaseDataServiceTests exits 0.
- dotnet build exits 0 with zero warnings for Hymnal.Core and Hymnal.

## Proof Level

- This slice proves: Automated unit tests (ChapterRegistryServiceTests, PhaseDataServiceTests) + dotnet build green for both projects.

## Integration Closure

ChapterViewModel.Node exposes the underlying ChapterNode so EditorViewModel (still typed to ChapterNode) is unmodified in S01. WorkspaceViewModel.SelectedNode changes type from ChapterNode? to ChapterViewModel?; all internal usages updated in-place. SidebarView DataTemplate switches from models:ChapterNode to vm:ChapterViewModel and binds the status dot and flyout. S02 will add WordCount/Target to ChapterViewModel; S03 will add the Chapter Info pane and phase-date pre-fill toggle.

## Verification

- PhaseDataService and ChapterRegistryService failures surface as Result.Fail; WorkspaceViewModel propagates failures via INotificationService.ShowError. ChapterViewModel.ChangeStatusCommand.ThrownExceptions subscribed to ShowError. No silent swallows of persistence errors.

## Tasks

- [x] **T01: Added ChapterStatus enum, PhaseData/ChapterRegistryEntry models, ChapterRegistryService, and PhaseDataService to Hymnal.Core — build clean.** `est:45m`
  **Why:** All M002 tracking (status, phase dates, word counts, targets) hangs off stable UUIDs. The Core layer must be pure .NET with zero Avalonia refs so it is independently testable.
  - Files: `src/Hymnal.Core/Models/ChapterStatus.cs`, `src/Hymnal.Core/Models/PhaseData.cs`, `src/Hymnal.Core/Models/ChapterRegistryEntry.cs`, `src/Hymnal.Core/Services/ChapterRegistryService.cs`, `src/Hymnal.Core/Services/PhaseDataService.cs`
  - Verify: dotnet build src/Hymnal.Core/Hymnal.Core.csproj

- [x] **T02: Added 10 xUnit tests covering ChapterRegistryService (6) and PhaseDataService (4); all pass at exit 0** `est:35m`
  **Why:** Registry reconciliation and round-trip JSON fidelity are the key correctness invariants for UUID-keyed identity. Tests must be zero-Avalonia and run without a real filesystem (use temp directories).
  - Files: `tests/Hymnal.Core.Tests/Services/ChapterRegistryServiceTests.cs`, `tests/Hymnal.Core.Tests/Services/PhaseDataServiceTests.cs`
  - Verify: dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter ChapterRegistryServiceTests && dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter PhaseDataServiceTests

- [x] **T03: Created ChapterViewModel wrapping ChapterNode with reactive Status/PhaseData and ChangeStatusCommand that persists to PhaseDataService; build clean at exit 0.** `est:40m`
  **Why:** ChapterViewModel is the single binding target for the sidebar list and later the Chapter Info pane (S03). It wraps ChapterNode (immutable record) to add mutable reactive status state without touching Core models.
  - Files: `src/Hymnal/ViewModels/ChapterViewModel.cs`
  - Verify: dotnet build src/Hymnal/Hymnal.csproj

- [x] **T04: Refactored WorkspaceViewModel to ChapterViewModel collection, added StatusToBrushConverter + BoolToOpacityConverter, wired up registry/phase hydration, updated SidebarView DataTemplate with coloured status dot + MenuFlyout, registered new services in DI — build clean.** `est:75m`
  **Why:** The sidebar is the primary author surface for status visibility and changes. WorkspaceViewModel must wire up registry reconciliation and phase-data hydration at workspace load. SidebarView DataTemplate must switch to ChapterViewModel and render the coloured dot + flyout.
  - Files: `src/Hymnal/ViewModels/WorkspaceViewModel.cs`, `src/Hymnal/Views/Converters/StatusToBrushConverter.cs`, `src/Hymnal/Views/SidebarView.axaml`, `src/Hymnal/App.axaml.cs`
  - Verify: dotnet build src/Hymnal/Hymnal.csproj

- [x] **T05: Restore-path verification workaround** `est:30m`
  Diagnose and resolve the NuGet restore path-combine failure that prevents `dotnet build` and `dotnet test` from running in this environment. Confirm a stable invocation or environment configuration that produces fresh build/test evidence for the slice without modifying the completed implementation tasks.
  - Files: `.gsd/milestones/M002/slices/S01/PLAN.md`, `(verification environment only)`
  - Verify: dotnet build src/Hymnal.Core/Hymnal.Core.csproj -nologo && dotnet build src/Hymnal/Hymnal.csproj -nologo && dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter ChapterRegistryServiceTests && dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter PhaseDataServiceTests

## Files Likely Touched

- src/Hymnal.Core/Models/ChapterStatus.cs
- src/Hymnal.Core/Models/PhaseData.cs
- src/Hymnal.Core/Models/ChapterRegistryEntry.cs
- src/Hymnal.Core/Services/ChapterRegistryService.cs
- src/Hymnal.Core/Services/PhaseDataService.cs
- tests/Hymnal.Core.Tests/Services/ChapterRegistryServiceTests.cs
- tests/Hymnal.Core.Tests/Services/PhaseDataServiceTests.cs
- src/Hymnal/ViewModels/ChapterViewModel.cs
- src/Hymnal/ViewModels/WorkspaceViewModel.cs
- src/Hymnal/Views/Converters/StatusToBrushConverter.cs
- src/Hymnal/Views/SidebarView.axaml
- src/Hymnal/App.axaml.cs
- .gsd/milestones/M002/slices/S01/PLAN.md
- (verification environment only)
