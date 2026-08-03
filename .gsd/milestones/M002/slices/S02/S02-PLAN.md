# S02: Word Count Targets and Rollup

**Goal:** Add live per-chapter word count (debounced 300ms), Part and book totals rolled up reactively from ChapterViewModel wrappers, optional min/max targets with a proximity fill bar, and daily count history persisted to wordcount-history.json — all keyed by UUID and visible in the sidebar immediately after workspace load.
**Demo:** Open a chapter, type 50 words, wait 300ms: word count displayed in sidebar updates. Ctrl+S: Part and book totals update; wordcount-history.json gains a new entry for today's date. Set a 4,000-word target on a 2,000-word chapter: sidebar proximity bar shows ~50% fill. dotnet test --filter WordCount and --filter Targets both exit 0.

## Must-Haves

- Open chapter, type 50 words, wait 400ms: sidebar word count updates from "—" to a formatted count
- Ctrl+S: Part and book totals update; wordcount-history.json gains a new entry for today
- Set a 4,000-word target on a 2,000-word chapter: proximity fill bar shows ~50% fill
- "Set Target…" flyout accepts PendingMinWords/PendingMaxWords, [Set] persists, [Clear] removes
- `dotnet build src/Hymnal/Hymnal.csproj -nologo` exits 0
- `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter WordCount` passes
- `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter Targets` passes

## Proof Level

- This slice proves: integration — build verifies cross-layer wiring; unit tests prove Core service contracts; sidebar display verified by visual inspection after workspace open

## Integration Closure

Upstream: ChapterViewModel (S01), EditorViewModel (M001), MetadataStore, WordCountService/TargetsService/WordCountHistoryService (already implemented in Core). New wiring: WordCountService injected into EditorViewModel; TargetsService + WordCountHistoryService + WordCountService injected into WorkspaceViewModel; new services registered in App.axaml.cs DI. Downstream unblocked: S03 ChapterInfoPane reads WordCount + Target already on ChapterViewModel.

## Verification

- No structured logging added. Failure mode: background word count Task.Run swallows per-chapter exceptions silently (chapter stays at "—" in sidebar). History append failures surfaced via INotificationService.ShowError in WorkspaceViewModel.

## Tasks

- [x] **T01: ViewModel layer — EditorViewModel.LiveWordCount/Saved and ChapterViewModel word-count state** `est:1.5h`
  Why: EditorViewModel is the live content buffer; S02 needs a 300ms-debounced word count while the author types, and a `Saved` observable so WorkspaceViewModel can persist counts and append history on each save rather than polling OriginalText. ChapterViewModel is the per-chapter data wrapper; it must carry word count state, a target, display-ready computed strings, a proximity fill double, and commands for the right-click target flyout.
  - Files: `src/Hymnal/ViewModels/EditorViewModel.cs`, `src/Hymnal/ViewModels/ChapterViewModel.cs`, `src/Hymnal/App.axaml.cs`
  - Verify: dotnet build src/Hymnal/Hymnal.csproj -nologo

- [x] **T02: WorkspaceViewModel orchestration — targets load, background word count, Saved subscription, totals rollup, DI wiring** `est:2h`
  Why: WorkspaceViewModel must coordinate the new services: load per-chapter targets at workspace open, trigger per-chapter background word count recalculation so the sidebar populates from '—' to real counts without blocking startup, subscribe to EditorViewModel.Saved to persist and record history, and maintain a TotalWordCount + Part totals that update whenever any chapter's count changes.
  - Files: `src/Hymnal/ViewModels/WorkspaceViewModel.cs`, `src/Hymnal/App.axaml.cs`
  - Verify: dotnet build src/Hymnal/Hymnal.csproj -nologo && dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter WordCount -nologo && dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter Targets -nologo

- [x] **T03: Sidebar AXAML — word count display, proximity fill bar, CHAPTERS total, Part totals, and Set Target flyout** `est:2h`
  Why: The sidebar currently shows status dots and titles. S02 adds the author's primary progress signal — live word counts, rollup totals, and target proximity bars — and the Set Target affordance so the author can set chapter targets without leaving the sidebar.
  - Files: `src/Hymnal/Views/SidebarView.axaml`, `src/Hymnal/ViewModels/ChapterViewModel.cs`, `src/Hymnal/Views/Converters/NodeKindConverters.cs`
  - Verify: dotnet build src/Hymnal/Hymnal.csproj -nologo && dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter WordCount -nologo && dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter Targets -nologo

## Files Likely Touched

- src/Hymnal/ViewModels/EditorViewModel.cs
- src/Hymnal/ViewModels/ChapterViewModel.cs
- src/Hymnal/App.axaml.cs
- src/Hymnal/ViewModels/WorkspaceViewModel.cs
- src/Hymnal/Views/SidebarView.axaml
- src/Hymnal/Views/Converters/NodeKindConverters.cs
