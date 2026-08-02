---
estimated_steps: 11
estimated_files: 1
skills_used: []
---

# T01: Replace OAPH-based ProximityFill and HasTarget with live per-chapter subscriptions

**Why**: ChapterInfoViewModel crashes on startup because the `_proximityFill` OAPH calls `WhenAnyValue(x => x.HasTarget, ...)` — which evaluates immediately — before `_hasTarget` is assigned, producing a NullReferenceException. Additionally, `ProximityFill` is permanently stubbed at 0.0 via `ComputeProximityFill`. Both defects are fixed by dropping the OAPHs in favour of plain backing fields driven from the existing per-chapter subscription pattern.

**Do**:
1. Change field declaration: `private readonly ObservableAsPropertyHelper<double> _proximityFill` → `private double _proximityFill`.
2. Change field declaration: `private readonly ObservableAsPropertyHelper<bool> _hasTarget` → `private bool _hasTarget`.
3. Replace both property getters — change `public double ProximityFill => _proximityFill.Value` to a full property with a private setter using `this.RaiseAndSetIfChanged(ref _proximityFill, value)`, and same for `HasTarget`.
4. Remove both OAPH initialisation blocks from the constructor (the `this.WhenAnyValue(x => x.HasTarget, x => x.WordCount, x => x.TargetDisplay, ...)` block for `_proximityFill` and the `this.WhenAnyValue(x => x.TargetDisplay, ...)` block for `_hasTarget`), along with their corresponding `Disposables.Add(...)` lines.
5. In `OnActiveNodeChanged`, inside the `if (chapterVm != null)` per-chapter block, add two new subscriptions after the existing four: `chapterVm.WhenAnyValue(x => x.HasTarget).Subscribe(h => HasTarget = h)` and `chapterVm.WhenAnyValue(x => x.ProximityFill).Subscribe(f => ProximityFill = f)`, each wrapped in `_chapterDisposables.Add(...)`.
6. In `SyncFromChapterVm`, add `HasTarget = vm.HasTarget; ProximityFill = vm.ProximityFill;` alongside the existing sync lines.
7. In the `node == null` reset path inside `OnActiveNodeChanged`, add `HasTarget = false; ProximityFill = 0.0;` alongside the existing resets.
8. Remove the `ComputeProximityFill` private static method entirely (it only returns 0.0 and is no longer referenced).

**Done when**: File compiles without errors; `_proximityFill` and `_hasTarget` are plain backing fields; no OAPH for either property exists; `ComputeProximityFill` is gone; `OnActiveNodeChanged` subscribes to `chapterVm.ProximityFill` and `chapterVm.HasTarget`.

## Inputs

- `src/Hymnal/ViewModels/ChapterInfoViewModel.cs`
- `src/Hymnal/ViewModels/ChapterViewModel.cs`

## Expected Output

- `src/Hymnal/ViewModels/ChapterInfoViewModel.cs`

## Verification

dotnet build src/Hymnal/Hymnal.csproj -nologo
