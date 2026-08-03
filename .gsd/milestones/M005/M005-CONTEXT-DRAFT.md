# M005: Late V1

**Status:** DRAFT — awaiting discussion before execution
**Depends on:** M004

## Seed Scope

M005 completes Late V1 with structural editing capabilities and AI editorial assistance.

### Corkboard Structural Editing (FR-31–FR-33)
- **Drag-to-reorder cards** (FR-31): dropping a card reorders chapters; if crossing a Part boundary, moves the `.md` file into the target Part subfolder; writes `Book.txt` atomically; updates sidebar and Gantt grouping
- **Chapter inclusion toggle** (FR-32): included chapters in `Book.txt`; excluded chapters tracked in `.hymnal-data/exclusions.json` for re-inclusion without re-entering filename (OQ-3 storage format to decide)
- **Chapter insertion from Corkboard** (FR-33): context menu / insertion affordance between cards; creates new `.md` file and adds to `Book.txt` at chosen position; author provides filename and optional title

### Gantt Drag-to-Reorder
- Drag rows to reorder chapters within the Gantt view (supersedes FR-27 read-only restriction)
- Same `Book.txt` atomic write as Corkboard drag

### AI Editorial Assistance (FR-44–FR-50)
- **Provider configuration** (FR-44): endpoint URL + model name in `AppSettingsStore`; API key in OS credential store (`ICredentialStore`) — never on disk
- **Generate and store summaries** (FR-45): Chapter/Part/book level; stored as Markdown in `.hymnal-data/summaries/`; regenerable on demand
- **AI chat panel with summary context** (FR-46): scoped to current chapter/part/book; relevant summaries included as context; exchange accumulates within session
- **Editorial query types** (FR-47): free-text primary; structured modes — Proofing, Consistency, Line Editing — each with defined prompt structure that parses into Issues
- **Structured Issues** (FR-48): stored in `.hymnal-data/issues.json`; fields: id, type, description, state (open/resolved/dismissed), created_date, optional location
- **Issues panel** (FR-49): filterable by scope/type/state; mark resolved or dismissed
- **Inline issue indicators** (FR-50): `IssueMargin` (AbstractMargin) on relevant editor lines; summary badges in sidebar; clicking opens Issues panel filtered to scope

## Key Technical Work

- `AiEditorialService` wrapping `IChatClient` (MEAI); returns data only (summary strings, Issue[] arrays)
- `SummaryService.StoreAsync()` → `MetadataStore` → `.hymnal-data/summaries/`
- `IssueStore` with `SourceCache<Issue, string>` (key = issue.Id); DynamicData pipeline to IssuesPanelViewModel
- `IssueMargin` extending `AbstractMargin` in AvaloniaEdit
- `WindowsCredentialStore` (PasswordVault) + `LinuxSecretServiceStore` (libsecret/SecretService D-Bus)
- Corkboard drag-and-drop: Avalonia drag-drop API; file move + Book.txt atomic rewrite
- Gantt drag-to-reorder: same Book.txt write path; row reorder in GanttViewModel
- `exclusions.json` storage design (resolves OQ-3)

## Risks

- `libsecret` on Linux: D-Bus availability varies across desktop environments; may need fallback
- AI provider endpoint compatibility: LiteLLM covers most cases, but prompt structures for Proofing/Consistency/Line Editing modes need calibration against real manuscript content
- Corkboard drag across Part boundaries requires file move on disk — must be atomic and rollback-safe
- OQ-1 (.hymnal-data/summaries/ and issues/ gitignore policy) should be resolved before M005 ships

## Requirements Covered

- R010 (AI Editorial Assistance) — primary
- R013 (Corkboard Structural Editing) — primary

## Open Questions for Discussion

- `libsecret` on Linux: use `SecretService` NuGet or raw D-Bus P/Invoke? What's the fallback if libsecret is unavailable (e.g. headless server)?
- OQ-1: Should Hymnal offer to add `.hymnal-data/summaries/` and `.hymnal-data/issues/` to `.gitignore`? Default policy?
- OQ-3: Excluded chapter storage format — commented `Book.txt` line, `exclusions.json`, or other?
- Issues panel navigation shape (OQ-6): flat filters or tree navigator with filters within a scope node?
- AI structured mode prompt templates: where do these live (embedded resources, `.hymnal-data/`, app config)?
