---
title: "PRD: Hymnal"
status: final
created: 2026-05-27
updated: 2026-05-27
---

# PRD: Hymnal

## 0. Document Purpose

This PRD defines the functional and non-functional requirements for Hymnal V1 — a cross-platform .NET desktop writing application for solo authors using LeanPub's Markua format. It is written for the author/PM/builder (Matthew-Hope) and is structured for downstream use by AI dev agents and future contributors.

It builds on the Hymnal Product Brief (2026-05-27) and its addendum; it does not duplicate them. The brief addendum carries rationale for rejected alternatives and detail reserved for architecture/UX docs. The PRD addendum in this workspace carries technical depth that belongs downstream: `.hymnal-data/` folder schema, Markua syntax token detail, AI provider integration options.

Vocabulary is anchored to the Glossary (§3). FRs are globally numbered (FR-1 through FR-N) for stable downstream reference. Inline `[ASSUMPTION]` tags mark inferences not confirmed by the author; these are indexed in §10.

---

## 1. Vision

Hymnal is a focused .NET desktop writing tool for solo authors who live in a Markua/Git workflow. It opens any Markua manuscript folder and immediately understands the project's structure — parts, chapters, front matter — ordered exactly as `Book.txt` defines — and provides a unified environment for prose writing, chapter lifecycle tracking, progress visualization, and AI-assisted editorial feedback.

The core conviction is that plaintext and Git are the right foundation for a self-publishing author, and no tool should ask them to abandon that foundation. Hymnal meets authors where they already are: it reads `Book.txt`, writes only standard `.md` files back, stores all its own metadata in a single `.hymnal-data/` folder at the project root, and otherwise stays out of the way. Writing in Hymnal should feel as clean and uninterrupted as writing in a good text editor — with Markua understanding, project management, and AI editorial review as capabilities that surface when needed, not demands on the author's attention.

Hymnal is MIT-licensed, open source, and built by its primary user — the author writing *A Choir of Minds* — so its roadmap follows real, lived use rather than a theoretical market.

---

## 2. Target User

### 2.1 Jobs To Be Done

- Write prose in a focused environment that understands Markua syntax without interrupting the writing flow.
- Know at a glance where every chapter is in its lifecycle and how much has been written.
- See the whole manuscript's phase progress in a single timeline view without maintaining a spreadsheet.
- Visualize the manuscript structure as notecards and understand each chapter's status at a glance.
- Keep per-chapter notes and supplemental reference material (character bibles, research) tied to the project without polluting the manuscript.
- Commit and push writing progress to Git without switching away from the writing environment.
- Get an editorial read on a chapter or the whole book from an AI assistant without context-switching to another tool.

### 2.2 Non-Users (V1)

- Co-authors and writing teams
- Non-Markua authors (Word, Google Docs, Scrivener users)
- Authors publishing through channels other than LeanPub
- Readers of the published book

### 2.3 Key User Journeys

- **UJ-1. Matthew-Hope opens a morning writing session.**
  He opens Hymnal; the previous Workspace reloads automatically. He clicks a chapter in the sidebar — the editor opens. He writes prose for an hour; live word count updates in the sidebar as he types. When finished, he saves, marks the chapter status as *Editing*; the Gantt pre-fills the Editing phase start date. He reviews the chapter notes panel briefly, then commits with one click and a default message.

- **UJ-2. Matthew-Hope reviews overall progress before planning the week.**
  He switches to the Gantt view and collapses completed parts, expanding only the current part to see which chapters are Drafting vs. Editing and where their phase dates land. He notices one chapter has no Editing end date and enters it inline. He switches to the Corkboard, reads the status cards and word count percentages to decide which chapters need attention this week.

---

## 3. Glossary

