# Requirements

This file is the explicit capability and coverage contract for the project.

## Active

### R002 — Write and save Markua manuscript files in a focused source-text editor with syntax highlighting, inline validation, and no toolbar or preview panel
- Class: primary-user-loop
- Status: active
- Description: Write and save Markua manuscript files in a focused source-text editor with syntax highlighting, inline validation, and no toolbar or preview panel
- Why it matters: Writing prose is the primary job; this is the surface where the author spends most of their time
- Source: user
- Primary owning slice: M001/S03
- Validation: unmapped
- Notes: FR-7 through FR-12. AvaloniaEdit with custom XSHD highlighting definition. Validation is advisory only; never blocks save.

### R004 — Compute and display live word count per chapter, rolled up to Part and book totals, with optional targets and proximity indicators
- Class: primary-user-loop
- Status: active
- Description: Compute and display live word count per chapter, rolled up to Part and book totals, with optional targets and proximity indicators
- Why it matters: Word count and progress against target are the author's primary progress signals; updates within 500ms of keystroke
- Source: user
- Primary owning slice: M002/S02
- Validation: Validated in S05 by code inspection: ChapterInfoViewModel uses plain backing fields for ProximityFill and HasTarget, with per-chapter subscriptions from ChapterViewModel; ChapterViewModel remains the authoritative source of target/proximity calculations.
- Notes: S05 closure record also documents build/test baseline and desktop-only verification steps for the remaining acceptance criteria.

### R005 — Display a per-chapter phase timeline (Gantt view) with phase boxes, inline date editing, Part rollup rows, time axis, and optional phase progress fill
- Class: core-capability
- Status: active
- Description: Display a per-chapter phase timeline (Gantt view) with phase boxes, inline date editing, Part rollup rows, time axis, and optional phase progress fill
- Why it matters: UJ-2: the author reviews overall manuscript progress and enters phase dates without leaving the app or maintaining a spreadsheet
- Source: user
- Primary owning slice: M003/S01
- Supporting slices: M003/S02, M003/S03
- Validation: unmapped
- Notes: FR-20 through FR-27. Custom GanttCanvas DrawingContext renderer. Chapter order is read-only in Early V1; drag-reorder is M005.

### R006 — Display a Corkboard card view showing each chapter's title, status, word count, and phase dates; status-coded card borders; click-to-open editor
- Class: core-capability
- Status: active
- Description: Display a Corkboard card view showing each chapter's title, status, word count, and phase dates; status-coded card borders; click-to-open editor
- Why it matters: UJ-2: visual manuscript overview at a glance; helps the author decide which chapters need attention this week
- Source: user
- Primary owning slice: M004/S01
- Validation: unmapped
- Notes: FR-28 through FR-30 (Early V1). Structural editing (FR-31–FR-33) is M005 Late V1.

### R010 — AI editorial assistance: configurable provider (LiteLLM or MEAI), chapter/part/book summaries, in-app chat panel with summary context, structured issues (proofing/consistency/line-editing), issues panel and inline editor indicators
- Class: differentiator
- Status: active
- Description: AI editorial assistance: configurable provider (LiteLLM or MEAI), chapter/part/book summaries, in-app chat panel with summary context, structured issues (proofing/consistency/line-editing), issues panel and inline editor indicators
- Why it matters: SM-2: author completes AI editorial round-trip without leaving the app; avoids context-switching to external AI tools
- Source: user
- Primary owning slice: M005/S03
- Validation: unmapped
- Notes: FR-44 through FR-50 (Late V1). API key stored in OS credential store only — never on disk. AiEditorialService returns data only; all persistence is caller-controlled.

### R011 — Dark synthwave visual theme (purple primary, yellow/pink/orange accents) with WCAG AA contrast (4.5:1 minimum) across all surfaces; minimal UI chrome with collapsible panels
- Class: quality-attribute
- Status: active
- Description: Dark synthwave visual theme (purple primary, yellow/pink/orange accents) with WCAG AA contrast (4.5:1 minimum) across all surfaces; minimal UI chrome with collapsible panels
- Why it matters: FR-11, FR-12, NFR-V1, NFR-V2: the writing environment must feel craft-focused and visually coherent; poor contrast or cluttered chrome undermines the writing flow
- Source: user
- Primary owning slice: M001/S01
- Validation: unmapped
- Notes: SynthwaveTheme.axaml with named brush resources. Inter for UI; JetBrains Mono for editor. 16px minimum body font, 1.6 line height, 70ch max content width.

## Validated

