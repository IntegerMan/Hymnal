---
name: Hymnal
description: "Dark synthwave writing environment for solo Markua authors — prose writing, manuscript tracking, and AI editorial review in a focused desktop shell."
status: draft
sources:
  - _bmad-output/planning-artifacts/briefs/brief-Hymnal-2026-05-27/brief.md
  - _bmad-output/planning-artifacts/prds/prd-Hymnal-2026-05-27/prd.md
  - _bmad-output/planning-artifacts/architecture.md
updated: 2026-05-27
colors:
  # --- Surfaces (tonal elevation, no shadows) ---
  surface-base: '#0D0B14'
  surface-elevated: '#141020'
  surface-overlay: '#1C1729'
  surface-high: '#241F35'
  # --- Borders ---
  border-subtle: '#2D2540'
  border-default: '#3D3560'
  # --- Text ---
  on-surface: '#EDE8F5'
  on-surface-dim: '#9589B0'
  on-surface-muted: '#574E70'
  # --- Accents ---
  primary: '#9D4EDD'
  primary-bright: '#B469F0'
  pink: '#E91E8C'
  yellow: '#F5C842'
  orange: '#FF6B35'
  cyan: '#38BDF8'
  # --- Chapter lifecycle status ---
  status-outlining: '#6B7480'
  status-drafting: '#38BDF8'
  status-editing: '#9D4EDD'
  status-polishing: '#F5C842'
  status-reviewing: '#E91E8C'
  status-done: '#22D3A0'
  # --- Semantic ---
  error: '#FF4D4F'
  success: '#22D3A0'
  info: '#38BDF8'
  warning: '#F5C842'
typography:
  ui-base:
    fontFamily: Inter
    fontSize: 13px
    fontWeight: '400'
    lineHeight: '1.5'
  ui-sm:
    fontFamily: Inter
    fontSize: 11px
    fontWeight: '400'
    lineHeight: '1.4'
  ui-label:
    fontFamily: Inter
    fontSize: 11px
    fontWeight: '500'
    lineHeight: '1.4'
    letterSpacing: 0.06em
  ui-label-caps:
    fontFamily: Inter
    fontSize: 10px
    fontWeight: '600'
    lineHeight: '1.4'
    letterSpacing: 0.12em
  ui-heading:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '600'
    lineHeight: '1.4'
  editor-prose:
    fontFamily: JetBrains Mono
    fontSize: 16px
    fontWeight: '400'
    lineHeight: '1.7'
    letterSpacing: -0.01em
  editor-sm:
    fontFamily: JetBrains Mono
    fontSize: 14px
    fontWeight: '400'
    lineHeight: '1.6'
rounded:
  sm: 2px
  DEFAULT: 4px
  md: 6px
  lg: 8px
  xl: 12px
  full: 9999px
spacing:
  unit: 8px
  sidebar-width: 240px
  right-panel-width: 280px
  editor-max-width: 70ch
  gutter: 16px
  panel-padding: 16px
  title-bar-height: 40px
  status-bar-height: 24px
