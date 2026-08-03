---
id: S04
milestone: M002
status: ready
---

# S04: Runtime Stabilization and Chapter Info Wiring — Context

## Goal

Fix the startup crash caused by OAPH initialization order in `ChapterInfoViewModel`, wire `ProximityFill` to live `ChapterViewModel` data, and perform a defensive sweep of the ChapterInfo and MainWindow ViewModels for any remaining stubs or null-unsafe patterns.

## Why this Slice

S03 completed the Chapter Info pane and validation margin but shipped two known defects: a `NullReferenceException` in `ChapterInfoViewModel.HasTarget` that prevents the app from launching at all, and a `ProximityFill` value permanently stubbed at `0.0`. Both must be resolved before S05 can run the full desktop UAT. This is the only thing standing between a buildable codebase and a runnable one.

## Scope

### In Scope

- **Fix the startup crash**: swap OAPH initialization order in `ChapterInfoViewModel` constructor so `_hasTarget` is initialized before `_proximityFill`. The crash path: `_proximityFill` OAPH calls `WhenAnyValue(x => x.HasTarget, ...)`, which immediately reads `_hasTarget.Value`, but `_hasTarget` is still `null` at that point in the constructor.
- **Wire live ProximityFill**: replace the `ComputeProximityFill` stub (always returns `0.0`) by subscribing to `chapterVm.WhenAnyValue(x => x.ProximityFill)` in the per-chapter subscription block inside `OnActiveNodeChanged`, and pushing the value into a simple backing field (same pattern as `WordCount`, `Status`, `TargetDisplay`). Remove or simplify the `_proximityFill` OAPH accordingly.
- **Simplify `_hasTarget` if it reduces risk**: the current OAPH derives `HasTarget` from `TargetDisplay` string content. Consider replacing it with a simple property set from `chapterVm.WhenAnyValue(x => x.HasTarget)` to match the per-chapter property pattern and eliminate the string-parsing logic.
- **Defensive sweep**: check `ChapterInfoViewModel` and `MainWindowViewModel` for any other stubs, commented-out wiring, or null-unsafe patterns. Document findings; fix anything that would crash or produce silent wrong behavior.
- **Full build + test run**: `dotnet build src/Hymnal/Hymnal.csproj -nologo` exits 0; `dotnet test Hymnal.sln -nologo` passes all 57+ tests with 0 failures.

### Out of Scope

- New UI features or layout changes in the Chapter Info pane or right rail
- `CredentialStoreStub` — explicitly deferred to a future milestone, not a runtime risk
- `ThrownExceptions` subscriptions on `ToggleSidebarCommand` and `ExitCommand` in `MainWindowViewModel` — both are synchronous `ReactiveCommand.Create()` with no throw paths; low-risk
- S05 UAT checks (full desktop smoke test deferred to S05)

## Constraints

- Do not change the public API of `ChapterInfoViewModel` — view bindings in `ChapterInfoView.axaml` and `MainWindow.axaml` must remain intact
- `Hymnal.Core` must remain free of Avalonia package references (compile-enforced boundary)
- All `.hymnal-data/` writes must continue through `MetadataStore` atomic path; do not introduce direct `File.WriteAllText` calls

## Integration Points

### Consumes

- `src/Hymnal/ViewModels/ChapterInfoViewModel.cs` — the crash site; OAPH ordering fix and ProximityFill rewiring happen here
- `src/Hymnal/ViewModels/ChapterViewModel.cs` — `ProximityFill` (live 0.0–1.0 fill fraction) and `HasTarget` (bool) are the authoritative sources; `ChapterInfoViewModel` subscribes to them in the per-chapter block
- `src/Hymnal/ViewModels/MainWindowViewModel.cs` — swept for stubs/nulls; likely no changes needed

### Produces

- `src/Hymnal/ViewModels/ChapterInfoViewModel.cs` — crash-free startup; `ProximityFill` and `HasTarget` driven from live `ChapterViewModel` state
- Clean build and full test suite pass as pre-condition for S05 UAT

## Open Questions

- **OAPH vs. backing field for ProximityFill**: Simplest fix is to drop the `_proximityFill` OAPH entirely and use a `double _proximityFill` backing field set in the per-chapter subscription (same as `WordCount`). This also eliminates the OAPH ordering hazard class for future maintenance. Current thinking: prefer this approach — remove the OAPH, add the subscription, keep the property getter using `RaiseAndSetIfChanged`.
- **HasTarget simplification**: Same choice for `_hasTarget` OAPH. Current thinking: replace with `bool _hasTarget` backing field set from `chapterVm.WhenAnyValue(x => x.HasTarget)` in the per-chapter block to eliminate the string-content derivation and the OAPH ordering risk.
