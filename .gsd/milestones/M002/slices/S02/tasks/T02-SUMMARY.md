---
id: T02
parent: S02
milestone: M002
key_files:
  - src/Hymnal/ViewModels/WorkspaceViewModel.cs
  - src/Hymnal/App.axaml.cs
key_decisions:
  - Subscribe lambda parameter renamed from '_' to 'saved' to avoid conflict with discard assignment '_ = Task' — reactive Unit vs Task ambiguity
  - Targets passed into ChapterViewModel constructor via existing optional 'target' param rather than post-construction setter (Target has private set) — avoids modifying ChapterViewModel
  - Background word count tasks use Task.Run + Dispatcher.UIThread.InvokeAsync with generation guard; failures swallowed silently per slice spec
duration: 
verification_result: passed
completed_at: 2026-05-31T02:59:39.817Z
blocker_discovered: false
---

# T02: WorkspaceViewModel wired with WordCountService/HistoryService: background chapter count on load, Saved subscription updates counts+history, TotalWordCount/TotalWordCountDisplay rollup, targets loaded at workspace open

**WorkspaceViewModel wired with WordCountService/HistoryService: background chapter count on load, Saved subscription updates counts+history, TotalWordCount/TotalWordCountDisplay rollup, targets loaded at workspace open**

## What Happened

WorkspaceViewModel.cs and App.axaml.cs updated to complete the orchestration layer for word count, targets, and history.\n\n**Fields added**: `_wordCountService` (WordCountService) and `_historyService` (WordCountHistoryService) stored alongside the existing `_targetsService`.\n\n**New properties**: `TotalWordCount` (int, RaiseAndSetIfChanged) and `TotalWordCountDisplay` (OAPH, formatted as '{N:N0} w') — both ready for sidebar CHAPTERS header binding.\n\n**Constructor additions**: OAPH for TotalWordCountDisplay wired via WhenAnyValue(x => x.TotalWordCount). Singleton Saved subscription: on each EditorViewModel.Saved signal, finds the active ChapterViewModel by matching ActiveNode, recounts via WordCountService.CountWords(_editor.Text), calls vm.UpdateWordCount + UpdateTotals(), then fire-and-forgets HistoryService.AppendAsync with error surfaced to INotificationService.ShowError via ContinueWith.\n\n**LoadRegistryAndPhaseDataAsync additions**: Targets loaded via TargetsService.LoadAsync after phase data. Targets passed directly into each ChapterViewModel constructor via the existing optional `target` parameter — cleaner than a post-construction setter assignment. Background Task.Run per chapter: reads file content, counts words, dispatches UpdateWordCount + UpdateTotals to UI thread with generation guard. Silent on failure. After the main Dispatcher.InvokeAsync populates _nodes, UpdateTotals() called once to initialize totals (0 until background tasks arrive).\n\n**CloseWorkspaceAsync**: TotalWordCount reset to 0 after clearing nodes.\n\n**App.axaml.cs**: Added `services.AddSingleton<WordCountHistoryService>()`. WorkspaceViewModel factory updated to pass WordCountService and WordCountHistoryService as the 9th and 10th constructor arguments.\n\n**One fix during implementation**: the `Subscribe(_ => ...)` lambda parameter named `_` conflicted with `_ = _historyService.AppendAsync(...)` discard assignment (compiler couldn't implicitly convert Task to Reactive.Unit). Fixed by renaming the lambda parameter to `saved` and storing the fire-and-forget Task in a named local `histTask` before calling ContinueWith.

## Verification

dotnet build src/Hymnal/Hymnal.csproj -nologo → Build succeeded, 0 errors. dotnet test --filter WordCount → 10/10 passed. dotnet test --filter Targets → 6/6 passed. dotnet test Hymnal.sln -nologo → 57/57 passed, 0 regressions.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build src/Hymnal/Hymnal.csproj -nologo` | 0 | ✅ pass | 5160ms |
| 2 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter WordCount -nologo` | 0 | ✅ pass — 10 passed, 0 failed | 2200ms |
| 3 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter Targets -nologo` | 0 | ✅ pass — 6 passed, 0 failed | 2400ms |
| 4 | `dotnet test Hymnal.sln -nologo` | 0 | ✅ pass — 57 passed, 0 failed | 3800ms |

## Deviations

Target assignment uses ChapterViewModel constructor's optional 'target' parameter rather than post-construction property assignment (which was the plan's suggested approach). Target.set is private in ChapterViewModel so the constructor path is the correct approach and requires no ChapterViewModel changes.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/App.axaml.cs`
