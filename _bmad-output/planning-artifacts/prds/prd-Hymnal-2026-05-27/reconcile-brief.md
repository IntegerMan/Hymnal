---
title: "Reconciliation: Product Brief → PRD"
status: draft
created: 2026-05-27
---

# Reconciliation: Product Brief → PRD

## Input: Product Brief (brief-Hymnal-2026-05-27/brief.md)

### Gaps found

- **[high]** No Success Metrics section — The brief contains five explicit success criteria ("The author uses Hymnal as their primary writing and project-management environment while writing *A Choir of Minds* — no spreadsheet supplement required"; "The editor does not get in the way: writing in Hymnal feels equivalent to writing in a good plaintext editor"; "The complete authoring workflow… requires no context switch to another application"; etc.). None of these appear in the PRD as a formal section. The PRD has FRs and NFRs that realize these criteria piecemeal, but there is no §6-equivalent "Success Criteria" or "Acceptance Signals" section a builder can test against at V1 completion. The "no spreadsheet supplement required" criterion in particular — which is the closest thing the brief has to a hard acceptance gate — has no PRD anchor.
  → *Should land in: a new §6.4 or §7.5 "Success Criteria" section, or as named acceptance signals appended to §6.1 and §6.2.*

- **[high]** "Not a platform, not a market" is absent from Non-Goals (§5) — The brief's Vision section states explicitly: "the product is not trying to be a platform or a market." This is a scope philosophy that constrains how feature requests should be evaluated and how the architecture should be sized. Without it in the PRD, the Non-Goals section reads as a list of specific excluded features rather than a governing design principle. Scope creep risk is higher when this principle is not written down.
  → *Should land in: §5 Non-Goals, as a preamble statement or first bullet; or as a named design constraint in §7.*

- **[medium]** Secondary user persona is missing from §2 — The brief distinguishes two audiences: (1) the primary author building Hymnal while writing *A Choir of Minds*, and (2) "Other solo Leanpub authors who are comfortable with a developer-adjacent workflow (folder-as-project, Markdown, Git) but are not themselves developers." The PRD's §2.1 Jobs to be Done and §2.2 Non-Users do not establish this secondary persona. §2.2 lists "Non-Markua authors" and "Authors publishing through channels other than LeanPub" as non-users, but gives no guidance on how the secondary audience (developer-adjacent but non-developer) should inform UX complexity decisions and onboarding assumptions.
  → *Should land in: §2.1 or a new §2.2 "Personas," with secondary persona attributes stated; §2.3 could then reference both personas in user journeys.*

- **[medium]** Post-V1 roadmap signals not captured — The brief's Vision lists explicit near-term post-V1 directions: "richer AI workflows (character consistency, timeline analysis), multi-book support when the first book ships, and deeper Markua tooling as the format evolves." These are absent from the PRD. Their omission means the architect has no directional signals to inform decisions that could foreclose these paths — e.g., the data schema for `.hymnal-data/issues/` needs to be character-aware to support consistency analysis; multi-book support implies the Workspace model should not hardcode global singletons.
  → *Should land in: a brief §10 or §6.4 "Post-V1 Horizon" list (not a roadmap commitment, just directional signals for architecture).*

- **[medium]** "No context switch" not formalized as an NFR — The brief's problem narrative frames the core harm as "Context switches between these tools interrupt the writing flow," and the success criteria include "The complete authoring workflow — writing, tracking, AI review, and Git commit/push — requires no context switch to another application." This is arguably the product's single most important non-functional requirement — a design constraint on the completeness of each integrated workflow. It appears in UJ-1 as a narrative but is not stated as an NFR. Without it as an NFR, gaps in workflow coverage (e.g., a Git auth failure that forces the author to open a terminal) have no formal requirement to evaluate against.
  → *Should land in: §7 Non-Functional Requirements, as a new "Workflow Completeness" NFR (e.g., NFR-W1: "The application must support the full authoring session loop — write, track, review, commit/push — without requiring any external tool").*

- **[low]** "Tool is better for the next book" learning/iteration intent dropped — The brief closes its Vision with: "The measure of success is simple: the author finishes the book using Hymnal, and the tool is better for the next book because of what was learned writing the first." This is a statement of the product's feedback loop and long-term health model — it implies the author/PM/builder will document learnings and revise requirements continuously. It does not appear in the PRD at all. While it may feel too qualitative for a PRD, it gives implementers important context about how to treat incomplete or imperfect V1 decisions (as learning input, not failures).
  → *Should land in: §1 Vision, as a closing sentence or short paragraph.*

- **[low]** Open-source MIT license absent from NFRs — The brief states: "Hymnal is MIT-licensed, open source, and built by its primary user." The PRD mentions this in §1 Vision but does not carry it into §7 as an NFR or constraint (e.g., all bundled dependencies must be MIT/Apache/BSD-compatible; no proprietary SDKs in the core build). This gap could create a late-stage conflict if a dependency with an incompatible license is chosen during architecture.
  → *Should land in: §7 Non-Functional Requirements, as an NFR-L1 "License Compatibility" constraint.*

---

### Well-covered

The core writing, tracking, and visualization feature set translated faithfully from brief to PRD: the Workspace/`Book.txt` model (FR-1–FR-6), the Markua editor philosophy (no toolbar, no preview, focus-first — FR-7–FR-12), the Chapter Status lifecycle and Gantt view (FR-14–FR-26), and the AI Issues structure with its full type/state/location model (FR-48–FR-50) all land with fidelity to the brief's intent. The Glossary (§3) and the PRD addendum together carry the technical depth the brief gestured at — folder schema, syntax token detail, AI provider options, color palette direction — in a form that genuinely serves downstream implementers. The Non-Goals section (§5) correctly excludes every item the brief listed as out of scope.
