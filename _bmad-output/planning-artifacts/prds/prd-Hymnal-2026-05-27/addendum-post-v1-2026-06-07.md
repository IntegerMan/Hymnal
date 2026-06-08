# PRD Addendum — Post-V1 Features

**Date:** 2026-06-07
**Supersedes:** Nothing. This addendum extends the original PRD (`prd-Hymnal-2026-05-27/prd.md`) with scope reopened after the 2026-06-07 roadmap revamp.
**Status:** Draft — individual sections become actionable when their owning milestone enters planning.

---

## Context

The original PRD defined a V1 boundary (Early V1 + Late V1) covering FR-1–FR-50. The 2026-06-07 roadmap revamp extends the product roadmap beyond V1 with milestones M008–M010. This addendum documents the feature scope, rationale, and implementation guidance for those extensions so downstream dev agents do not treat them as V1 scope creep.

The four post-V1 feature areas are:
1. **Review Mode and Structured Analysis** (M008) — activating `ShellMode.Edit` as REVIEW; story-structure analysis templates
2. **Brainstorm and Mind Map** (M009) — visual ideation canvas
3. **Marketing Activity Tracker** (M010/S01–S02) — local launch task management
4. **LeanPub API Integration** (M010/S03–S04) — read-only metadata sync and preview build trigger

---

## Section 1: Review Mode and Structured Analysis (M008)

### PRD §5 Non-Goal Update

The original PRD §5 does not list "REVIEW center tab" as a non-goal — `ShellMode.Edit` was reserved as "reserved for later." This addendum formalizes its activation.

### New FRs

#### FR-51: REVIEW center tab activation
The `ShellMode.Edit` enum value is activated as a navigable center-panel mode. The nav button label is "REVIEW". A `ReviewViewModel` is dispatched as the center-panel content when the REVIEW tab is active. The tab is always enabled when a workspace is open.

#### FR-52: Issue inbox in REVIEW center panel
The REVIEW center panel hosts a filterable issue inbox showing all issues from `IssueStore`. Filters: scope (book / Part / chapter), type (grammar / inconsistency / line-editing / developmental / proofing / story-structure), state (open / resolved / dismissed). Clicking a line-level issue opens the relevant chapter in the editor and scrolls to the flagged line. Implements FR-49 in the REVIEW surface (FR-49 originally deferred to Late V1).

#### FR-53: Chapter review queue
The REVIEW center panel includes a chapter queue panel showing chapters currently in Reviewing status, each with an open issue count. Clicking a chapter row opens it in the editor.

#### FR-54: IAnalysisTemplate framework
Analysis templates implement `IAnalysisTemplate`: `Name`, `Description`, `SupportedScopes`, `BuildPrompt(scope, context) → string`, `ParseResponse(text) → (Issue[], narrativeReport)`. Templates are registered in DI as `IEnumerable<IAnalysisTemplate>` and shown in the REVIEW panel as a runnable list.

#### FR-55: Three-Act Structure analysis template
A built-in `IAnalysisTemplate` implementation that evaluates the manuscript at book or Part scope for three-act structure: setup, confrontation, and resolution beat coverage. Produces Issue[] records (type: story-structure) and a narrative report stored in `.hymnal-data/reviews/{scope}-three-act-{date}.md`.

#### FR-56: Story Grid beat sheet analysis template
A built-in `IAnalysisTemplate` implementation that evaluates the manuscript against the Story Grid's 15 core beats (opening image, theme stated, setup, catalyst, debate, break into 2, B-story, fun and games, midpoint, bad guys close in, all is lost, dark night of the soul, break into 3, finale, final image — adapted for literary fiction). Produces Issue[] records and a narrative report stored in `.hymnal-data/reviews/{scope}-story-grid-{date}.md`.

#### FR-57: Additional analysis templates (Hero's Journey, Consistency Pass)
At least two additional `IAnalysisTemplate` implementations shipped in M008: Hero's Journey arc evaluation and a Consistency Pass (character consistency, setting consistency, timeline consistency). Further templates are extensible by DI registration.

#### FR-58: Review mode right-panel detail pane
The right panel in REVIEW mode shows a detail pane for the selected issue (description, type, state, location, created date) or selected analysis run (report text, issues generated count, re-run button). This replaces the generic Notes/AI/Git slot in REVIEW mode only.

### OQ-6 Resolution

**OQ-6 (Issues panel navigation shape)** is resolved: flat filters with scope chips in the REVIEW center panel. The Issues panel is not a hierarchical tree navigator. Scope is one filter axis (book / Part / chapter chip row at top of the inbox). This decision is canonical for M008/S01 implementation.

---

## Section 2: Brainstorm and Mind Map (M009)

### PRD §5 Non-Goal Update

The original PRD §5 does not list mind-mapping as a non-goal; it was simply out of scope for V1. This addendum adds it to the roadmap without conflicting with any V1 non-goal.

### New FRs

#### FR-59: Mind-map canvas
A custom Avalonia canvas surface (or third-party Avalonia diagramming control if available) where the author can create nodes, draw edges, drag nodes freely, and pan/zoom the canvas.