- **Workspace** — A folder opened in Hymnal as the current project. One Workspace is open at a time.
- **Manuscript** — The collection of chapter and structural files governed by `Book.txt`, comprising the content of the book.
- **Book.txt** — The ordered manifest file in a Markua project (at `{workspace}/Book.txt` or `{workspace}/manuscript/Book.txt`) that defines which files are included in the manuscript build and in what order.
- **Chapter** — A single `.md` or `.txt` file referenced as a prose content unit in `Book.txt`. Front matter, back matter, and directive-only files (e.g., `mainmatter.txt`) are structural entries, not Chapters.
- **Part** — A logical grouping of Chapters, stored as a subfolder within the `manuscript/` directory. Each Part folder contains a `part.md` file with a `{class: part}` attribute and a `#` heading that gives the Part its title. Part folder names are author-defined. `Book.txt` references all files using folder-prefixed paths. A Part contains zero or more Chapters.
- **Status** — The author's declared lifecycle stage for a Chapter. Ordered: `Outlining → Drafting → Editing → Polishing → Reviewing → Done`.
- **Phase** — One named slot in the Chapter lifecycle, corresponding to a Status value. Each Phase on a Chapter may have a start date, an end date, and an optional progress percentage.
- **Phase dates** — The manually-entered start and end date pair for a given Phase on a Chapter.
- **Phase progress** — An optional manually-entered completion percentage (0–100%) for a Phase on a Chapter.
- **Word count** — The live-computed count of prose words in a Chapter file, updated as the author types.
- **Target** — An optional word count goal set at Chapter, Part, or book level. May be a single value or a range (min/max).
- **Corkboard** — The card-based visual outline view, one card per Chapter, organized in manuscript order.
- **Gantt view** — The phase-timeline visualization displaying one row per Chapter (with Part header rows), plotting Phases against a time axis.
- **.hymnal-data/** — The folder at the Workspace root where Hymnal persists all non-manuscript metadata. Git-trackable by default.
- **Chapter notes** — Per-Chapter author notes stored in `.hymnal-data/notes/`. Not included in the manuscript build.
- **Supplemental docs** — Author reference materials (character bibles, location notes, research) stored in `.hymnal-data/docs/`. Not included in the manuscript build.
- **Summary** — An AI-generated synopsis of a Chapter, Part, or book stored in `.hymnal-data/summaries/`. Used as context in AI chat sessions.
- **Issue** — A structured AI-generated editorial finding stored in `.hymnal-data/issues/`, carrying type, description, state, created date, and optional location.

---

## 4. Features

### 4.1 Workspace & Manuscript Model

**Description:** Hymnal opens a folder as a Workspace, parses `Book.txt` to build the Chapter/Part tree, and maintains that model as the authoritative structure for all views. The application stores its own metadata in `.hymnal-data/` at the Workspace root. It writes back to `Book.txt` only on explicit user actions (Chapter reorder, insertion, or inclusion toggle).

**Functional Requirements:**

#### FR-1: Open folder as Workspace
The author opens a Workspace via a folder picker or by dragging a folder onto the application window. The most recently opened Workspace is restored on application launch without requiring re-navigation.

**Consequences:**
- Hymnal launches to the last-used Workspace.
- Opening a new folder replaces the current Workspace; unsaved editor changes prompt a save confirmation first.

#### FR-2: Parse Book.txt into Chapter/Part tree
On Workspace open, Hymnal locates `Book.txt` (checked first at `{workspace}/Book.txt`, then `{workspace}/manuscript/Book.txt`) and parses it to build the Chapter/Part tree. File order in `Book.txt` is the canonical Chapter sequence.

**Consequences:**
- All Chapters and Parts appear in the sidebar in `Book.txt` order.
- Directive-only and front/back matter structural files are shown in the tree but excluded from Status and Word count tracking.

**Out of scope:** Hymnal does not parse `Subset.txt` or `Sample.txt` for tree building.

#### FR-3: Recognize Markua Parts
Hymnal identifies Parts from the manuscript folder structure: each subfolder within `manuscript/` that contains a `part.md` file is treated as a Part. The Part's title is read from the `#` heading in `part.md`; the `{class: part}` attribute above the heading is the structural signal. Part folder names are author-defined (e.g., `part1/`, `part2/`, `epilogue/`). `Book.txt` entries reference files using folder-prefixed paths (e.g., `part1/part.md`, `part1/01-chapter.md`). Chapter files listed in `Book.txt` under a Part's folder are associated with that Part. Files at the `manuscript/` root (not inside a Part subfolder) are treated as top-level, unparted Chapters.

#### FR-4: Handle missing files gracefully
If `Book.txt` references a file that does not exist on disk, that entry is shown in the tree marked as missing (visually distinct) and does not prevent other Chapters from loading. The author can remove missing entries from the tree via context menu.

#### FR-5: Persist Hymnal metadata in .hymnal-data/
All Hymnal-generated data — Chapter notes, Phase dates, Phase progress, word count Targets, AI Summaries, and Issues — is stored in `.hymnal-data/` at the Workspace root. Hymnal creates this folder on first write if it does not exist.

**Out of scope:** Hymnal never writes to `manuscript/resources/` or other manuscript subfolders unprompted.

#### FR-6: Detect external Book.txt changes
When `Book.txt` is modified externally while the Workspace is open, Hymnal detects the change via file-system watch and prompts the author to reload the Workspace structure.

---

### 4.2 Markua Editor

**Description:** A focused, source-text-only writing environment with Markua-aware syntax highlighting and inline validation. No toolbar, no rendered preview. The editor is the primary surface and is designed to keep the author in a writing flow. Realizes UJ-1.

**Functional Requirements:**

#### FR-7: Single-chapter editor
Clicking a Chapter in the sidebar or Corkboard opens it in the editor. One Chapter is the active editing target at a time. The editor saves to the file on save action (Ctrl+S). On Workspace close, unsaved changes prompt a save confirmation.

#### FR-8: Markua syntax highlighting
The editor applies syntax highlighting for Markua 0.30 constructs:
- **Standard Markdown:** headings (`#`–`#####`), bold, italic, inline code, fenced code blocks, links, blockquotes.
- **Markua constructs:** part markers (`# Part N #` heading syntax; `{class: part}` attribute on a heading, the LeanPub/real-world form — covered by attribute list highlighting), blurb line prefixes (`A>`, `B>`, `C>`, `D>`, `E>`, `I>`, `Q>`, `T>`, `W>`, `X>`), attribute lists (`{key: value, ...}`), directives (`{sample: true}`, `{mainmatter}`, `{backmatter}`, `{id:}`, `{format:}`, `{line-numbers:}`, etc.), and magic-comment markers (`markua-start-insert` / `markua-end-insert`).

See addendum for token-level color assignments.

#### FR-9: Inline Markua validation
The editor performs non-blocking inline validation and marks known error patterns as margin indicators. V1 validated patterns at minimum:
- Blank line between a `{sample: true}` directive and its heading.
- Unrecognized attribute key in an attribute list.
- `Sample.txt` referenced in `Book.txt` (warns that this is LFM-only, not supported in Markua 0.30).

Validation is advisory only; it does not block editing or saving.

#### FR-10: No toolbar, no preview panel
The editor has no formatting toolbar and no rendered Markdown preview panel. All formatting is entered as Markua source text.

#### FR-11: Dark synthwave theme
The application uses a dark synthwave visual theme across all surfaces. Primary accent: purple. Secondary accents: yellow, pink, orange. Syntax highlighting token colors use palette-appropriate assignments. A light theme is not required for V1.

#### FR-12: Focus-first editor layout
UI chrome is minimal. Line height, reading width, and font sizing default to prose-optimized values: minimum 16px body font size, 1.6 line height, and 70ch maximum content width. These defaults are adjustable in settings. Side panels are collapsible; the editor expands to fill available width when all panels are collapsed.

#### FR-13: Chapter notes panel
A notes panel is accessible from within the editor (e.g., a sidebar toggle or tab) showing the active Chapter's notes. The author can read and write notes without leaving the editing context. Notes are stored as Markdown in `.hymnal-data/notes/{chapter-filename}.md`.

---

### 4.3 Project Management

**Description:** Chapter Status lifecycle, live Word count tracking, optional Targets, and the Chapter notes system. Provides the project-awareness layer that eliminates the spreadsheet. Realizes UJ-1, UJ-2.

**Functional Requirements:**

#### FR-14: Chapter Status lifecycle
Each Chapter has a Status drawn from the ordered lifecycle: `Outlining → Drafting → Editing → Polishing → Reviewing → Done`. Status is set manually by the author. The current Status is visible on the Chapter's sidebar entry, Corkboard card, and Gantt row.

#### FR-15: Status-triggered Phase date pre-fill
When the author sets a Chapter's Status to a given lifecycle stage, Hymnal optionally pre-populates the corresponding Phase's start date with today's date — if no start date is already set for that Phase. This pre-fill is always editable and can be disabled in settings.

#### FR-16: Live word count per Chapter
Word count for each Chapter is computed live from the file content as the author types or whenever the file changes externally. Word count is displayed in the sidebar alongside the Chapter name and in the editor status bar when the Chapter is active. Word count counts prose words, excluding Markua directives and attribute list content. Word count uses whitespace tokenization on non-directive, non-attribute-list text — not a full Markdown AST parse.

#### FR-17: Word count rollup per Part and book
Per-Chapter word counts roll up to Part totals (sum of member Chapters) and a book total (sum of all Chapters). These are displayed in the sidebar at the Part and book level.

#### FR-18: Optional word count Targets
The author may set an optional Target at Chapter, Part, or book level. Book-level Target may be a range (min/max). Chapter- and Part-level Targets may be a single value or a range.

#### FR-19: Target proximity indicator
Where a Target is set, a visual proximity indicator (e.g., progress bar or fill) is shown: in the sidebar alongside the word count at the Chapter, Part, and book level; and on each Chapter row in the Gantt view. The indicator reflects proximity to the Target (or to the minimum value of a range Target).

---

### 4.4 Gantt Phase View

**Description:** A per-Chapter phase timeline giving the author a whole-manuscript lifecycle picture at a glance. Parts collapse and expand. Phase dates and optional progress percentages are directly editable on the chart. Realizes UJ-2.

**Functional Requirements:**

#### FR-20: Chapter rows with phase boxes
The Gantt view displays one row per Chapter. Each row contains a sequence of phase boxes — one per lifecycle Phase (Outlining, Drafting, Editing, Polishing, Reviewing, Done) — ordered left to right.

#### FR-21: Part rollup rows
Parts appear as collapsible header rows above their member Chapters. Expanding a Part shows its Chapter rows; collapsing hides them. All Parts are expanded by default. Part header rows display the Part name and an aggregate summary (e.g., Chapter count per Status).

#### FR-22: Phase date editing
Each phase box displays its Phase dates (start and end) when set. The author edits Phase dates directly on the Gantt row via an inline date picker or typed entry.

#### FR-23: Status-triggered Phase date pre-fill on Gantt
Setting a Chapter's Status from the Gantt row applies FR-15 behavior: today's date optionally pre-fills the Phase's start date if none is set.

#### FR-24: Optional Phase progress indicator
Each phase box optionally displays a manually-entered Phase progress percentage (0–100%). This value is blank by default. The visual fill of the phase box reflects the entered percentage when present.

#### FR-25: Phase box visual states
Phase boxes are visually distinct by state:
- **Completed phase** (Chapter Status has advanced past this Phase): filled, muted color.
- **Active phase** (current Chapter Status): highlighted in the synthwave primary accent (purple).
- **Future phase** (Chapter Status has not yet reached this Phase): outline only.

#### FR-26: Time axis and horizontal scroll
The Gantt always displays a horizontal time axis. The default span is today through approximately one month out. Once Phase dates are entered, the axis adjusts to span from the earliest Phase date in the Workspace to the latest, with today remaining visible. The view scrolls horizontally.

#### FR-27: Chapter order is read-only in Early V1
Row order in the Gantt mirrors `Book.txt` order and is not draggable in Early V1. Drag-to-reorder is a Late V1 capability (see §6.2; the Late V1 line item there supersedes this restriction).

---

### 4.5 Corkboard View

**Description:** A card-based visual outline of the Manuscript. Early V1 delivers status visualization. Late V1 adds structural editing (reorder, include/exclude, insert). Realizes UJ-2.

**Functional Requirements:**

**Early V1 — Status visualization:**

#### FR-28: Chapter cards
The Corkboard displays each Chapter as a card showing: Chapter title, current Status, Word count, word count percentage toward Target (if a Target is set), and current-phase start/end dates (if entered). Parts appear as labeled section dividers between their Chapters' cards.

#### FR-29: Status-coded card styling
Each card's visual treatment reflects its Status using the synthwave palette (color-coded border or badge per Status stage). Status colors are consistent with Status indicators in the sidebar and Gantt view.

#### FR-30: Card click opens editor
Clicking a Chapter card opens that Chapter in the editor.

**Late V1 — Structural editing:**

#### FR-31: Drag-to-reorder cards
Chapter cards are draggable within the Corkboard. Dropping a card in a new position reorders the Chapters and writes the updated order back to `Book.txt`. Moving a Chapter into a different Part's section updates the Chapter's folder location on disk (moving the file into the target Part's subfolder), updates the Part grouping in the sidebar and Gantt, and updates `Book.txt` — the Chapter's `.md` content is not modified.

#### FR-32: Chapter inclusion toggle
A Chapter can be toggled as included or excluded from the manuscript build from its Corkboard card. Inclusion state is the authoritative record: included Chapters appear in `Book.txt`; excluded Chapters are removed from `Book.txt` but tracked in `.hymnal-data/` so they can be re-included without re-entering the filename. [ASSUMPTION: The exact exclusion storage mechanism — commented line, separate manifest, JSON index — is an implementation decision for Late V1.]

#### FR-33: Chapter insertion from Corkboard
New Chapters can be inserted between existing cards via a context menu or an insertion affordance between cards. Inserting a Chapter creates a new `.md` file (at the manuscript folder location) and adds it to `Book.txt` at the chosen position. The author provides the filename and an optional display title.

---

### 4.6 Supplemental Docs

**Description:** A project-scoped folder of author reference material (character bibles, location notes, technology references, research documents) that lives inside the project but is never part of the manuscript build.

**Functional Requirements:**

#### FR-34: Supplemental docs surface in sidebar
Hymnal shows a Supplemental Docs section in the sidebar displaying the contents of `.hymnal-data/docs/` as a file/folder tree.

#### FR-35: No enforced structure
Supplemental docs are plain files (`.md`, `.txt`, or others) organized in whatever folder structure the author chooses within `.hymnal-data/docs/`. Hymnal applies no required naming convention or schema.

#### FR-36: Supplemental docs are editable in the editor
Supplemental doc files can be opened in the editor for reading and writing. They are never included in the manuscript build.

#### FR-37: Create supplemental docs and folders from sidebar
The author can create new files and subfolders within `.hymnal-data/docs/` via a sidebar context menu. New files open in the editor immediately on creation.

---

### 4.7 Lightweight Git Integration

**Description:** A single stage-all → commit → push workflow for end-of-session saves. No branch management. Realizes UJ-1.

**Functional Requirements:**

#### FR-38: Detect Git repository at Workspace open
On Workspace open, Hymnal checks whether the Workspace folder (or any parent up to the filesystem root) is a Git repository. If yes, Git features are enabled. If no Git repository is detected, all Git UI is hidden.

#### FR-39: Stage all → commit → push workflow
A Git panel provides a linear workflow: stage all changed files → commit with a message → push to the current remote. Per-file staging, branch selection, and merge operations are not exposed.

#### FR-40: Optional commit message
The commit message field is optional. If left blank, Hymnal uses a generated default: `"Hymnal: save progress {ISO-8601 timestamp}"`.

#### FR-41: Branch and change count display
The Git panel displays the current branch name and the count of uncommitted changes (modified + untracked files tracked by Git).

#### FR-42: System Git binary
Hymnal invokes the system-installed Git binary via PATH. It does not bundle its own Git implementation. If no Git binary is found, a clear message is displayed and Git features are disabled.

#### FR-43: Git error surfacing
Errors from Git operations (authentication failure, no remote configured, push rejection, network error) are shown as in-app notifications that include the raw Git error output. Hymnal does not attempt to resolve conflicts, re-authenticate, or retry automatically.

---

### 4.8 AI Editorial Assistance *(Late V1)*

**Description:** Configurable AI provider, manuscript-level Summaries, an in-app editorial chat panel, and a persistent tracked Issues list. Provides AI editorial feedback without leaving Hymnal.

**Functional Requirements:**

#### FR-44: AI provider configuration
The author configures an AI provider in application settings by providing an endpoint URL and API key. Supported integration surfaces: LiteLLM-compatible endpoints; Microsoft Extensions for AI / Agent Framework. The API key is stored in the OS credential store (Windows Credential Manager on Windows; Secret Service / libsecret on Linux) — never in `.hymnal-data/` or application settings files on disk.

#### FR-45: Generate and store Summaries
Hymnal generates Summaries at Chapter, Part, or book level using the configured AI provider. Summaries are stored as Markdown files in `.hymnal-data/summaries/`. Summaries are regenerable on demand; regenerating overwrites the prior Summary for that scope.

#### FR-46: AI chat panel with Summary context
The author opens an AI chat panel scoped to the current Chapter, Part, or whole book. The relevant Summaries are included as context in the prompt sent to the AI provider. The panel displays the exchange and accumulates within the session.

#### FR-47: Editorial query types
The AI chat panel supports editorial queries including: readability, grammar, internal inconsistency, developmental feedback, and line-level notes. Free-text queries are the primary interaction mode. Structured editorial modes — at minimum **Proofing**, **Consistency**, and **Line Editing** — are targeted for Late V1; each mode sends a defined prompt structure and parses the response into the Issues list automatically. Additional structured modes are post-V1.

#### FR-48: Structured Issues from AI findings
AI findings can be saved as structured Issues in `.hymnal-data/issues/`. Each Issue carries: `id`, `type` (readability / grammar / inconsistency / developmental / line-editing), `description`, `state` (open / resolved / dismissed), `created_date`, and optional `location` (book / part / chapter / paragraph / line).

#### FR-49: Issues panel
An Issues panel lists all Issues, filterable by scope (book / part / chapter), type, and state. Issues can be marked resolved or dismissed from the panel.

#### FR-50: Inline issue indicators
Line-level Issues are shown as margin indicators on the relevant editor lines. Chapter- and Part-level Issues are surfaced as: (a) a summary indicator at the top of the Chapter or Part section in the editor, and (b) summary badges in the sidebar at the appropriate scope. Clicking any indicator or badge opens the Issues panel filtered to that scope.

---

## 5. Non-Goals (Explicit)

- **Not a platform, not a market** — Hymnal is a tool for one author's specific workflow. It will not evolve into a publishing platform, a writing community, or a tool marketed to non-Markua authors. New features are evaluated against this principle.
- **Rendered Markdown preview panel** — explicitly out of scope. The editor is source-text only with syntax highlighting.
- **Multi-book project library** — one Workspace = one open book. Multi-book support is post-V1.
- **Full Git branch management** — no branch creation, switching, merging, or conflict resolution in V1.
- **LeanPub publishing or API integration** — Hymnal does not trigger LeanPub builds or manage previews.
- **Collaboration or multi-author support** — single-author only.
- **macOS support** — not a V1 target platform.
- **Mobile or web versions.**
- **Built-in spell-check engine** — relies on any spell-check provided natively by the editor component or OS.

---

## 6. MVP Scope

### 6.1 V1 Early Core

- Workspace open; `Book.txt` parse and Chapter/Part tree (FR-1–FR-6)
- Markua editor: syntax highlighting, inline validation, no toolbar/preview (FR-7–FR-12)
- Chapter notes panel in editor (FR-13)
- Chapter Status lifecycle with Phase date pre-fill (FR-14–FR-15)
- Live word count: per Chapter, Part, and book (FR-16–FR-17)
- Optional Targets and proximity indicators (FR-18–FR-19)
- Gantt view: phase boxes, Part rollup rows, Phase date editing, optional Phase progress, phase box visual states, time axis (FR-20–FR-26; FR-27 read-only)
- Corkboard Early V1: Chapter cards with title/status/word count/dates, status coding, click-to-open (FR-28–FR-30)
- Supplemental docs folder in sidebar (FR-34–FR-37)
- Lightweight Git: stage-all + commit + push (FR-38–FR-43)
- Dark synthwave theme (FR-11, NFR-V1, NFR-V2)

### 6.2 V1 Late Core

- Corkboard Late V1: drag-to-reorder, inclusion toggle, Chapter insertion (FR-31–FR-33)
- Gantt drag-to-reorder rows (supersedes FR-27 read-only restriction)
- AI provider configuration (FR-44)
- AI Summaries: generate and store at Chapter/Part/book level (FR-45)
- AI chat panel with Summary context (FR-46–FR-47)
- Issues: structured storage, panel, inline indicators (FR-48–FR-50)

### 6.3 Out of Scope for V1

Per §5 Non-Goals.

---

## 7. Success Metrics

V1 is successful when the primary user (Matthew-Hope, author of *A Choir of Minds*) can do all of the following without supplementing Hymnal with an external tool.

### Primary Metrics

- **SM-1: No spreadsheet supplement** — The author tracks chapter lifecycle, phase dates, progress, and word count targets entirely within Hymnal. No external spreadsheet or tracker is maintained.
- **SM-2: Inline AI editorial read** — The author completes an AI editorial round-trip (generate summary → pose an editorial question → receive findings) on any chapter or part without leaving the app.
- **SM-3: One-action commit** — The author stages, commits, and pushes a writing session's progress to Git from within Hymnal in a single workflow sequence.
- **SM-4: Primary writing tool** — *A Choir of Minds* V1 manuscript is written and submitted to LeanPub using Hymnal as the primary writing environment.

### Counter-Metrics

Signs the tool is drifting from its purpose:
- Author spending more session time in Hymnal settings or configuration than writing.
- Author maintaining a parallel spreadsheet alongside Hymnal.
- Author exporting manuscript content to another tool to complete an AI editorial review.

---

## 8. Non-Functional Requirements

### Platform

- **NFR-P1:** Hymnal targets Windows 10+ and Linux (Ubuntu LTS, Fedora current) as first-class supported platforms. macOS is not a V1 target.
- **NFR-P2:** The application ships as a self-contained .NET publish — no separate .NET runtime installation required by end users.

### Performance

- **NFR-Perf1:** Per-Chapter Word count updates complete within 500 ms of the last keystroke for manuscripts up to 200,000 words total.
- **NFR-Perf2:** Application cold start and initial Workspace load complete within 5 seconds on a mid-range development machine.
- **NFR-Perf3:** `Book.txt` parse and initial Chapter/Part tree build complete within 2 seconds for manuscripts with up to 100 Chapters.

### Data Safety

- **NFR-D1:** Hymnal never modifies Chapter `.md` files except through explicit author edits in the editor. All writes to `Book.txt` (from Corkboard reorder, Chapter insertion, or inclusion toggle) are atomic: write to a temp file in the same directory, then rename into place.
- **NFR-D2:** `.hymnal-data/` is the only directory Hymnal writes to outside of explicit editor saves. Hymnal never writes to `manuscript/resources/` or other manuscript subfolders without a user-initiated action.

### Visual Design

- **NFR-V1:** All primary and body text on all surfaces must meet WCAG AA contrast ratio (minimum 4.5:1 text-to-background).
- **NFR-V2:** UI chrome is minimal. Panels not in active use are collapsible. The editor surface expands to fill available width when side panels are hidden.

### Workflow Integrity

- **NFR-W1:** Every writing-session action — editing, status update, word count review, Git commit — is completable from within Hymnal without switching to another application. The application is the complete writing session; no supplemental tool should be required.

### Storage & Portability

- **NFR-S1:** All project-scoped Hymnal metadata lives in `.hymnal-data/` at the Workspace root. The Workspace is fully portable — moving or copying the folder preserves all Hymnal metadata.
- **NFR-S2:** AI provider API keys are stored in the OS credential store. They do not appear in `.hymnal-data/`, application config files, or logs in any form.

---

## 9. Open Questions

1. **OQ-1 — .hymnal-data/ in .gitignore:** Should Hymnal offer to add AI-generated subdirectories (`.hymnal-data/summaries/`, `.hymnal-data/issues/`) to `.gitignore` while keeping notes and phase-date files tracked? These files can grow large and may contain sensitive manuscript content. A user-configurable policy (rather than a blanket default) is likely the right answer — but the default needs deciding before Late V1 ships.

2. **OQ-2 — Word count definition precision:** Should FR-16's word count exclude only Markua structural directives and attribute lists, or also strip Markdown formatting tokens (e.g., `**bold**` counted as "bold", not "**bold**")? The definition affects the meaning of the NFR-Perf1 manuscript size bound.

3. **OQ-3 — Chapter exclusion storage format:** FR-32 defers the exact storage format for excluded Chapters to implementation. This decision (commented `Book.txt`, a separate `.hymnal-data/exclusions.json`, or other) should be made before Late V1 to avoid a migration.

4. **OQ-4 — ~~Part boundary detection edge case~~** — *Resolved.* Parts are identified by folder structure (`manuscript/{part-folder}/part.md`), not by heading scan. See FR-3.

5. **OQ-5 — ~~Git push auth failure behavior~~** — *Resolved.* See DL-19. Surface the raw Git error output and leave the local commit in place. No credential prompting or automatic retry in V1.

6. **OQ-6 — Issues panel navigation shape:** FR-49 specifies flat filtering (by scope / type / state). The brief addendum implies scope-organized hierarchical navigation (book → part → chapter). The difference affects UX panel design. Should the Issues panel use flat filters with scope as one filter axis, or a tree navigator with filters within a scope node? Defer to UX/architecture phase.

---

## 10. Assumptions Index

- **FR-16** — *(confirmed)* Word count uses whitespace tokenization on non-directive, non-attribute-list text; not a full Markdown AST parse.
- **FR-26** — *(confirmed)* Default Gantt time axis spans today to ~one month out; expands to cover entered Phase dates once any exist.
- **FR-31** — *(confirmed)* Moving a Chapter between Parts moves the file on disk into the target Part's subfolder, updates `Book.txt`, and updates sidebar/Gantt grouping. Chapter `.md` content is not modified.
- **FR-32** — Excluded Chapters are removed from `Book.txt` and tracked in `.hymnal-data/` for re-inclusion. The exact storage format is an implementation decision.
- **FR-47** — *(partially confirmed)* Free-text is the primary query mode. Proofing, Consistency, and Line Editing structured modes are targeted for Late V1.
