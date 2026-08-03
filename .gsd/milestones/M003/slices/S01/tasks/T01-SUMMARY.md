---
id: T01
parent: S01
milestone: M003
key_files:
  - src/Hymnal.Core/Models/GanttRowData.cs
  - src/Hymnal.Core/Services/GanttProjection.cs
  - src/Hymnal/ViewModels/GanttRowViewModel.cs
  - src/Hymnal/ViewModels/GanttViewModel.cs
  - tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs
key_decisions:
  - GanttProjection placed in Hymnal.Core.Services as a pure static class so tests can target it from Hymnal.Core.Tests without referencing the Avalonia project
  - ParseDate uses TryParseExact('yyyy-MM-dd') not TryParse — strict ISO 8601 only, matching phases.json storage format
  - GanttViewModel subscribes to Nodes via direct event handler on INotifyCollectionChanged interface to avoid Rx Unit ambiguity in non-ImplicitUsings project
duration: 
verification_result: passed
completed_at: 2026-06-01T18:18:00.658Z
blocker_discovered: false
---

# T01: Added GanttProjection (Core), GanttRowData, GanttRowViewModel, and GanttViewModel with 21 passing tests covering projection and strict ISO 8601 date parsing.

**Added GanttProjection (Core), GanttRowData, GanttRowViewModel, and GanttViewModel with 21 passing tests covering projection and strict ISO 8601 date parsing.**

## What Happened

Created five files to establish the Gantt projection ViewModel layer.

**Hymnal.Core additions:**
- `GanttRowData.cs` — immutable record holding RelativePath, Title, Kind, Status, StartDate (DateOnly?), EndDate (DateOnly?), and IsMissingDates. Lives in Models alongside ChapterNode.
- `GanttProjection.cs` — pure static class in Services. `Project(ChapterNode, PhaseData?)` delegates to `PhaseDataService.DefaultPhaseData` when phase is null, then calls `ParseDate` for each date string. `ParseDate` uses `DateOnly.TryParseExact("yyyy-MM-dd")` for strict ISO 8601-only parsing — `TryParse` accepted "2024/03/15" which was caught by a test failure and corrected. IsMissingDates is true when either date is null.

**Hymnal VM additions:**
- `GanttRowViewModel.cs` — thin ViewModelBase wrapper around GanttRowData, adding `IsChapter`/`IsPart` convenience properties for view binding.
- `GanttViewModel.cs` — observes `WorkspaceViewModel.Nodes` via `(INotifyCollectionChanged)` cast (ReadOnlyObservableCollection exposes CollectionChanged only through the interface explicitly). Subscribes with a direct event handler + `Disposable.Create` cleanup rather than an Rx chain, which avoided a `System.Reactive.Unit` type ambiguity issue in the non-ImplicitUsings Hymnal project. PhaseData is already loaded onto ChapterViewModel by WorkspaceViewModel so no additional PhaseDataService call is needed for projection. PhaseDataService is retained in the constructor for future filter/refresh use cases.

**Tests:**
- 21 new tests in `GanttViewModelTests.cs` covering: `ParseDate` (valid, null, whitespace, multiple invalid formats), `Project` identity mapping (title, path, kind preservation), date parsing through projection (valid, null phase, empty strings, partial, invalid), status defaults and preservation.

## Verification

dotnet test Hymnal.sln --filter GanttViewModelTests → 21/21 passed.
dotnet test Hymnal.sln → 80/80 passed (no regressions from baseline 31+).
dotnet build src/Hymnal/Hymnal.csproj → succeeded, 0 errors.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test Hymnal.sln --filter GanttViewModelTests` | 0 | ✅ pass | 8000ms |
| 2 | `dotnet test Hymnal.sln` | 0 | ✅ pass — 80 tests, 0 failures | 12000ms |
| 3 | `dotnet build src/Hymnal/Hymnal.csproj` | 0 | ✅ pass — 0 errors | 7500ms |

## Deviations

GanttViewModel reads PhaseData directly from ChapterViewModel (already loaded) rather than calling PhaseDataService.LoadAsync — the task plan assumed a separate load, but ChapterViewModel.PhaseData is already populated by WorkspaceViewModel.LoadRegistryAndPhaseDataAsync at workspace open time. PhaseDataService is still injected for future use. Used direct event handler + Disposable.Create instead of Observable.FromEventPattern Rx chain due to System.Reactive.Unit ambiguity in non-ImplicitUsings Avalonia project.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal.Core/Models/GanttRowData.cs`
- `src/Hymnal.Core/Services/GanttProjection.cs`
- `src/Hymnal/ViewModels/GanttRowViewModel.cs`
- `src/Hymnal/ViewModels/GanttViewModel.cs`
- `tests/Hymnal.Core.Tests/ViewModels/GanttViewModelTests.cs`