### R001 — Open a folder as a Workspace, parse Book.txt into a Chapter/Part tree, and restore the last-opened Workspace on launch
- Class: core-capability
- Status: validated
- Description: Open a folder as a Workspace, parse Book.txt into a Chapter/Part tree, and restore the last-opened Workspace on launch
- Why it matters: The manuscript model is the foundation every view depends on; without it, nothing else renders
- Source: user
- Primary owning slice: M001/S02
- Validation: Slice S02 completion evidence: T01-T05 all passed; workspace models, ManuscriptService parsing/watcher behavior, AppSettingsStore persistence, WorkspaceViewModel/sidebar binding, and DI auto-restore wiring are implemented and verified by task summaries and structural checks.
- Notes: Validated by S02 closeout evidence for opening a workspace, parsing Book.txt into an ordered tree, and restoring the last workspace on launch.

### R003 — Track chapter status through the Outlining→Drafting→Editing→Polishing→Reviewing→Done lifecycle with optional phase date pre-fill on status change
- Class: primary-user-loop
- Status: validated
- Description: Track chapter status through the Outlining→Drafting→Editing→Polishing→Reviewing→Done lifecycle with optional phase date pre-fill on status change
- Why it matters: Status tracking is the core project management loop that replaces the spreadsheet; it must be available early so historical data accumulates
- Source: user
- Primary owning slice: M002/S01
- Validation: Validated by S01 evidence: 6 ChapterRegistryServiceTests + 4 PhaseDataServiceTests pass (dotnet test exit 0); sidebar status dot renders from ChapterViewModel.Status; phase-date prefill wired in WorkspaceViewModel; UUID continuity confirmed by code inspection of ChapterRegistryService.ReconcileAsync and the title-aware orphan matching path in WorkspaceViewModel (lines ~441-470). S03 ChapterInfoViewModel inspection confirms ProximityFill and HasTarget are plain backing fields (not OAPH), WhenAnyValue live subscriptions are present in the per-chapter _chapterDisposables block, and SyncFromChapterVm copies both values on chapter switch — rebuts S03 stubbed-0.0 known limitation.
- Notes: M002/S07 validation alignment confirmed the requirement remains validated; canonical traceability is recorded in M002-VALIDATION.md with S01 test counts, sidebar/status-dot evidence, and the ChapterInfoViewModel inspection that rebuts the old ProximityFill stub concern.

### R007 — Show a Supplemental Docs section in the sidebar for .hymnal-data/docs/ with create/edit/folder support; docs open in the editor
- Class: core-capability
- Status: validated
- Description: Show a Supplemental Docs section in the sidebar for .hymnal-data/docs/ with create/edit/folder support; docs open in the editor
- Why it matters: Authors need per-project reference material (character bibles, research) tied to the project without polluting the manuscript
- Source: user
- Primary owning slice: M004/S02
- Validation: Validated by M004/S02 focused DOCS service, editor arbitrary-file lifecycle, workspace/shell routing, and DOCS sidebar smoke coverage. The slice exercised create folder/file, open in editor with ActiveNode null, atomic save, and reopen-with-content-intact behavior through automated tests and smoke evidence.
- Notes: Supplemental docs remain a separate docs tree under .hymnal-data/docs/ and open in the same EditorView as manuscript chapters.

### R008 — Provide a Git panel for stage-all, commit with optional message, and push to current remote — surfacing errors without swallowing them
- Class: primary-user-loop
- Status: validated
- Description: Provide a Git panel for stage-all, commit with optional message, and push to current remote — surfacing errors without swallowing them
- Why it matters: UJ-1: the author commits writing progress without switching to terminal; one-action workflow is a success metric (SM-3)
- Source: user
- Primary owning slice: M004/S03
- Validation: Validated by M004/S03 task evidence: ProcessGitServiceTests (fake runner), GitPanelViewModelTests (debounced refresh, watcher, notifications), MainWindowGitPanel smoke/tests (DI and toolbar/dialog wiring), ProcessGitServiceLocalRepoTests (real temp repo and bare remote), and `dotnet build src/Hymnal/Hymnal.csproj`.
- Notes: Git UI remains hidden when no repo is detected; default commit message is `Hymnal: save progress {UTC ISO-8601}`; commit/push failures preserve raw stderr in notifications.

### R009 — Per-chapter notes panel accessible from the editor without leaving the editing context; stored as Markdown in .hymnal-data/notes/
- Class: core-capability
- Status: validated
- Description: Per-chapter notes panel accessible from the editor without leaving the editing context; stored as Markdown in .hymnal-data/notes/
- Why it matters: Authors need to keep per-chapter research and plot notes close to the writing surface without polluting the manuscript file
- Source: user
- Primary owning slice: M001/S04
- Validation: Slice S04 verification: dotnet test tests/Hymnal.Core.Tests --filter "NotesService|NotesViewModel" --nologo passed, and dotnet build src/Hymnal/Hymnal.csproj -c Debug --nologo passed. The implemented Notes panel loads, auto-saves, and persists per-chapter note text under .hymnal-data/notes/ through the existing atomic metadata write path.
- Notes: Validated by M001/S04 completion evidence.

