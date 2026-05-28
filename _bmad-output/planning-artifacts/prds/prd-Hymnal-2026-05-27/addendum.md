---
title: "Addendum: Hymnal PRD"
status: final
created: 2026-05-27
updated: 2026-05-27
---

# Addendum: Hymnal PRD

_Technical depth that belongs in downstream documents (architecture, solution design, UX spec) or that was captured during discovery but does not fit the PRD's main narrative._

---

## Manuscript Folder Structure (confirmed — for architecture)

Parts live as subfolders within `manuscript/`. Each Part folder contains a `part.md` file (carrying the Markua part-marker heading) and the Chapter files for that Part. Chapters not belonging to a Part sit at the `manuscript/` root.

```
manuscript/
├── Book.txt                          # ordered manifest — folder-prefixed paths
├── part1/                            # Part folder — identified by presence of part.md
│   ├── part.md                       # {class: part}\n\n# New Game
│   ├── prologue.md
│   ├── 01-first-chapter.md
│   └── 02-second-chapter.md
├── part2/
│   ├── part.md                       # {class: part}\n\n# The Reckoning
│   ├── 01-arrival.md
│   └── 02-descent.md
└── epilogue/                         # also a Part folder (has part.md)
    ├── part.md                       # {class: part}\n\n# Continue
    └── 01-aftermath.md
```

Example `Book.txt`:
```
part1/part.md
part1/prologue.md
part1/01-first-chapter.md
part1/02-second-chapter.md

part2/part.md
part2/01-arrival.md
part2/02-descent.md

epilogue/part.md
epilogue/01-aftermath.md
```

**Key rules:**
- A subfolder is a Part if and only if it contains a `part.md` file.
- `part.md` format: `{class: part}` attribute on line 1, blank line, then a `# Title` heading.
- Part folder names are author-defined — numbered, semantic, or mixed.
- Part order and Chapter order are both determined by `Book.txt` — folder name order on disk is irrelevant.
- Moving a Chapter between Parts (FR-31) moves its `.md` file into the target Part's subfolder and updates `Book.txt` to use the new folder-prefixed path.

---

## .hymnal-data/ Folder Schema (for architecture)

Proposed folder structure for Hymnal's metadata directory. Implementation may vary; this is the expected shape for architecture planning.

```
.hymnal-data/
├── notes/
│   ├── chapter-01-intro.md          # per-chapter notes (filename mirrors chapter file)
│   └── chapter-02-the-world.md
├── phases/
│   └── phases.json                  # phase dates + progress % per chapter, keyed by chapter filename
├── targets/
│   └── targets.json                 # word count targets per chapter, part, book
├── summaries/
│   ├── book.md                      # book-level AI summary
│   ├── part-1-foundations.md        # part-level AI summary (keyed by part folder name)
│   └── chapter-01-intro.md          # chapter-level AI summary
├── issues/
│   └── issues.json                  # structured issues array (or one file per issue)
├── docs/
│   ├── characters/
│   │   └── protagonist.md           # author-created supplemental docs, any structure
│   └── research/
│       └── world-building-notes.md
└── exclusions.json                  # (Late V1) chapters excluded from Book.txt build
```

**Notes:**
- `phases.json` and `targets.json` are the primary files Hymnal writes to on every status update or target edit. They should use a stable schema with chapter filename as key to survive Book.txt reorders without orphaning data.
- `issues/` could be a single `issues.json` array or one file per issue — the latter is more Git-diff friendly but creates many small files. Decision deferred to implementation (see OQ-3 in PRD).
- `exclusions.json` tracks chapter filenames that have been excluded from the active build (FR-32). Enables re-inclusion without re-typing filenames.

---

## Markua 0.30 Syntax Highlighting Tokens (for UX/architecture)

Recommended token categories and palette assignment guidance for FR-8. Final hex values are a UX design decision.

| Token category | Markua constructs | Suggested palette role |
|---|---|---|
| Structural directive | `{mainmatter}`, `{backmatter}`, `{frontmatter}` | Orange accent — marks section boundaries |
| Part marker | `# Part N #` heading syntax (Markua 0.30 spec); `{class: part}` attribute on a heading (LeanPub/real-world form — see FR-3) | Purple primary — most prominent structural element |
| Attribute list | `{key: value, ...}` blocks | Pink accent — metadata, distinct from prose |
| Sample / include directives | `{sample: true}`, `{id: ...}`, `{format: ...}` | Yellow accent — LeanPub-specific, important to spot |
| Blurb prefix | `A>`, `B>`, `C>`, `D>`, `E>`, `I>`, `Q>`, `T>`, `W>`, `X>` | Pink or orange accent |
| Magic comments | `markua-start-insert`, `markua-end-insert` | Muted orange or gray — editorial markers |
| Standard heading | `#` through `#####` | Slightly brighter base text or purple tint |
| Standard Markdown | Bold, italic, inline code, fenced code, links | Standard Markdown editor conventions adapted to dark palette |
| Prose / body text | Everything else | Near-white on dark background; highest contrast |