components:
  title-bar:
    background: '{colors.surface-elevated}'
    border-bottom: '1px solid {colors.border-subtle}'
    height: '{spacing.title-bar-height}'
    font: '{typography.ui-label-caps}'
    tab-active-color: '{colors.on-surface}'
    tab-active-indicator: '2px solid {colors.primary}'
    tab-inactive-color: '{colors.on-surface-muted}'
    tab-inactive-hover-color: '{colors.on-surface-dim}'
    icon-button-color: '{colors.on-surface-dim}'
    icon-button-hover-color: '{colors.on-surface}'
    icon-button-active-color: '{colors.primary}'
  sidebar:
    background: '{colors.surface-elevated}'
    border-right: '1px solid {colors.border-subtle}'
    width: '{spacing.sidebar-width}'
    font: '{typography.ui-base}'
    item-selected-background: '{colors.surface-overlay}'
    item-selected-indicator: '2px solid {colors.primary}'
    item-hover-background: '{colors.surface-overlay}'
    section-label-font: '{typography.ui-label-caps}'
    section-label-color: '{colors.on-surface-muted}'
    word-count-color: '{colors.on-surface-dim}'
  status-dot:
    size: 8px
    border-radius: '{rounded.full}'
  status-pill:
    font: '{typography.ui-label-caps}'
    border-radius: '{rounded.full}'
    padding: '2px 8px'
    note: 'Background: status color at 15% opacity. Border: 1px solid status color. Text: status color.'
  editor:
    background: '{colors.surface-base}'
    font: '{typography.editor-prose}'
    max-width: '{spacing.editor-max-width}'
    padding: '32px 24px'
    cursor-color: '{colors.primary}'
    selection-background: 'rgba(157, 78, 221, 0.25)'
    line-number-color: '{colors.on-surface-muted}'
    margin-indicator-size: 6px
    margin-indicator-color-validation: '{colors.yellow}'
    margin-indicator-color-issue: '{colors.yellow}'
    margin-indicator-color-error: '{colors.error}'
  gantt-row:
    height: 32px
    part-header-height: 36px
    part-header-background: '{colors.surface-overlay}'
    part-header-font: '{typography.ui-heading}'
    chapter-label-font: '{typography.ui-base}'
    chapter-label-color: '{colors.on-surface}'
    time-axis-font: '{typography.ui-sm}'
    time-axis-color: '{colors.on-surface-muted}'
  gantt-phase-box:
    height: 20px
    border-radius: '{rounded.sm}'
    font: '{typography.ui-sm}'
    active-fill: '{colors.primary}'
    active-text: '{colors.on-surface}'
    completed-fill: '{colors.surface-high}'
    completed-text: '{colors.on-surface-muted}'
    future-fill: 'transparent'
    future-border: '1px solid {colors.border-default}'
    future-text: '{colors.on-surface-muted}'
  corkboard-card:
    background: '{colors.surface-elevated}'
    border-radius: '{rounded.lg}'
    border: '1px solid {colors.border-subtle}'
    hover-border: '1px solid {colors.border-default}'
    status-stripe-height: 4px
    title-font: '{typography.ui-heading}'
    meta-font: '{typography.ui-sm}'
    meta-color: '{colors.on-surface-dim}'
    padding: '12px'
    min-width: 180px
    max-width: 260px
  status-bar:
    background: '{colors.surface-elevated}'
    border-top: '1px solid {colors.border-subtle}'
    height: '{spacing.status-bar-height}'
    font: '{typography.ui-sm}'
    color: '{colors.on-surface-dim}'
    separator-color: '{colors.border-subtle}'
    git-changes-pending-color: '{colors.orange}'
  right-panel:
    background: '{colors.surface-elevated}'
    border-left: '1px solid {colors.border-subtle}'
    width: '{spacing.right-panel-width}'
    padding: '{spacing.panel-padding}'
    header-font: '{typography.ui-heading}'
    body-font: '{typography.ui-base}'
  notification-banner:
    border-radius: '{rounded.md}'
    padding: '10px 14px'
    font: '{typography.ui-base}'
    error-background: 'rgba(255, 77, 79, 0.12)'
    error-border: '1px solid {colors.error}'
    info-background: 'rgba(56, 189, 248, 0.08)'
    info-border: '1px solid {colors.info}'
    success-background: 'rgba(34, 211, 160, 0.08)'
    success-border: '1px solid {colors.success}'
---

## Brand & Style

Hymnal is a writer's instrument, not a writer's assistant. Its visual language comes from the world of synthwave — dark, atmospheric, purposefully nocturnal — because writing a novel is night work: focused, interior, slightly transgressive of conventional daylight productivity.

The aesthetic posture is **focused minimalism with deliberate accent color**. Chrome is nearly invisible. Surfaces are dark layers stacked by tone, not bordered boxes. Purple does the primary work: it marks what is active, what demands attention, what is the cursor in the manuscript. Pink, yellow, and orange earn their appearance — pink for scrutiny and review, yellow for caution and refinement, orange for system signals.

Hymnal respects the author's eye. Long writing sessions demand that the visual layer recede. The frame exists so the words do not have to compete with it.

**Anti-pattern:** neon-on-black with no depth. Avoid retro CRT glow effects, strobing accents, or decorative animations. The synthwave aesthetic is about atmosphere, not performance.

## Colors