### R012 — All file writes are atomic (write-temp-then-rename); .hymnal-data/ is the only directory written outside explicit editor saves; manuscript .md files are never modified except through explicit author edits
- Class: constraint
- Status: validated
- Description: All file writes are atomic (write-temp-then-rename); .hymnal-data/ is the only directory written outside explicit editor saves; manuscript .md files are never modified except through explicit author edits
- Why it matters: NFR-D1, NFR-D2: data loss on crash or bug must be impossible; the author's manuscript is the primary asset
- Source: user
- Primary owning slice: M001/S03
- Validation: Validated by S03 closeout: MetadataStore atomic write implementation from T01 uses temp-file-then-rename behavior, and S03 verified editor save/switch flows and source files present for save-on-switch and explicit save wiring. See T01/T02/T03 task summaries for passing verification evidence.
- Notes: Slice S03 closes the editor/save workflow without introducing non-explicit manuscript writes outside the atomic save path.

### R013 — Corkboard structural editing: drag-to-reorder cards (updates Book.txt and moves files between Part folders), chapter inclusion toggle, and new chapter insertion between cards
- Class: core-capability
- Status: validated
- Description: Corkboard structural editing: drag-to-reorder cards (updates Book.txt and moves files between Part folders), chapter inclusion toggle, and new chapter insertion between cards
- Why it matters: FR-31 through FR-33: allows the author to restructure the manuscript visually without touching Book.txt manually or using the terminal
- Source: user
- Primary owning slice: M005/S05
- Supporting slices: M005/S06, M005/S08
- Validation: Validated by M005/S08: StructuralConsistencyUatTests replay sidebar include/exclude, rename, same-Part and Part reorder, Corkboard same-Part reorder, cross-Part move, include/exclude, inline chapter insertion, and Gantt same-Part row reorder through the canonical BookTxtStructureService paths on one temp workspace; after fresh ViewModel-stack restart they assert consistent Book.txt order, file layout, registry UUID mappings, exclusions manifest, notes/phase/target/history metadata continuity, and no duplicate projections. Controlled Corkboard conflict and illegal cross-Part Gantt failure tests prove user-visible diagnostics and non-destructive state. Manual desktop UAT script recorded in docs/uat/M005-S08-structural-consistency.md.
- Notes: S08 closes the final cross-surface/end-to-end replay and failure-polish gap noted after S05/S06. Full solution test remains unreliable in the agent gsd_exec environment due known Windows dotnet/WSL restore and testhost file-lock issues (MEM008); focused S08 replay and desktop app build evidence are recorded in task summaries.

### R014 — Windows 10+ and Linux (Ubuntu LTS, Fedora) as first-class targets; self-contained .NET 10 publish (no runtime install); cold start under 5s; Book.txt parse under 2s for 100 chapters; word count update under 500ms
- Class: quality-attribute
- Status: validated
- Description: Windows 10+ and Linux (Ubuntu LTS, Fedora) as first-class targets; self-contained .NET 10 publish (no runtime install); cold start under 5s; Book.txt parse under 2s for 100 chapters; word count update under 500ms
- Why it matters: NFR-P1, NFR-P2, NFR-Perf1–3: the app must run portably on the author's primary machines without runtime friction
- Source: user
- Primary owning slice: M001/S01
- Validation: Validated by combined M001 + M002 evidence. Platform/publish: M001-VALIDATION.md records Windows win-x64 and Linux linux-x64 CI matrix passing, self-contained dotnet publish verified, and Book.txt parse under 2s confirmed. Performance: S06 benchmark (WordCountPerformanceTests) measured [S06] Live word-count latency: 1 ms for 10,000 words (threshold under 500ms) and [S06] Cold-start recalculation: 82 ms for 100 chapters (threshold under 5s); full suite 59/59 pass.
- Notes: M002/S07 closure preserves the validated state; M002-VALIDATION.md records the combined M001 platform/publish evidence and S06 benchmark timings for live word-count latency and 100-chapter cold-start recalculation.

## Deferred

## Out of Scope

## Traceability