---

## AI Provider Integration Options (for architecture)

Two integration approaches identified for FR-44. Choice is an architecture decision.

### Option A: LiteLLM-compatible endpoint
- Author configures a URL + API key pointing to any LiteLLM proxy or compatible endpoint (OpenAI-API-shape).
- Hymnal sends `POST /chat/completions` with model name and message array.
- Advantage: single integration surface works with OpenAI, Anthropic, local models (Ollama via LiteLLM), Azure OpenAI, etc.
- Disadvantage: requires the author to run or access a LiteLLM proxy if they are not using a natively compatible API.

### Option B: Microsoft Extensions for AI (MEAI) / Semantic Kernel
- Hymnal uses MEAI's `IChatClient` abstraction; the author configures a provider-specific connector.
- Advantage: native .NET integration, multi-provider without a proxy, consistent with ecosystem.
- Disadvantage: author must select a provider-specific NuGet connector; configuration is more complex.

**Recommendation:** Support both via an abstraction layer — expose LiteLLM-style endpoint config as the simple path (one URL + one key), and optionally expose MEAI connector config for power users. Architecture doc should decide.

---

## Synthwave Color Palette Direction (for UX)

Author preference stated: dark theme, synthwave aesthetic, purple primary, yellow + pink + orange accents.

Reference aesthetics for the UX designer:
- Deep dark background (near-black with slight purple or blue-gray tint, not pure #000000)
- Purple: primary interactive accent, headings, active phase highlight, sidebar selection states
- Yellow: warning states, target-proximity indicators, certain syntax tokens
- Pink/magenta: Markua attribute and directive tokens, hover states, secondary badges
- Orange: structural directives, Git notification indicators, warning/error states
- All text on backgrounds must meet WCAG AA (4.5:1 minimum)

Anti-reference: avoid neon-on-black with no depth (retro CRT look); prefer a slightly elevated, layered surface hierarchy with subtle gradients or transparency.

---

## Performance Baseline Assumptions (for architecture)

NFR-Perf1–3 are based on the following assumptions:

- **200,000 words** is approximately 2× the length of a typical novel; a generous upper bound for the primary use case (A Choir of Minds is expected to be 80–120K words).
- **500 ms word count update latency** assumes the word count calculation runs on a background thread and debounces keystrokes; it does not block the editor UI thread.
- **5-second cold start** includes framework initialization, Book.txt parse, and first render of the sidebar. Assumes a mid-range dev machine (e.g., 8-core CPU, NVMe SSD, 16GB RAM).
- **100 chapters** is a generous upper bound; most novels have 20–40 chapters.

These bounds should be validated against the actual .NET framework choice (WPF, Avalonia, etc.) during architecture.

---

## Post-V1 Directional Signals (for architecture)

The following capabilities are out of scope for V1 but are likely future directions. Architecture decisions that could easily accommodate these without over-engineering (e.g., schema flexibility, stable IDs) are worth noting now:

- **Character and entity consistency checking** — AI-assisted cross-chapter consistency for named characters, places, and recurring details.
- **Timeline analysis** — detecting in-manuscript chronological inconsistencies.
- **Multi-book project library** — managing multiple Workspaces from a single app session.

These signals should influence schema choices (stable chapter IDs, extensible `.hymnal-data/` keys) but do not drive V1 requirements.

---

## Rejected Alternatives (carried from brief addendum)

- **Rendered preview panel** — explicitly rejected. Author prefers pure Markdown editing with syntax highlighting.
- **Git operations in V1 (original brief)** — brief originally excluded Git ops entirely. Reopened during PRD discovery as lightweight commit/push (see `.decision-log.md` DL-6).
- **Multi-book project library** — out of scope for V1.
- **Collaboration / multi-author** — out of scope entirely for V1.
- **Structured supplemental docs (linked entities)** — considered during Discovery; author declined in favor of a plain folder approach.