**Surface stack.** Four dark surface tiers create depth without shadows. `surface-base` (#0D0B14) is the editor canvas — the lowest, most recessive surface. `surface-elevated` (#141020) carries the title bar, sidebar, right panel, and status bar — the persistent shell. `surface-overlay` (#1C1729) is used for hover states, selected sidebar rows, and inline popovers. `surface-high` (#241F35) is reserved for completed phase boxes in the Gantt (suggesting receded, finished work) and for tooltips and context menus.

**Primary (purple, #9D4EDD).** The active-state color throughout. Active phase box fill, text cursor, sidebar selection indicator, tab underline, active icon button. `primary-bright` (#B469F0) is the hover or pressed variant for interactive purple elements.

**Pink (#E91E8C).** The Reviewing lifecycle stage; AI editorial panel active state; entity chips and secondary badges. Used when something is under scrutiny.

**Yellow (#F5C842).** The Polishing lifecycle stage; word count target proximity indicators; inline validation margin indicators; the `warning` semantic token. Used when something is close but not finished.

**Orange (#FF6B35).** The system layer: Git uncommitted-change count in the status bar; structural Markua directive syntax tokens (e.g., `{mainmatter}`, `{backmatter}`); error-adjacent notification borders. Not used in the lifecycle status color sequence.

**Cyan (#38BDF8).** The Drafting lifecycle stage; informational banners; the active-generation state in the AI panel. The color of something being produced right now.

**Lifecycle status color assignments** (canonical — must be consistent everywhere a status is represented):

| Stage | Color | Hex |
|---|---|---|
| Outlining | Neutral gray | `#6B7480` |
| Drafting | Cyan | `{colors.cyan}` |
| Editing | Purple | `{colors.primary}` |
| Polishing | Yellow | `{colors.yellow}` |
| Reviewing | Pink | `{colors.pink}` |
| Done | Mint green | `{colors.status-done}` |

**Text.** `on-surface` (#EDE8F5) carries a faint purple warmth — the color of light through a subtly violet lens, not clinical white. `on-surface-dim` (#9589B0) is secondary text: word counts in the sidebar, status bar labels, timestamps. `on-surface-muted` (#574E70) is placeholder text, disabled labels, section header caps, line numbers, and any text that should visually recede.

**All primary and body text on all backgrounds must meet WCAG AA (4.5:1 minimum). Lifecycle status colors used on `surface-elevated` must be verified — the mint and cyan values may require a brightness adjustment to meet the ratio.**

## Typography

Two embedded fonts cover all surfaces. Both are open-source (SIL OFL), embedded in the self-contained app bundle, and render well on both Windows 10+ and Linux (Ubuntu LTS, Fedora) via Avalonia's Skia rasterizer without requiring system font packages.

**Inter** (sans-serif) carries all UI chrome: title bar tabs, sidebar tree, status bar, status pills, panel headers, right panel content, Corkboard card metadata, and Gantt labels. Inter is weight-flexible (400–700) and reads cleanly at the small sizes typical for desktop application chrome (10–14px). The all-caps `ui-label-caps` style (10px, 600w, 0.12em tracking) follows the convention established in CORA — it is used for tab navigation labels, sidebar section headers, and status pill text.

**JetBrains Mono** (monospace) is the editor font for all Markua source editing. The author sees raw markup at all times — no preview panel. A monospace font makes the structural signals (attribute lists, blurb prefixes, heading hierarchies, directive blocks) legible at a glance and aligns with the author's developer-adjacent workflow.

**Scale notes:**
- `ui-base` (13px/1.5) — standard sidebar and panel text.
- `ui-sm` (11px) — status bar, metadata, timestamps.
- `ui-label-caps` (10px, 600w, +0.12em, all-caps) — CORA-lineage convention. Navigation labels, section headers, status pills.
- `ui-heading` (14px, 600w) — panel headers, Gantt Part header rows, Corkboard card titles.
- `editor-prose` (16px JetBrains Mono, 1.7lh) — the writing surface. Minimum 16px per PRD FR-12. Line height 1.7 gives the author breathing room between lines of prose markup.
- `editor-sm` (14px JetBrains Mono) — Notes panel body text, AI Chat message content.

**User-adjustable in Settings (V1):** `editor-prose` font size (floor 14px, no ceiling specified) and editor max content width (default 70ch). These are the only typography settings exposed to users.

## Layout & Spacing

8px spatial unit throughout. All structural dimensions are multiples of 8; fine details may use 4px increments.

**Shell layout:**

```
┌──────────────────────────────────────────────────────────────────────┐
│  Title bar (40px): [⬦ Hymnal] [Workspace ▼]  WRITE · GANTT · CORKBOARD  ···  [AI] [↑] [⚙]  │
├───────────────┬──────────────────────────────────────┬───────────────┤
│  Sidebar      │                                      │  Right panel  │
│  (240px)      │  Main content area                   │  (280px)      │
│  collapsible  │  (fills remaining width)             │  collapsible  │
│               │                                      │               │
└───────────────┴──────────────────────────────────────┴───────────────┘
│  Status bar (24px): path · words · STAGE · branch · N changes        │
└──────────────────────────────────────────────────────────────────────┘
```

The sidebar collapses to zero width (slides out left) via keyboard shortcut or sidebar toggle. The right panel collapses to zero width (slides out right) the same way. The main content region expands to fill all available width when both panels are hidden. At a typical 1440px-wide window with both panels open, the main content receives approximately 920px; at 1080px it receives approximately 560px.

**Panel padding:** 16px uniform interior padding in the sidebar, right panel, and title bar ends.
**Gutter:** 16px horizontal padding on each side of the editor's content column, inside the 70ch max-width boundary.
**Minimum window size:** [ASSUMPTION] ~680px wide (sidebar 240 + editor minimum 400 + scrollbar 8 + right panel collapsed) × ~420px tall.

## Elevation & Depth

Hymnal uses tonal elevation, not shadow. No `drop-shadow` or `box-shadow` is used for surface hierarchy — the dark value differences do the work.

Surface stack from bottom to top of visual hierarchy:
1. `surface-base` (#0D0B14) — editor canvas, deepest and most recessive.
2. `surface-elevated` (#141020) — persistent shell: title bar, sidebar, status bar, right panel.
3. `surface-overlay` (#1C1729) — interactive foreground: hover states, selected rows, inline date-picker popovers, context menus.
4. `surface-high` (#241F35) — floating or completed: tooltips, completed Gantt phase boxes.

Borders (`border-subtle`, `border-default`) mark structural divisions between adjacent elements at similar tonal levels — e.g., the bottom edge of the title bar, the right edge of the sidebar, the top edge of the status bar. Borders between clearly differentiated tonal surfaces are optional and should be omitted when the tonal contrast is already sufficient.

## Shapes

Minimal rounding. Sharp corners on large structural surfaces; subtle rounding on interactive elements.

- `rounded.sm` (2px) — Gantt phase boxes; status-dot container (though the dot itself is `rounded.full`).
- `rounded.DEFAULT` (4px) — buttons, dropdown menus, context menu items, input fields.
- `rounded.md` (6px) — notification banners, tooltip containers.
- `rounded.lg` (8px) — Corkboard cards, modal dialogs.
- `rounded.full` (9999px) — status pills, status dots, issue type chips in the Issues panel.

## Components

### Title bar

A custom 40px title bar replaces native OS window chrome. Three regions:
- **Left:** Hymnal diamond icon mark (16px) + workspace name (the folder name of the open workspace) in `ui-base`. Clicking the workspace name opens a small popover listing recently opened workspaces (up to 5) and an "Open folder…" option.
- **Center:** Three primary view tabs — **WRITE · GANTT · CORKBOARD** — in `ui-label-caps`. Active tab: `on-surface` text + 2px `primary` underline. Inactive tab: `on-surface-muted` text, no underline. Hover: `on-surface-dim`. Tabs are spaced with equal gaps; the group is horizontally centered within the title bar.
- **Right:** Three icon buttons — AI (neural/chat icon), Git (upload/branch icon), Settings (gear icon) — 20px icons, `on-surface-dim` at rest, `on-surface` on hover, `primary` when their panel is open.

The title bar also serves as the drag-to-move region for the window (excluding the interactive elements).

### Sidebar

Left panel, 240px, `surface-elevated`, `border-subtle` right edge. Two sections separated by a 1px `border-subtle` divider:

**Manuscript section** (section label: `ui-label-caps` / `on-surface-muted` / "MANUSCRIPT"): A tree of Parts and Chapters. Each Part is a collapsible row with a chevron icon and the Part title in `ui-heading`. Expanding a Part reveals its Chapter rows. Each Chapter row: an 8px status dot (lifecycle color) + chapter filename/title truncated in `ui-base` + word count right-aligned in `on-surface-dim`. Selected Chapter: `surface-overlay` background + 2px `primary` left-edge indicator. Hover: `surface-overlay` background. Hovering a Chapter row reveals a `···` icon at right for the context menu.

At the top of the sidebar, above the Manuscript section, a Book-level word count summary line shows total words (and target proximity if a book-level target is set).

**Docs section** (section label: "DOCS"): A plain file/folder tree for `.hymnal-data/docs/`. Files show a document icon in `on-surface-dim`; folders show a chevron. No word count or status for supplemental docs. A `+` icon at the section header right creates new files/folders.

### Status dot and status pill

**Status dot:** 8px circle, `rounded.full`, filled with the lifecycle stage color from the canonical mapping in `## Colors`. Used in sidebar chapter rows and Corkboard cards. Not interactive — status is changed via the status pill in the status bar or via the context menu.

**Status pill:** Full-radius pill in `ui-label-caps`. Background: the stage color at 15% opacity. Border: 1px solid the stage color. Text: the stage color. Example: "DRAFTING" in `{colors.cyan}` on a translucent cyan background. Used in the status bar (where it is a clickable dropdown), Corkboard cards (display-only), and context menus.

### Editor

The editor occupies `surface-base` (#0D0B14) — the darkest surface in the application and the visual center of gravity. The prose content column is horizontally centered within the editor region and constrained to 70ch maximum width. Padding: 32px vertical, 24px horizontal inside the column boundary.

Line numbers appear in a left gutter in `on-surface-muted`, separated from content by a 1px `border-subtle` vertical rule. The gutter between line numbers and content carries inline margin indicators: a 6px circle in `{colors.yellow}` for validation warnings and AI issues.

Text cursor: `{colors.primary}` purple. Selection: `primary` at 25% opacity.

The editor surface has no toolbar, no formatting ribbon, and no rendered preview. The author writes Markua source directly.

### Gantt phase box

Phase boxes are drawn by `GanttCanvas` (a custom Avalonia `DrawingContext` renderer). Each box: 20px tall, vertically centered in the 32px chapter row. Width proportional to the phase's date span on the time axis. Rounded 2px (`rounded.sm`).

- **Active phase** (current Chapter Status): filled `{colors.primary}`, text `on-surface`.
- **Completed phase** (Status has advanced past this phase): filled `{colors.surface-high}`, text `on-surface-muted`. Visually receded — this phase is in the past.
- **Future phase** (Status has not yet reached this phase): transparent fill, 1px `{colors.border-default}` outline, text `on-surface-muted`.

Text inside phase boxes: `ui-sm`, abbreviated phase name (e.g., "DRAFT", "EDIT") + progress percentage if set (e.g., "EDIT 60%"). Text is elided or hidden if the box is too narrow.

Part header rows in the Gantt: 36px, `surface-overlay` background, Part title in `ui-heading`. Collapsible via a chevron.

### Corkboard card

Cards are 180–260px wide (column-count-dependent), intrinsically tall (minimum ~100px). Background: `surface-elevated`. Border-radius: `rounded.lg`. Border: `border-subtle` at rest; `border-default` on hover.

**Anatomy (top to bottom):**
1. **Status stripe:** 4px-tall bar spanning the full card width, filled with the lifecycle stage color.
2. **Card body (padding 12px):** Chapter title in `ui-heading` / `on-surface`. Word count and target proximity in `ui-sm` / `on-surface-dim` (e.g., "1,847 words · 74%"). Active phase date range in `ui-sm` / `on-surface-muted` (e.g., "Drafting: May 10–?").
3. **Card footer:** Status pill (`status-pill` component) bottom-left.

### Status bar

24px, `surface-elevated`, `border-subtle` top edge. Content from left to right, separated by `|` characters in `on-surface-muted`:

`current-file-path (truncated)` · `N words` · `|` · `BOOK: N words` · `|` · `[STATUS PILL]` · `|` · `branch-name` · `N changes`

The change count is `{colors.orange}` when > 0 (uncommitted work exists). All other text is `on-surface-dim`. The status pill in the status bar is the only interactive instance — clicking it opens a compact dropdown with the 6 lifecycle stage options.

### Right panel — Notes

Header: "NOTES" in `ui-heading`. A monospace textarea (or AvaloniaEdit instance) in `editor-sm` fills the panel. Autosaves on 600ms idle. A "Saved." confirmation in `on-surface-muted` appears briefly in the panel header after each save. Notes are scoped to the active chapter; switching chapters loads new notes.

### Right panel — AI Chat

Header: "AI CHAT" in `ui-heading` + current scope indicator (e.g., "Chapter 9") in `on-surface-dim`. A segmented scope control (CHAPTER · PART · BOOK) below the header changes scope. A mode pill row (PROOFING · CONSISTENCY · LINE EDITING) above the input field sets the structured mode; free-text is always available regardless. Messages: author messages right-aligned, `surface-high` background, `on-surface` text; AI responses left-aligned, `surface-overlay` background. Font: `ui-base`. A streaming cursor or spinner in the AI response bubble while generating. After a structured-mode response: a "View N issues →" button in `primary` links to the Issues tab.

The panel has two tabs at the top: CHAT and ISSUES, in `ui-label-caps`. Switching tabs does not clear any content.

### Right panel — Issues

Header: "ISSUES" in `ui-heading` + scope. Filter row: scope dropdown · type filter · state filter — all in `ui-sm`. Issue list: each row shows a type chip (colored pill: grammar = `cyan`, inconsistency = `pink`, developmental = `primary`, readability = `yellow`, line-editing = `orange`) + description preview in `ui-base` + state badge (OPEN / RESOLVED / DISMISSED in `on-surface-muted`). Clicking a row with a line-level location scrolls the editor to that line. State toggle (OPEN → RESOLVED / DISMISSED) is a row-level action button.

### Right panel — Git

Header: "GIT" in `ui-heading`. Branch name + `on-surface-dim`. Change count. An expandable changed-files list (read-only; no per-file staging). Commit message `textarea` in `editor-sm`, placeholder: "Hymnal: save progress {date}" (the default used if left blank). "Commit & Push" primary button. Error text in `error` below the button; full raw `git` stderr in an expandable block.

### Notification banner

Full-width, appears below the title bar (above the main content area), animated in with a 150ms ease-out slide (skipped under Reduce Motion). Not modal. Three variants: error (red-tinted background, `error` border), info (cyan-tinted, `info` border), success (mint-tinted, `success` border). Error banners persist until manually dismissed. Info and success banners auto-dismiss after 4s. A dismiss `×` button appears at the right edge. Git error banners always include the raw `git` stderr in an expandable section.

## Do's and Don'ts

**Do:**
- Use the four dark surface tiers — `surface-base` through `surface-high` — as your elevation vocabulary. Never reach for shadow.
- Use all-caps `ui-label-caps` for all navigation tabs, sidebar section headers, and status pill text. This is the CORA-lineage caps convention.
- Use the lifecycle status color mapping in `## Colors` consistently and exclusively for all representations of a chapter's stage (sidebar dot, status pill, Gantt phase box active fill, Corkboard card stripe).
- Apply `{colors.primary}` purple to every active state: tab underline, sidebar selection indicator, text cursor, active phase box fill, open right-panel icon.
- Verify WCAG AA (4.5:1) for every text/background combination before marking `status: final`.
- Keep the right panel a single slot. One content at a time: Notes, AI Chat + Issues, or Git.

**Don't:**
- Use neon glow (`text-shadow`, outer glow filters) on dark backgrounds. Atmosphere comes from tonal depth, not glow effects.
- Use `box-shadow` or `drop-shadow` for surface hierarchy. Tonal elevation handles it.
- Use color alone to convey status — the stage name must always accompany the status color (accessibility; colorblind users).
- Place borders between surfaces of clearly different dark tones unless a structural edge is semantically needed.
- Add any toolbar, ribbon, or formatting affordance to the editor surface.
- Show two right panels simultaneously. The right panel slot has one occupant at a time.
- Use `on-surface` (the brightest text color) for secondary or metadata content. Reserve it for primary prose and active labels; use `on-surface-dim` or `on-surface-muted` for everything below that.