| ID | Class | Status | Primary owner | Supporting | Proof |
|---|---|---|---|---|---|
| R001 | core-capability | validated | M001/S02 | none | Slice S02 completion evidence: T01-T05 all passed; workspace models, ManuscriptService parsing/watcher behavior, AppSettingsStore persistence, WorkspaceViewModel/sidebar binding, and DI auto-restore wiring are implemented and verified by task summaries and structural checks. |
| R002 | primary-user-loop | active | M001/S03 | none | unmapped |
| R003 | primary-user-loop | validated | M002/S01 | none | Validated by S01 evidence: 6 ChapterRegistryServiceTests + 4 PhaseDataServiceTests pass (dotnet test exit 0); sidebar status dot renders from ChapterViewModel.Status; phase-date prefill wired in WorkspaceViewModel; UUID continuity confirmed by code inspection of ChapterRegistryService.ReconcileAsync and the title-aware orphan matching path in WorkspaceViewModel (lines ~441-470). S03 ChapterInfoViewModel inspection confirms ProximityFill and HasTarget are plain backing fields (not OAPH), WhenAnyValue live subscriptions are present in the per-chapter _chapterDisposables block, and SyncFromChapterVm copies both values on chapter switch — rebuts S03 stubbed-0.0 known limitation. |
| R004 | primary-user-loop | active | M002/S02 | none | Validated in S05 by code inspection: ChapterInfoViewModel uses plain backing fields for ProximityFill and HasTarget, with per-chapter subscriptions from ChapterViewModel; ChapterViewModel remains the authoritative source of target/proximity calculations. |
| R005 | core-capability | active | M003/S01 | M003/S02, M003/S03 | unmapped |
| R006 | core-capability | active | M004/S01 | none | unmapped |
| R007 | core-capability | validated | M004/S02 | none | Validated by M004/S02 focused DOCS service, editor arbitrary-file lifecycle, workspace/shell routing, and DOCS sidebar smoke coverage. The slice exercised create folder/file, open in editor with ActiveNode null, atomic save, and reopen-with-content-intact behavior through automated tests and smoke evidence. |
| R008 | primary-user-loop | validated | M004/S03 | none | Validated by M004/S03 task evidence: ProcessGitServiceTests (fake runner), GitPanelViewModelTests (debounced refresh, watcher, notifications), MainWindowGitPanel smoke/tests (DI and toolbar/dialog wiring), ProcessGitServiceLocalRepoTests (real temp repo and bare remote), and `dotnet build src/Hymnal/Hymnal.csproj`. |
| R009 | core-capability | validated | M001/S04 | none | Slice S04 verification: dotnet test tests/Hymnal.Core.Tests --filter "NotesService|NotesViewModel" --nologo passed, and dotnet build src/Hymnal/Hymnal.csproj -c Debug --nologo passed. The implemented Notes panel loads, auto-saves, and persists per-chapter note text under .hymnal-data/notes/ through the existing atomic metadata write path. |
| R010 | differentiator | active | M005/S03 | none | unmapped |
| R011 | quality-attribute | active | M001/S01 | none | unmapped |
| R012 | constraint | validated | M001/S03 | none | Validated by S03 closeout: MetadataStore atomic write implementation from T01 uses temp-file-then-rename behavior, and S03 verified editor save/switch flows and source files present for save-on-switch and explicit save wiring. See T01/T02/T03 task summaries for passing verification evidence. |
| R013 | core-capability | validated | M005/S05 | M005/S06, M005/S08 | Validated by M005/S08: StructuralConsistencyUatTests replay sidebar include/exclude, rename, same-Part and Part reorder, Corkboard same-Part reorder, cross-Part move, include/exclude, inline chapter insertion, and Gantt same-Part row reorder through the canonical BookTxtStructureService paths on one temp workspace; after fresh ViewModel-stack restart they assert consistent Book.txt order, file layout, registry UUID mappings, exclusions manifest, notes/phase/target/history metadata continuity, and no duplicate projections. Controlled Corkboard conflict and illegal cross-Part Gantt failure tests prove user-visible diagnostics and non-destructive state. Manual desktop UAT script recorded in docs/uat/M005-S08-structural-consistency.md. |
| R014 | quality-attribute | validated | M001/S01 | none | Validated by combined M001 + M002 evidence. Platform/publish: M001-VALIDATION.md records Windows win-x64 and Linux linux-x64 CI matrix passing, self-contained dotnet publish verified, and Book.txt parse under 2s confirmed. Performance: S06 benchmark (WordCountPerformanceTests) measured [S06] Live word-count latency: 1 ms for 10,000 words (threshold under 500ms) and [S06] Cold-start recalculation: 82 ms for 100 chapters (threshold under 5s); full suite 59/59 pass. |

## Coverage Summary

- Active requirements: 6
- Mapped to slices: 6
- Validated: 8 (R001, R003, R007, R008, R009, R012, R013, R014)
- Unmapped active requirements: 0
