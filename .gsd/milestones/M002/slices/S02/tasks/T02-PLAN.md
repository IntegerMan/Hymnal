---
estimated_steps: 9
estimated_files: 2
skills_used: []
---

# T02: WorkspaceViewModel orchestration — targets load, background word count, Saved subscription, totals rollup, DI wiring

Why: WorkspaceViewModel must coordinate the new services: load per-chapter targets at workspace open, trigger per-chapter background word count recalculation so the sidebar populates from '—' to real counts without blocking startup, subscribe to EditorViewModel.Saved to persist and record history, and maintain a TotalWordCount + Part totals that update whenever any chapter's count changes.

Do:
1. **WorkspaceViewModel constructor**: add parameters `WordCountService wordCountService`, `TargetsService targetsService`, `WordCountHistoryService historyService`; store as `_wordCountService`, `_targetsService`, `_historyService`.

2. **TotalWordCount**: add `int _totalWordCount` / `TotalWordCount` int property backed by `RaiseAndSetIfChanged`. Add private helper `void UpdateTotals()` that: (a) sets `TotalWordCount = _nodes.Where(vm => vm.Node.Kind == NodeKind.Chapter).Sum(vm => vm.WordCount)`; (b) calls `RecomputePartTotals()`. Add `void RecomputePartTotals()` that iterates `_nodes` in order: for each Part node, accumulates the WordCount of following Chapter nodes (until the next Part or end), then calls `vm.PartTotalWordCount = accumulated_sum` using the ChapterViewModel's setter (make PartTotalWordCount publicly settable). Note: a `TotalWordCountDisplay` string property can be exposed on WorkspaceViewModel as `$"{TotalWordCount:N0} w"` for the CHAPTERS header binding.

3. **LoadRegistryAndPhaseDataAsync** additions (after building vms list, before the Dispatcher.InvokeAsync call): load targets dict via `await _targetsService.LoadAsync(model.WorkspaceRoot)`. Inside Dispatcher.InvokeAsync, after populating `_nodes`, assign `Target` from the loaded dict for each chapter vm: `vm.Target = targetsService.GetTarget(targets, vm.Uuid)`. After populating nodes, subscribe to `EditorViewModel.Saved` (store the IDisposable in Disposables): on each save, find the active ChapterViewModel by matching `_editor.ActiveNode`, recount words using `_wordCountService.CountWords(_editor.Text)`, call `vm.UpdateWordCount(count)`, call `UpdateTotals()`, then fire-and-forget `_historyService.AppendAsync(workspaceRoot, vm.Uuid, DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"), count)` with error surfaced via `_notificationService.ShowError`.

4. **Background word count recalculation**: after building `vms` list (outside Dispatcher.InvokeAsync), for each chapter vm (NodeKind.Chapter, not IsMissing), launch `Task.Run(async () => { var absPath = Path.Combine(model.ManuscriptRoot, vm.Node.RelativePath); var content = await File.ReadAllTextAsync(absPath); return _wordCountService.CountWords(content); }).ContinueWith(t => { if (!t.IsCompletedSuccessfully) return; await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { if (_workspaceGeneration != generation) return; vm.UpdateWordCount(t.Result); UpdateTotals(); }); }, TaskScheduler.Default)`. Each chapter recalculates independently; failures are silent (chapter stays at '—').

5. **CloseWorkspaceAsync**: dispose the Saved subscription (it's in Disposables so automatic), and call `UpdateTotals()` at the end (or simply reset TotalWordCount = 0).

6. **App.axaml.cs**: register `services.AddSingleton<WordCountService>()`, `services.AddSingleton<TargetsService>()`, `services.AddSingleton<WordCountHistoryService>()`. Update the WorkspaceViewModel factory to pass `sp.GetRequiredService<WordCountService>()`, `sp.GetRequiredService<TargetsService>()`, `sp.GetRequiredService<WordCountHistoryService>()` as the three new arguments after `PhaseDataService`.

Done when: build passes and the Core service tests pass.

## Inputs

- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/App.axaml.cs`
- `src/Hymnal/ViewModels/EditorViewModel.cs`
- `src/Hymnal/ViewModels/ChapterViewModel.cs`
- `src/Hymnal.Core/Services/WordCountService.cs`
- `src/Hymnal.Core/Services/TargetsService.cs`
- `src/Hymnal.Core/Services/WordCountHistoryService.cs`
- `src/Hymnal.Core/Models/ChapterRegistryEntry.cs`
- `src/Hymnal.Core/Models/ChapterNode.cs`

## Expected Output

- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/App.axaml.cs`

## Verification

dotnet build src/Hymnal/Hymnal.csproj -nologo && dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter WordCount -nologo && dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter Targets -nologo