#### FR-60: Mind-map persistence
Each mind map is persisted as `.hymnal-data/brainstorm/{slug}.json` with `schemaVersion`. Node positions are stored as `x, y` floats relative to a fixed origin. An index file `.hymnal-data/brainstorm/index.json` lists all maps with name, created, and modified timestamps.

#### FR-61: Multiple maps per workspace
The author can create, rename, and delete multiple named mind maps per workspace (e.g., "Character Web", "Plot Outline", "Research Threads"). Map selection is surfaced in the Brainstorm surface's sidebar or header.

#### FR-62: Node linking to chapters and supplemental docs
A node can be linked to a chapter UUID or a supplemental doc path. Clicking a linked node opens the linked item in the editor. Links are displayed with a distinct visual indicator on the node.

#### FR-63: Mind-map export to supplemental doc
The author can export any mind map as a Markdown outline to `.hymnal-data/docs/`. The export is a one-time write (not a live sync) and the exported file appears in the supplemental docs tree immediately.

#### FR-64: AI-assisted node expansion (optional/stretch)
With a configured AI provider, the author can select a node and request AI-suggested sub-ideas. Suggested nodes appear as a preview that the author can accept or dismiss individually. Uses `AiChatViewModel` context from M006.

### Nav Decision (deferred to M009 planning)

Navigation placement is deferred. Options to evaluate at M009 `/gsd discuss`: (a) BRAINSTORM title-bar tab; (b) nested surface under PLAN mode; (c) accessible from PLAN mode's sidebar panel.

---

## Section 3: Marketing Activity Tracker (M010/S01–S02)

### PRD §5 Non-Goal Update

The original PRD §5 does not list a marketing tracker as a non-goal. This is additive scope with no conflict.

### New FRs

#### FR-65: Marketing activity store
Marketing activities are persisted in `.hymnal-data/market/activities.json` with `schemaVersion`. Each activity has: `id` (UUID), `title`, `channel` (free-text tag), `dueDate` (optional, ISO 8601), `status` (pending / in-progress / done), `notes` (optional Markdown), `createdDate`.

#### FR-66: Marketing activity list and CRUD
The author can create, edit, mark complete, and delete marketing activities from a dedicated surface in Hymnal. Activities are displayed in a filterable list (by status and channel). Completed activities can be hidden via a toggle.

#### FR-67: Marketing dashboard
A summary panel showing: total pending activities, activities due in the next 7 days (by dueDate), activity counts by channel, and completion rate.

### Nav Decision (deferred to M010 planning)

Navigation placement is deferred. Options: MARKET title-bar tab; nested under MANAGE mode; settings-like sidebar surface.

---

## Section 4: LeanPub API Integration (M010/S03–S04)

### PRD §5 Non-Goal Removal

**Original non-goal:** "LeanPub publishing or API integration — Hymnal does not trigger LeanPub builds or manage previews."

**Amended:** This non-goal is removed for M010. LeanPub read-only metadata sync (S03) and preview build triggering (S04, stretch) are in scope for M010.

**Rationale:** PRD SM-4 states the author will submit *A Choir of Minds* to LeanPub using Hymnal as the primary environment. An in-app LeanPub connection closes that loop. The original non-goal was a deferral, not a permanent exclusion.

**Constraint preserved:** Hymnal does not manage LeanPub's full publishing workflow (pricing, royalty splits, store metadata updates, etc.). The scope is limited to: read book status, view preview URL, and optionally trigger one preview build.

### New FRs

#### FR-68: LeanPub provider configuration
The author can configure a LeanPub book slug and API token in Settings. The API token is stored via `ICredentialStore` (never on disk). The LeanPub API base URL is a constant (`https://leanpub.com/`).

#### FR-69: LeanPub read-only book metadata panel
With a valid LeanPub slug and token, the Market surface shows a read-only metadata panel: book title, cover thumbnail URL, latest published version, preview URL (deep-link to the LeanPub preview), and latest job status (in-progress / completed / errored). Data is fetched on demand (not polled automatically).

#### FR-70: LeanPub preview build trigger (stretch)
The author can trigger a LeanPub preview build from the Market surface. Build job status is polled at a 10-second interval and shown in the UI. Failures display the raw API error. Only one in-flight build job is tracked at a time.

### Implementation Notes

- LeanPub API authentication: bearer token via `Authorization: Bearer {token}` header.
- Key LeanPub API endpoints to research before M010/S03 begins: `GET /api/v1/{slug}/book_version`, `POST /api/v1/{slug}/preview` (if available). Verify against current LeanPub API docs at https://leanpub.com/help/api.
- `ILeanPubClient` interface in `Hymnal.Core`; `HttpClient`-backed implementation in `Hymnal`; fake `ILeanPubClient` for unit testing.
- LeanPub API shape has changed historically — a research spike is recommended before committing S03/S04 scope.

---

## Unchanged V1 Non-Goals

These non-goals from PRD §5 remain in force and are not affected by this addendum:

- Multi-book project library (one workspace = one open book)
- Full Git branch management (no branch creation, switching, merging, conflict resolution)
- Collaboration or multi-author support
- macOS support
- Mobile or web versions
- Built-in spell-check engine
- Rendered Markdown preview panel
- Formatting toolbar in the editor
