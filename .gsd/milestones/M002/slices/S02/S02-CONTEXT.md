---
id: S02
milestone: M002
status: ready
---

# S02: Word Count Targets and Rollup — Context

## Goal

Add live word count to the open chapter (debounced 300ms), persist per-chapter counts on save, compute Part and book totals reactively from `ChapterViewModel` wrappers, display them in the sidebar, and allow the author to set per-chapter min/max targets with a proximity fill bar — all keyed by UUID and accumulating history in `wordcount-history.json`.

## Why this Slice

S02 builds directly on the `ChapterViewModel` wrappers and UUID identity introduced in S01. Word count is the author's primary daily progress signal and must start accumulating history as soon as the author opens Hymnal. Deferring to S03 would mean the first writing sessions go unrecorded. S02 also unblocks S03: the Chapter Info pane needs a live `WordCount` and `Target` property already on `ChapterViewModel` to display.

## Scope

### In Scope

- `WordCountService` (in `Hymnal.Core/Services/`): whitespace tokenizer; counts all words on all lines **except** lines that start with `{` (Markua attribute/directive lines); `#` heading lines count normally; handles empty/whitespace-only content; returns `int`
- `WordCountTarget` model (in `Hymnal.Core/Models/`): `minWords` (nullable int) and `maxWords` (nullable int); either or both may be null; proximity fill is calculated against `maxWords` if set, otherwise against `minWords`
- `WordCountHistoryEntry` model (in `Hymnal.Core/Models/`): `uuid`, `date` (ISO 8601 date-only, e.g. `"2026-05-30"`), `wordCount`
- `TargetsService` (in `Hymnal.Core/Services/`): load/save `targets.json` (keyed by UUID); `GetTarget(uuid)` returns null gracefully if no entry; atomic write via `MetadataStore`
- `WordCountHistoryService` (in `Hymnal.Core/Services/`): append-on-save to `wordcount-history.json`; last-write-wins deduplication per `(UUID, date)` pair (today's last save wins for that day)
- Live word count in `EditorViewModel`: debounced 300ms on `Text` changes via `WhenAnyValue(x => x.Text).Throttle(300ms)`; exposed as `LiveWordCount` (`ObservableAsPropertyHelper<int>`)
- Explicit post-save `IObservable<Unit> Saved` on `EditorViewModel`: fires after `OriginalText = Text` on successful save — S02 adds this as a public observable so `WorkspaceViewModel` and `ChapterInfoViewModel` (S03) can subscribe without observing `OriginalText` changes directly
- `ChapterViewModel` additions (building on S01): `WordCount` (`ObservableAsPropertyHelper<int>`, updated from persisted value on load and from `EditorViewModel.LiveWordCount` when this chapter is active), `Target` (`WordCountTarget?`, loaded from `TargetsService`), `SetTargetCommand` (`ReactiveCommand<WordCountTarget?, Unit>`)
- Background word count recalculation: at workspace load, chapters with no saved count in `phases.json` are recalculated from their `.md` file using `WordCountService` in a `Task.Run` background task; each chapter recalculates independently (per-chapter failure is silent; contributes `—` to display until resolved); does not block workspace load
- `WorkspaceViewModel` additions: `TotalWordCount` (OAPH, reactive sum of all `ChapterViewModel.WordCount` items); `Saved` event hookup — on `EditorViewModel.Saved`, persist updated count for the active chapter and append `wordcount-history.json` entry; Part totals computed from contiguous chapter ranges after each Part node
- Sidebar `SidebarView.axaml` updates:
  - **Chapter row**: right-aligned word count (`2,130 w`); uncached chapters show `—`; proximity fill bar (row-fill background at ~20% opacity) if a `Target` is set — fill = `min(wordCount / maxWords, 1.0)` (capped at 100%)
  - **Part header row**: right-aligned Part total (sum of its chapter counts)
  - **"CHAPTERS" header**: book total shown inline next to the "CHAPTERS" label (e.g. `CHAPTERS  12,450 w`)
- Right-click context menu on chapter rows: "Set Status ▶" (S01) + "Set Target…" (S02 new); "Set Target…" opens a small inline popup with min and max word count fields (both optional); [Set] saves, [Clear] removes the target, [Cancel] dismisses; closes on click outside
- `TargetsServiceTests` and `WordCountServiceTests` in `Hymnal.Core.Tests`

### Out of Scope

- Chapter Info pane (F3) display of word count and target — S03
- Phase-date pre-fill toggle UI — S03
- Min/max range UI in the Chapter Info pane — S03 (S02 stores the full model; S03 displays it in the pane)
- Progress chart / writing velocity visualisation — data accumulates in `wordcount-history.json`; display deferred to a future milestone
- `wordcount-history.json` compaction/pruning — M002 writes without pruning; future concern
- `ValidationMargin` — S03
- Book-total word count target (a target for the whole book, not a chapter) — not in M002

## Constraints

- `Hymnal.Core.csproj` must retain zero Avalonia references; `WordCountService`, `TargetsService`, `WordCountHistoryService`, and all new models are pure .NET
- All `.hymnal-data/` writes (targets.json, wordcount-history.json) go through `MetadataStore.WriteTextAtomicAsync`
- JSON serializer options for M002 files: `JsonStringEnumConverter`, `JsonIgnoreCondition.WhenWritingNull`, `PropertyNamingPolicy.CamelCase`, `"schemaVersion": 1` — same custom options as S01's services; do not copy `AppSettingsStore` options
- Live word count latency: ≤ 500ms from keystroke to displayed count (debounce at 300ms leaves 200ms for propagation)
- Workspace load must not block on word count recalculation — background tasks per chapter; cold-start budget remains < 5s
- `WordCountTarget.minWords` and `WordCountTarget.maxWords` are both nullable; proximity fill uses `maxWords` if set, falls back to `minWords` if only `minWords` is set; if neither is set the target is meaningless (treat as no target)
- `ReactiveCommand.ThrownExceptions` subscribed on all new commands including `SetTargetCommand`
- Uncached chapter counts (background recalculation not yet complete): display `—` in the sidebar; no fill bar; contribute 0 to Part/book total while pending

## Integration Points

### Consumes

- `src/Hymnal/ViewModels/ChapterViewModel.cs` (S01) — adds `WordCount`, `Target`, `SetTargetCommand` properties on top of S01's `Status`/`ChangeStatusCommand`
- `src/Hymnal/ViewModels/EditorViewModel.cs` — adds `LiveWordCount` (OAPH) and `IObservable<Unit> Saved`; `Saved` fires after successful atomic write
- `src/Hymnal/ViewModels/WorkspaceViewModel.cs` — adds `TotalWordCount` OAPH; subscribes to `EditorViewModel.Saved` to persist count and append history
- `src/Hymnal.Core/Infrastructure/MetadataStore.cs` — write path for `targets.json` and `wordcount-history.json`
- `src/Hymnal/Views/SidebarView.axaml` — further updated from S01 (adds count, Part total, CHAPTERS header total, fill bar, right-click menu extension)
- `src/Hymnal/Views/MainWindow.axaml` — no change needed for S02 (right pane refactor is S03)

### Produces

- `src/Hymnal.Core/Models/WordCountTarget.cs` — `{ minWords: int?, maxWords: int? }`; consumed by `ChapterViewModel` (S02) and `ChapterInfoViewModel` (S03)
- `src/Hymnal.Core/Models/WordCountHistoryEntry.cs` — `{ uuid, date, wordCount }`; consumed by `WordCountHistoryService`
- `src/Hymnal.Core/Services/WordCountService.cs` — consumed by background recalculation in `WorkspaceViewModel` and by `EditorViewModel` live count
- `src/Hymnal.Core/Services/TargetsService.cs` — consumed by `WorkspaceViewModel` (load/save targets) and `ChapterViewModel.SetTargetCommand`
- `src/Hymnal.Core/Services/WordCountHistoryService.cs` — consumed by `WorkspaceViewModel` on `EditorViewModel.Saved`
- `.hymnal-data/targets.json` — per-chapter min/max word count targets; keyed by UUID; consumed by S03's Chapter Info pane
- `.hymnal-data/wordcount-history.json` — daily word count history per chapter; consumed by future progress chart milestone
- `tests/Hymnal.Core.Tests/Services/WordCountServiceTests.cs`
- `tests/Hymnal.Core.Tests/Services/TargetsServiceTests.cs`

## Wordcount Rules Reference

| Line type | Counted? | Example |
|-----------|----------|------
