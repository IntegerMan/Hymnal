---
name: Hymnal
status: draft
sources:
  - _bmad-output/planning-artifacts/briefs/brief-Hymnal-2026-05-27/brief.md
  - _bmad-output/planning-artifacts/prds/prd-Hymnal-2026-05-27/prd.md
  - _bmad-output/planning-artifacts/architecture.md
updated: 2026-05-27
---

# Hymnal — Experience Spine

## Foundation

Single-surface desktop application. Windows 10+ and Linux (Ubuntu LTS, Fedora) as first-class targets. No macOS, no mobile, no web in V1. Avalonia UI 12.0 + ReactiveUI MVVM. No third-party component library — all surfaces are custom Avalonia controls.

`DESIGN.md` is the visual identity reference. This spine owns behavior, states, and interactions. `DESIGN.md` owns tokens; this spine references them by name using `{path.to.token}` syntax.

Single-window, single-workspace-at-a-time. One workspace = one open Markua manuscript folder. The app shell is persistent; the main content region switches between named views via the title bar tab navigation.

[ASSUMPTION] Custom window chrome is used on both Windows and Linux (`ExtendClientAreaToDecorationsHint`) to eliminate the native OS title bar and host Hymnal's custom 40px title bar. Fallback to default OS chrome on platforms that do not support this.

## Information Architecture

| Surface | Reached from | Purpose |
|---|---|---|
| Write — Editor | Tab "WRITE" / sidebar chapter click / Corkboard card click | Prose editing for the active chapter |
| Gantt | Tab "GANTT" | Phase-timeline view for all chapters |
| Corkboard | Tab "CORKBOARD" | Card-based manuscript overview |
| Notes panel | Notes toggle (right panel) in any view | Per-chapter author notes alongside the editor |
| AI Chat panel | AI icon in title bar | Editorial chat, scoped to chapter / part / book |
| Issues panel | Issues tab within AI panel / margin indicator click | Tracked AI editorial findings |
| Git panel | Git icon in title bar | Stage-all → commit → push workflow |
| Settings | Settings gear in title bar | App preferences, AI provider configuration |
| Welcome screen | App launch with no prior workspace | Folder-picker entry point with recent workspaces list |

**Navigation model.** A 40px title bar spans the full window width. Left region: Hymnal icon + workspace name (clickable: recent-workspace popover + "Open folder…"). Center: three primary view tabs — **WRITE · GANTT · CORKBOARD** — with `{colors.primary}` underline on the active tab. Right: AI, Git, and Settings icon buttons. Clicking an already-active icon button closes its panel.

**Left sidebar** is persistent across all views. It contains the manuscript tree (Parts and Chapters) and the Supplemental Docs section. It is collapsible (slides out left; main content expands to fill). Default: open.

**Right panel** is a single-slot toggle region. Contents: Notes, AI Chat + Issues tab strip, or Git — whichever was last activated. Only one right panel content is shown at a time. Default: closed.

**Status bar** (24px, bottom of window) is persistent on all views.

→ Composition reference: key-screen mocks for Write/Editor and Gantt to be created at Finalize. Spine wins on conflict.

## Voice and Tone

Microcopy. Brand voice and aesthetic posture live in `DESIGN.md`.

| Do | Don't |
|---|---|
| "Open a folder to begin." | "Welcome to Hymnal! Let's get started." |
| "3 changes staged." | "Files ready to commit!" |
| "Saved." | "✓ File saved successfully." |
| "No chapters yet." | "Your manuscript is empty. Click here to add your first chapter!" |
| "Commit & Push" | "Save to Cloud" |
| "Outlining · 0 words" | "This chapter has no words yet." |
| Paste raw `git` error text verbatim in Git error surfaces. | "Something went wrong. Please try again." |
| "Chapter notes" | "Notes for this chapter" |
| Sentence case for all UI text. | Title Case everywhere. |
| All-caps for navigation tab labels and status stage names in pills only. | All-caps anywhere else. |

Hymnal does not celebrate or encourage. It records. "Saved." is the complete success confirmation. The author knows what they accomplished.

## Component Patterns

Behavioral. Visual specs live in `DESIGN.md.Components`.

