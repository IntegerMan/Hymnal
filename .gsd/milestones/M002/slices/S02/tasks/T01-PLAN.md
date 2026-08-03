---
estimated_steps: 4
estimated_files: 3
skills_used: []
---

# T01: ViewModel layer — EditorViewModel.LiveWordCount/Saved and ChapterViewModel word-count state

Why: EditorViewModel is the live content buffer; S02 needs a 300ms-debounced word count while the author types, and a `Saved` observable so WorkspaceViewModel can persist counts and append history on each save rather than polling OriginalText. ChapterViewModel is the per-chapter data wrapper; it must carry word count state, a target, display-ready computed strings, a proximity fill double, and commands for the right-click target flyout.

Do:
1. **EditorViewModel**: inject `WordCountService` (Hymnal.Core.Services) as constructor parameter. Add `Subject<Unit> _savedSubject = new Subject<Unit>()` and expose `IObservable<Unit> Saved => _savedSubject.AsObservable()`. In `SaveAsync`, after `OriginalText = Text`, fire `_savedSubject.OnNext(Unit.Default)`. Add `ObservableAsPropertyHelper<int> _liveWordCount` wired as: `this.WhenAnyValue(x => x.Text).Throttle(TimeSpan.FromMilliseconds(300), RxApp.TaskpoolScheduler).Select(t => _wordCountService.CountWords(t)).ObserveOn(RxApp.MainThreadScheduler).ToProperty(this, x => x.LiveWordCount, out _liveWordCount)`. Register `_liveWordCount` with `Disposables.Add(_liveWordCount)`. Add public `int LiveWordCount => _liveWordCount.Value`. Update `App.axaml.cs` EditorViewModel factory to pass `sp.GetRequiredService<WordCountService>()` as the new last argument.

2. **ChapterViewModel**: Add the following RaiseAndSetIfChanged-backed properties: `int WordCount` (default 0), `bool WordCountKnown` (default false — sidebar shows '—' until true), `int PartTotalWordCount` (only meaningful for Part nodes; WorkspaceViewModel sets), `WordCountTarget? Target`. Add staging properties for target flyout: `int? PendingMinWords`, `int? PendingMaxWords`. Add computed OAPH properties (derived in constructor): `string WordCountDisplay` — returns '—' when !WordCountKnown, else $"{WordCount:N0} w" (e.g. '2,130 w'); `string PartTotalDisplay` — same format for PartTotalWordCount; `double ProximityFill` — 0.0 when Target is null or both fields null, else min((double)WordCount / effectiveMax, 1.0) where effectiveMax = Target.MaxWords ?? Target.MinWords ?? 1; `bool HasTarget` — Target != null && (Target.MinWords.HasValue || Target.MaxWords.HasValue). Inject `TargetsService _targetsService`. Add `ReactiveCommand<WordCountTarget?, Unit> SetTargetCommand` — async impl calls `_targetsService.UpsertAsync(_workspaceRoot, Uuid, target)` then on UI thread sets `Target = target` and refreshes `PendingMinWords`/`PendingMaxWords`. Add `ReactiveCommand<Unit, Unit> ConfirmTargetCommand` — constructs `new WordCountTarget { MinWords = PendingMinWords, MaxWords = PendingMaxWords }` and executes `SetTargetCommand`. Add `ReactiveCommand<Unit, Unit> ClearTargetCommand` — executes `SetTargetCommand` with null. Add `public void UpdateWordCount(int count)` that sets `WordCount = count; WordCountKnown = true`. Subscribe ThrownExceptions on all three new commands to `_notificationService.ShowError`.

## Inputs

- `src/Hymnal/ViewModels/EditorViewModel.cs`
- `src/Hymnal/ViewModels/ChapterViewModel.cs`
- `src/Hymnal/App.axaml.cs`
- `src/Hymnal.Core/Services/WordCountService.cs`
- `src/Hymnal.Core/Services/TargetsService.cs`
- `src/Hymnal.Core/Models/WordCountTarget.cs`

## Expected Output

- `src/Hymnal/ViewModels/EditorViewModel.cs`
- `src/Hymnal/ViewModels/ChapterViewModel.cs`
- `src/Hymnal/App.axaml.cs`

## Verification

dotnet build src/Hymnal/Hymnal.csproj -nologo

## Observability Impact

EditorViewModel.Saved fires a Unit after every successful atomic save, providing a reliable hook for history appending without polling.
