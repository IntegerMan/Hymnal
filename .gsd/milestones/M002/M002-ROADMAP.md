# M002: Status Tracking and Word Count

**Vision:** Replace the author's tracking spreadsheet with in-app chapter status lifecycle management and live word count, accumulating project management data from the first writing session in M002. By the end of this milestone the author can see a coloured status dot next to every chapter, open a Chapter Info pane (F3) to set status and phase dates, watch word count update as they type, and see an advisory margin indicator for known Markua issues — all persisted across renames and restarts via UUID-keyed JSON files in .hymnal-data/.

## Success Criteria

- Change chapter status from Outlining to Drafting: sidebar dot updates immediately, today's date pre-fills as phase start, status persists after app restart
- Rename a chapter .md file and update Book.txt: relaunch shows status and word count intact (UUID identity preserved via chapter-registry.json)
- Open chapter, write 100 words, Ctrl+S: word count updates in Chapter Info pane, Part and book totals update in sidebar, wordcount-history.json gains a new entry for today
- Set word count target of 4,000 on a 2,000-word chapter: sidebar proximity bar shows ~50% fill
- dotnet build exits 0; all ChapterRegistryServiceTests, WordCountServiceTests, PhaseDataServiceTests, and TargetsServiceTests pass

## Slices

- [x] **S01: Chapter Registry and Status Lifecycle** `risk:Highest — introduces ChapterViewModel wrapper that changes the existing ChapterNode sidebar binding established in M001; registry reconciliation logic (rename, orphan) is novel ground with no prior art in this codebase.` `depends:[]`
  > After this: Open workspace: each chapter in sidebar shows a coloured dot (grey = Outlining, sky = Drafting, violet = Editing, amber = Polishing, pink = Reviewing, emerald = Done). Change a chapter status to Drafting: dot updates immediately; phase start date pre-fills with today. Close and reopen the app: dot and phase date survive the restart. Rename the .md file and update Book.txt, relaunch: the UUID-keyed status is still intact. dotnet test --filter ChapterRegistry and --filter PhaseData both exit 0.

- [x] **S02: Word Count Targets and Rollup** `risk:Medium — reactive CombineLatest rollup across many ChapterViewModels may have subscription overhead; WordCountService Markua-directive exclusion logic needs careful tokenisation.` `depends:[S01]`
  > After this: Open a chapter, type 50 words, wait 300ms: word count displayed in sidebar updates. Ctrl+S: Part and book totals update; wordcount-history.json gains a new entry for today's date. Set a 4,000-word target on a 2,000-word chapter: sidebar proximity bar shows ~50% fill. dotnet test --filter WordCount and --filter Targets both exit 0.

- [x] **S03: Chapter Info Pane and Validation Margin** `risk:Medium-low — AbstractMargin API surface untested in this codebase; fall back to IBackgroundRenderer if needed. F3 pane mirrors established Notes pane pattern so shell integration is low-risk.` `depends:[S01,S02]`
  > After this: Press F3: Chapter Info pane opens on the right rail showing the chapter's current status (Drafting), today as phase start date, live word count, and target if set. Change status to Editing in the pane: phase start date auto-fills with today. Open a Markua file with a blank line before a {sample: true} heading: an advisory dot appears in the editor gutter (no crash, no blocking). GridSplitter resizes both panes.

- [x] **S04: Runtime Stabilization and Chapter Info Wiring** `risk:High` `depends:[S03]`
  > After this: Launch the app successfully, open a workspace, use F3 without crashing, and see Chapter Info target/proximity state driven by the live chapter data instead of a stub.

- [x] **S05: Desktop UAT and Operational Verification** `risk:Medium` `depends:[S04]`
  > After this: Run the desktop verification flow end-to-end: status survives restart and rename, word count save updates pane/sidebar/history, target shows expected proximity, validation gutter appears, and latency/cold-start evidence is recorded for validation.

- [x] **S06: Operational Benchmark Evidence** `risk:Medium` `depends:[S05]`
  > After this: After this: measured evidence exists for live word-count latency on a 10,000-word chapter and cold-start timing for a 100-chapter workspace, with results captured in milestone artifacts for re-validation.

- [x] **S07: Validation Alignment and Closure** `risk:High` `depends:[S06]`
  > After this: After this: M002 has a coherent requirement and assessment closure story — touched requirements are explicitly mapped, S01/S02/S03 remediation evidence is recorded or slices are reopened as needed, and the milestone is ready for another validation pass.

## Boundary Map

```
┌─ Hymnal.Core (zero Avalonia refs — compile-enforced) ──────────────────┐
│  Models:    ChapterStatus, PhaseData, WordCountTarget                   │
│  Services:  ChapterRegistryService, PhaseDataService,                   │
│             WordCountService, WordCountHistoryService, TargetsService    │
│  Infra:     MetadataStore (atomic write), AppSettingsStore              │
└────────────────────────────────────────────────────────────────────────┘
         │ Result<T>  │ interfaces              ↑ reads .md files
┌─ Hymnal (UI / Avalonia) ───────────────────────────────────────────────┐
│  ViewModels: ChapterViewModel (new wraps ChapterNode),                  │
│              WorkspaceViewModel (updated — registry, phase, rollup),    │
│              EditorViewModel (live count debounce),                     │
│              ChapterInfoViewModel (new)                                 │
│  Views:      SidebarView (status dot, proximity bar — updated),         │
│              ChapterInfoView (new — F3 pane),                           │
│              ValidationMargin (new — AbstractMargin)                    │
└────────────────────────────────────────────────────────────────────────┘
         │ atomic JSON reads/writes
┌─ .hymnal-data/ JSON files ─────────────────────────────────────────────┐
│  chapter-registry.json  (schemaVersion:1, UUID → path + orphaned)      │
│  phases.json            (schemaVersion:1, UUID → status + dates)        │
│  targets.json           (schemaVersion:1, UUID → min/max word count)    │
│  wordcount-history.json (schemaVersion:1, UUID + date → word count)     │
└────────────────────────────────────────────────────────────────────────┘
         │ xUnit tests (no Avalonia)
┌─ Hymnal.Core.Tests ────────────────────────────────────────────────────┐
│  ChapterRegistryServiceTests, PhaseDataServiceTests,                    │
│  WordCountServiceTests, TargetsServiceTests                             │
└────────────────────────────────────────────────────────────────────────┘
```