| Component | Use | Behavioral rules |
|---|---|---|
| Sidebar chapter row | Manuscript tree | Single-click: select chapter and open it in the Write editor. If currently in Gantt or Corkboard, clicking a chapter row switches to Write and opens it. Right-click: context menu. |
| Chapter context menu | Sidebar, Corkboard | Items: Open / Set Status (submenu: 6 stages) / Rename / Remove from Book.txt / New chapter below. Remove from Book.txt requires confirmation (single confirm dialog; not destructive to the file on disk). |
| Status dot | Sidebar row, Corkboard card | 8px colored circle. Display-only — not directly clickable. Status is changed via the status pill in the status bar or via the context menu. Tooltip on hover: the stage name. |
| Status pill (status bar) | Status bar | Clickable: opens a compact dropdown with the 6 lifecycle stage options. Selecting a stage applies FR-15 behavior (today's date pre-fills the Phase start date if none is set and the setting is enabled). |
| Gantt phase box — active / future | Gantt canvas | Click opens an inline popover: start date field + end date field (date-picker or typed entry, `yyyy-mm-dd` format). A progress percentage field (0–100, optional) appears below the dates on the active phase box only. Clicking outside the popover or pressing Escape closes it and saves entered values. |
| Gantt phase box — completed | Gantt canvas | Click-to-edit is disabled. Tooltip shows the dates for reference. |
| Part header row (Gantt) | Gantt | Clicking the chevron or the Part name collapses/expands all chapter rows in that Part. Collapse state is session-only (not persisted to `.hymnal-data/`). Part header shows Part name + summary (e.g., "3 Drafting, 1 Editing, 2 Done"). |
| Corkboard card | Corkboard | Single-click: opens the chapter in Write view. Drag (Late V1): reorders. No multi-select in V1. |
| Notes textarea | Notes right panel | Autosaves on 600ms idle after any keystroke. No explicit Save action required. "Saved." appears briefly in the panel header after each autosave cycle. Switching chapters loads that chapter's notes; unsaved content from the prior chapter is autosaved first. |
| Commit message input | Git right panel | Placeholder text: "Hymnal: save progress {YYYY-MM-DD}" (used verbatim if left blank). "Clear" button at the right edge of the field. |
| AI scope selector | AI Chat right panel header | Segmented control: CHAPTER · PART · BOOK. Changing scope preserves the chat history for each scope independently — returning to a scope restores its prior conversation. |
| Structured mode pills | AI Chat panel | PROOFING · CONSISTENCY · LINE EDITING pill row above the input field. Clicking a pill sets the mode; the input placeholder updates to reflect it. Free-text input is always available regardless of mode selection. Deselecting a pill returns to free-text mode. |
| Issue row | Issues panel | Single-click on an issue with a line-level location: opens the relevant chapter in Write (if not already open) and scrolls the editor to the line. State action: a button or right-click option to toggle OPEN → RESOLVED or OPEN → DISMISSED. |
| Notification banner | Below title bar | Error banners: persist until manually dismissed (×). Info and success banners: auto-dismiss after 4s. Git error banners: always include the raw `git` stderr output in a collapsed expandable section (click "Show details" to expand). |
| Missing chapter indicator | Sidebar | A `⚠` icon + `on-surface-muted` text on the chapter row. Clicking does nothing. Tooltip: "File not found: {path}". Context menu: "Remove from tree." |
| Welcome screen | Cold app launch | Centered layout: Hymnal logo mark + "Open a folder to begin." primary button + recent workspaces list (up to 5 entries, each clickable to reopen). If no recent workspaces, the list is omitted. |

## State Patterns

| State | Surface | Treatment |
|---|---|---|
| Cold launch — no prior workspace | Welcome screen | Centered: logo, "Open a folder to begin." button, recent list (empty). |
| Cold launch — workspace restored | Write — Editor | Previous workspace reloads. Last-edited chapter restores to the editor with scroll and cursor position preserved. Sidebar opens. |
| External `Book.txt` change detected | Any view | Info banner: "Book.txt changed outside Hymnal. [Reload] [Dismiss]". Dismissing keeps the current in-memory model; Reload rebuilds the chapter tree. |
| Missing chapter file | Sidebar | Chapter row visible with `⚠` icon and `on-surface-muted` label. Not clickable in editor. |
| No Git repository at workspace | Git panel | Panel body: "No Git repository found at this workspace. Initialize one with `git init` in your terminal." Git icon visible but panel shows this message. Git features are entirely hidden from the status bar. |
| Git: uncommitted changes | Status bar | Change count turns `{colors.orange}`. No other behavior change. |
| Git operation in progress | Git panel | "Commit & Push" button disabled and shows a spinner. All other Git panel inputs disabled. |
| Git error | Git panel | Inline error text below the button in `{colors.error}`. Raw `git` stderr in a collapsed "Show details" expandable. |
| AI: provider not configured | AI panel (all tabs) | Body: "Configure an AI provider in Settings to use AI features." Settings link navigates directly to the AI provider section of Settings. |
| AI: generating (streaming) | AI Chat panel | Tokens stream into the AI response bubble as they arrive. A spinner appears in the panel header until the first token; the spinner disappears once streaming begins. |
| AI: structured mode complete | AI Chat panel | A "View N issues →" link button in `{colors.primary}` appears at the bottom of the AI response. Clicking switches to the Issues tab within the right panel. |
| Issues panel — no issues for scope | Issues panel | "No issues for {scope}." in `on-surface-dim`. No empty-state illustration. |
| Target set, word count within range | Sidebar, Gantt row | Progress fill visible. No special state color — the fill itself communicates proximity. |
| Target set, word count over maximum | Sidebar, Gantt row | Progress fill reaches 100% and color turns `{colors.yellow}` (overlong). Tooltip: "{N} words over target maximum." |
| Unsaved editor changes on workspace close | Editor | System dialog: "You have unsaved changes to '{chapter}'. Save before closing?" — Save / Don't Save / Cancel. Standard Avalonia dialog. |
| Workspace switch with unsaved changes | Editor | Same dialog as above before loading the new workspace. |
| Chapter with no words | Sidebar | Word count: "0 words" in `{colors.on-surface-muted}`. No special indicator. |
| AI summary not yet generated | AI Chat panel (on open) | Scope indicator shows "No summary — generate one to improve context quality." with a [Generate Summary] button. Chat can still be used without a summary. |

## Interaction Primitives

**Mouse-first with full keyboard support.** Hymnal's primary user is a solo author who expects a conventional desktop application. Keyboard shortcuts are provided for the most frequent actions and are discoverable via tooltips (shown after ~500ms hover on icon buttons).

| Action | Shortcut | Notes |
|---|---|---|
| Save current chapter | `Ctrl+S` | Standard editor save. Only applies in Write view with an active chapter. |
| Switch to Write view | `Ctrl+1` | |
| Switch to Gantt view | `Ctrl+2` | |
| Switch to Corkboard view | `Ctrl+3` | |
| Toggle sidebar | `Ctrl+B` | VS Code muscle-memory convention. |
| Toggle right panel (last used) | `Ctrl+J` | Opens the most recently active right panel content. |
| Toggle Notes panel | `Ctrl+Shift+N` | Directly opens Notes regardless of last panel state. |
| Toggle AI panel | `Ctrl+Shift+A` | Directly opens AI Chat. |
| Toggle Git panel | `Ctrl+Shift+G` | Directly opens Git. |
| Open Settings | `Ctrl+,` | VS Code convention. |

**Text editing.** Standard AvaloniaEdit keyboard conventions throughout. No custom keybindings in the editor in V1. No vim mode.

**Drag.** Drag-to-reorder Corkboard cards is Late V1 (FR-31). No drag interactions in Early V1, including the Gantt (FR-27).

**Context menus.** Right-click on chapter rows (sidebar and Corkboard) opens the chapter context menu. Right-click in the editor body: system default (AvaloniaEdit built-in). Exception: right-clicking on an issue margin indicator opens the Issues panel filtered to that issue.

**Banned everywhere:**
- Modal stacks deeper than one level.
- Auto-save of manuscript `.md` files without explicit author action.
- Confirmation dialogs for non-destructive actions.
- Any write to a chapter `.md` file outside of an explicit `Ctrl+S` or Save dialog.
- Drag interactions on the Gantt in Early V1.

## Accessibility Floor

Behavioral. Visual contrast targets live in `DESIGN.md`.

- WCAG 2.2 AA across all surfaces. All primary and body text at ≥ 4.5:1 against its immediate background. Note: `{colors.cyan}` and `{colors.status-done}` (mint) on `{colors.surface-elevated}` must be verified — brightness adjustments may be needed to meet the ratio.
- All interactive controls are keyboard-reachable via `Tab` traversal. Tab order matches the visual reading order on every view.
- Focus rings: 2px `{colors.primary}` outline, 2px offset, visible against all backgrounds. The editor's own focus ring follows AvaloniaEdit defaults and must be verified to meet AA.
- Screen readers (Narrator on Windows, Orca on Linux): every labeled interactive element has an accessible name. Status pills include the stage name as text, not color alone. The status pill in the status bar has role `combobox`.
- Status lifecycle colors are never the sole indicator of a chapter's stage — the stage name is always present as text (in the pill or as a tooltip on the status dot).
- Notification banners are announced via an Avalonia automation property equivalent when they appear.
- The Git error expandable block is keyboard-accessible (Enter to expand/collapse).
- Reduce Motion: Avalonia's OS reduced-motion preference should disable the 150ms panel slide animation — panels appear/disappear at full size immediately.
- [ASSUMPTION] AvaloniaEdit's built-in accessibility support is relied upon for the editor surface. Any gaps found in testing are tracked as issues but are not V1 blockers.

## Responsive & Platform

**Fixed desktop window, not responsive.** No breakpoints. The window is freely resizable; panels collapse at the author's discretion. [ASSUMPTION] Minimum window size: approximately 680px wide × 420px tall.

**Windows 10+ specifics.**
Custom window chrome via `ExtendClientAreaToDecorationsHint`. OS credential store: Windows Credential Manager (`PasswordVault`). Font rendering: Skia rasterizer via Avalonia (not GDI) — both embedded fonts render consistently with Linux. Taskbar integration: standard single-window task entry; no jump lists in V1.

**Linux (Ubuntu LTS, Fedora) specifics.**
Credential store: `libsecret` / SecretService D-Bus API. Font rendering: Skia rasterizer via Avalonia; no system font packages required. Window manager: tested on both X11 and Wayland. [ASSUMPTION] Custom title bar drag-to-move on Wayland requires Avalonia's `_MOTIF_WM_HINTS` or XDG decoration protocol fallback — implementation detail for architecture, not a UX decision.

**macOS** is not a V1 target. No design decisions for macOS.

## Inspiration & Anti-patterns

**Lifted from CORA (author's prior app):**
- Top horizontal tab navigation centered in the title bar for primary view switching. The author has established this pattern and finds it natural.
- All-caps small-tracking `ui-label-caps` labels for navigation tabs and section headers.
- Dark hierarchy via tonal surface stacking (four levels, no shadows or heavy borders).
- Pink/magenta as a foregrounded, first-class accent color — not merely a warning indicator.
- Three-panel shell: left persistent sidebar · main content · collapsible right panel.

**Lifted from VS Code:**
- `Ctrl+B` for sidebar toggle — muscle memory for the author's primary development environment.
- `Ctrl+,` for Settings.
- Source-text-only editing as the default and only writing surface.

**Lifted from iA Writer:**
- No formatting toolbar. The editor is Markua source plus syntax highlighting — no mode-switch between edit and preview.
- Typography discipline in the editor: maximum line width (70ch), generous line height (1.7), large font floor (16px). The author's eye should be on the prose, not squinting at dense text.

**Rejected — Toolbar above the editor.** The author has stated this explicitly. No toolbar. All formatting is entered as Markua source text. Any implementation that adds formatting buttons to the editor violates a core product decision.

**Rejected — Rendered Markdown preview panel.** Explicitly out of scope. Source-text-only editing.

**Rejected — Gamification and productivity signals.** Hymnal does not congratulate the author for writing 1,000 words, maintaining a streak, or completing a chapter. It records. The work is its own reward. No celebration banners, no "🎉 Great session!" toasts, no streak counters.

**Rejected — Drag-to-reorder in Early V1 Gantt.** The Gantt is an edit surface for phase dates in Early V1; row reordering is Late V1. No drag affordance appears on Gantt rows in Early V1.

**Rejected — Multiple simultaneous right panels.** The right panel is one slot. Git, Notes, AI Chat, and Issues never appear at the same time. This constraint keeps the editor dominant and prevents visual fragmentation.

**Rejected — Per-file staging in the Git panel.** Stage-all is the only staging action. Git power operations belong in the author's external Git client or terminal, not in Hymnal.

## Key Flows

### Flow 1 — Morning writing session (Matthew-Hope, 8:30am, coffee in hand)

Matthew-Hope is writing Part 2 of *A Choir of Minds*. Chapter 9 is in Drafting. He stopped at 1,847 words yesterday.

1. He opens Hymnal from the taskbar.
2. The app loads in under 5 seconds. The previous workspace restores automatically. The sidebar shows the manuscript tree; Chapter 9 ("The Reckoning") is highlighted — it was the last-edited chapter. The editor opens to his last scroll position.
3. He glances at the status bar: `part2/09-the-reckoning.md · 1,847 words · BOOK: 54,201 words · DRAFTING · main · 0 changes`. Everything is where he left it.
4. He starts writing. Word count in the status bar and sidebar ticks up live. His chapter-level target is 2,500 words; the proximity fill in the sidebar is at 74%.
5. He writes for 75 minutes and reaches 2,243 words (89% of target). He presses `Ctrl+S`.
6. He clicks the `DRAFTING` pill in the status bar. A dropdown appears: Outlining / Drafting / Editing / Polishing / Reviewing / Done. He selects **Editing**.
7. **Climax:** The status dot in the sidebar shifts from cyan to purple. The Editing phase's start date pre-fills today's date in the Gantt. The status bar now reads `EDITING`. He glances at the sidebar — Chapter 9, purple dot, 2,243 words. No spreadsheet opened this session. He presses `Ctrl+Shift+G` to open the Git panel, enters a brief commit message, and presses "Commit & Push." The status bar reads `0 changes`. Session complete.

---

### Flow 2 — Weekly planning review (Matthew-Hope, Sunday evening, planning the coming week)

He wants to understand where the manuscript stands before deciding what to work on.

1. He clicks **GANTT** in the title bar.
2. The Gantt loads. All Parts are expanded. Part 1: chapters are Done (mint) and Reviewing (pink). Part 2: mostly Drafting (cyan) and Editing (purple), with one chapter still Outlining (gray). Part 3: all gray.
3. He notices Chapter 12 in Part 2 has no Drafting end date. He clicks the Drafting phase box on that row. An inline popover appears with Start and End date fields. He types an estimated end date; presses Enter. The box updates on the canvas.
4. He clicks **CORKBOARD**. Part 3's cards are all gray with "OUTLINING · 0 words". Three cards have placeholder titles — he hadn't named these chapters yet.
5. He clicks Chapter 17's card ("Part 3 — Chapter 3"). The app switches to the Write view and opens Chapter 17 in the editor.
6. **Climax:** He begins sketching an outline for Chapter 17 directly in the editor. When he saves and advances the status to Drafting, the sidebar dot and Gantt row for Chapter 17 will shift to cyan — proof that his Sunday planning session is recorded in the manuscript's state, not in a separate spreadsheet.

---

### Flow 3 — AI editorial round-trip (Matthew-Hope, after completing a draft of Part 1)

Part 1 is entirely in Reviewing status. He wants a consistency check on Chapter 3.

1. He opens Chapter 3 ("New Game") in the Write editor. He clicks the AI icon in the title bar.
2. The AI Chat panel slides into the right panel slot. Scope is set to **CHAPTER** by default. The panel header shows: "AI CHAT · Chapter 3" and a note: "No summary — generate one to improve context quality." with a [Generate Summary] button.
3. He clicks [Generate Summary]. A spinner. After ~8 seconds: "Summary generated." The note disappears.
4. He clicks **CONSISTENCY** in the structured mode pill row, then presses Enter.
5. The panel header shows a spinner. After ~14 seconds, an AI response populates with a prose summary of findings.
6. A "View 4 issues →" button appears at the bottom of the response. He clicks it. The panel switches to the Issues tab.
7. Issue list: 2 grammar, 1 inconsistency (line-level), 1 line-editing. He clicks the inconsistency issue. The editor scrolls to line 43. A yellow margin indicator appears on that line. The relevant paragraph is visible.
8. **Climax:** He reads the finding — a character described as left-handed in Chapter 3 was right-handed in Chapter 1. He edits the line. He returns to the Issues panel and marks the issue RESOLVED. The yellow margin indicator disappears. He dismisses the line-editing issue as intentional. The Issues panel is empty for this chapter. He presses `Ctrl+Shift+G` to commit the edit.
