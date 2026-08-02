---
id: T01
parent: S05
milestone: M002
key_files:
  - src/Hymnal/ViewModels/ChapterInfoViewModel.cs
  - src/Hymnal/ViewModels/ChapterViewModel.cs
  - src/Hymnal/ViewModels/WorkspaceViewModel.cs
  - src/Hymnal/Views/Editor/ValidationMargin.cs
  - src/Hymnal/ViewModels/EditorViewModel.cs
key_decisions:
  - ProximityFill and HasTarget in ChapterInfoViewModel use plain backing fields (not OAPHs) updated via per-chapter WhenAnyValue subscriptions disposed on chapter switch
  - Title-match UUID-preservation (ReconcileRename) executes before AssignUuid, satisfying rename-continuity requirement
  - ValidationMargin uses two distinct regex-driven advisory triggers: blank-before-sample-block and unknown-attribute-key
  - EditorViewModel.Saved is a Subject<Unit>-backed observable; word-count-history appends in WorkspaceViewModel are driven by this signal
duration: 
verification_result: passed
completed_at: 2026-05-31T20:05:01.352Z
blocker_discovered: false
---

# T01: Build baseline confirmed clean (0 errors, 0 warnings) and all 57 tests pass; R004 code-inspection evidence collected for all five acceptance-criteria files.

**Build baseline confirmed clean (0 errors, 0 warnings) and all 57 tests pass; R004 code-inspection evidence collected for all five acceptance-criteria files.**

## What Happened

## Step 1 — Build baseline
Ran `dotnet build src/Hymnal/Hymnal.csproj -nologo` via bg_shell. Output: Build succeeded, 0 Warning(s), 0 Error(s), elapsed 16.77 s.

## Step 2 — Test baseline
Ran `dotnet test Hymnal.sln -nologo`. Output: Passed! — Failed: 0, Passed: 57, Skipped: 0, Total: 57, Duration: 380 ms.

## Step 3 — ChapterInfoViewModel.cs code inspection (R004 check a–c)
(a) **No ObservableAsPropertyHelper** for `ProximityFill` or `HasTarget` — both are plain backing fields (`private double _proximityFill` and `private bool _hasTarget`) with standard `RaiseAndSetIfChanged` setters.
(b) **Both fields updated inside per-chapter subscription block** in `OnActiveNodeChanged()`:
  - `chapterVm.WhenAnyValue(x => x.HasTarget).Subscribe(h => HasTarget = h)` added to `_chapterDisposables`
  - `chapterVm.WhenAnyValue(x => x.ProximityFill).Subscribe(f => ProximityFill = f)` added to `_chapterDisposables`
(c) **No `ComputeProximityFill` method** or local fallback calculation anywhere in the file.

## Step 4 — ChapterViewModel.cs (ProximityFill + HasTarget computed from backing fields)
- `_proximityFill` is `ObservableAsPropertyHelper<double>` using `WhenAnyValue(x => x.Target, x => x.WordCount, (t, count) => { if (t is null) return 0.0; var effectiveMax = t.MaxWords ?? t.MinWords ?? 1; return Math.Min((double)count / effectiveMax, 1.0); })` — authoritative backing fields `_target` (WordCountTarget?) and `_wordCount` (int).
- `_hasTarget` is `ObservableAsPropertyHelper<bool>` using `WhenAnyValue(x => x.Target, t => t != null && (t.MinWords.HasValue || t.MaxWords.HasValue))`.

## Step 5 — WorkspaceViewModel.cs (orphan-marking + rename-continuity before AssignUuid)
In `LoadRegistryAndPhaseDataAsync()`:
1. `registry = _registryService.ReconcileOrphans(registry, activePaths)` — marks orphans.
2. Title-match UUID-preservation block: for each node without a current-path entry, searches `orphanedEntries` for `string.Equals(entry.Title, node.Title, StringComparison.OrdinalIgnoreCase)`; on single match calls `_registryService.ReconcileRename(registry, titleMatches[0].CurrentPath, node.RelativePath)`.
3. `_registryService.AssignUuid(registry, node.RelativePath, node.Title)` — called after both passes.

## Step 6 — ValidationMargin.cs (two advisory triggers)
In `Refresh(IDocument, TextView)`:
- **Trigger 1** (`SampleBlockRegex`): `string.IsNullOrWhiteSpace(text) && i < lineCount` → next-line matches `@"^\{[^}]*sample\s*:\s*true[^}]*\}"` → blank line immediately before `{sample: true}` attribute block heading adds advisory dot.
- **Trigger 2** (`AttrBlockRegex` + `ValidMarkuaKeys`): `AttrBlockRegex.Matches(text)` finds `{…}` blocks; `KeyValueRegex` extracts keys; any key not in `ValidMarkuaKeys` (25-entry set) adds advisory dot to that line.

## Step 7 — EditorViewModel.cs (debounce + Saved observable)
- `_liveWordCount` OAPH: `this.WhenAnyValue(x => x.Text).Throttle(TimeSpan.FromMilliseconds(300), TaskPoolScheduler.Default)` — 300 ms debounce confirmed.
- `private readonly Subject<Unit> _savedSubject = new();` → `public IObservable<Unit> Saved => _savedSubject.AsObservable();` fires in `SaveAsync()` via `_savedSubject.OnNext(Unit.Default)` after successful atomic write.
- `WorkspaceViewModel` subscribes: `_editor.Saved.Subscribe(saved => { ... _historyService.AppendAsync(workspaceRoot, uuid, date, count) ... })` — word-count-history appends are driven by the Saved signal.

## Verification

1. `dotnet build src/Hymnal/Hymnal.csproj -nologo` — exit 0, Build succeeded, 0 Warning(s), 0 Error(s).
2. `dotnet test Hymnal.sln -nologo` — exit 0, Passed: 57, Failed: 0, Skipped: 0.
3–7. All five code-inspection checks passed with positive findings confirmed by direct file reads (see narrative above).

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build src/Hymnal/Hymnal.csproj -nologo` | 0 | ✅ pass — 0 errors, 0 warnings | 16770ms |
| 2 | `dotnet test Hymnal.sln -nologo` | 0 | ✅ pass — 57 passed, 0 failed | 21000ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `src/Hymnal/ViewModels/ChapterInfoViewModel.cs`
- `src/Hymnal/ViewModels/ChapterViewModel.cs`
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs`
- `src/Hymnal/Views/Editor/ValidationMargin.cs`
- `src/Hymnal/ViewModels/EditorViewModel.cs`
