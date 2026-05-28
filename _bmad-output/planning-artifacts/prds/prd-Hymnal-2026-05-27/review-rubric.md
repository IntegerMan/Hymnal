---
title: "PRD Quality Review — Hymnal"
created: 2026-05-27
reviewer: GitHub Copilot (Claude Sonnet 4.6)
prd-version: draft 2026-05-27
---

# PRD Quality Review — Hymnal

## Overall verdict

The PRD is well-crafted for its tier — Vision is genuine, FR specificity is high, the Glossary is thorough, and the Early/Late V1 split is cleanly drawn. The primary structural failure is a missing Success Metrics section, which severs the feedback loop between the thesis and what "winning" looks like. A secondary cluster of issues (one non-open Open Question, one orphaned reference, two vague done-ness clauses) are fixable in an hour and should be before this PRD is handed to a dev agent.

---

## 1. Decision-readiness — **adequate**

The scope, V1 boundaries, and feature intent are clearly enough drawn that a decision-maker can greenlight Early V1 work. Trade-offs are present (AI provider options in addendum, Git exclusion format deferred, inclusion toggle storage deferred). The weak spot is OQ-5, which is not genuinely open.

### Findings

- **high** — OQ-5 is answered, not open (§8, OQ-5) — "The latter is safer and simpler for V1" is a recommendation posing as a question. A downstream dev agent may wait for a decision that has already been made. *Fix:* Close OQ-5 with resolution language: "Resolved: surface raw Git error and leave local commit in place. No retry / credential prompt in V1."

- **medium** — DL-6 is an orphaned reference (addendum, "Rejected Alternatives") — The addendum mentions "Reopened during PRD discovery as lightweight commit/push (DL-6)" but there is no decision log in the PRD, addendum, or visible workspace. A contributor encountering this has no place to look it up. *Fix:* Either add a small Decision Log section to the PRD, or remove the DL-6 citation and inline the rationale.

- **low** — No explicit rationale trail for the AI feature landing in Late V1 (§6.2) — FR-44–FR-50 are grouped as Late V1 in §6 without a stated reason. A PM or contributor might question the deferral. *Fix:* Add a one-sentence rationale in §6.2 (e.g., "AI features deferred to Late V1: the writing workflow must be stable before editorial layer is added").

---

## 2. Substance over theater — **strong**

The PRD earns its content. Vision (§1) is Markua/Git-specific, not generic. JTBDs (§2.1) are grounded in the real workflow of a solo author. NFRs carry numeric thresholds (500 ms, 5 s, 2 s, 100 chapters, 200 K words) with baseline reasoning offloaded to the addendum. No persona theater detected. No swappable-vision language detected.

### Findings

- **low** — NFR-V2 is a near-duplicate of FR-12 (§7 vs §4.2) — "UI chrome is minimal. Panels not in active use are collapsible. The editor surface expands to fill available width when side panels are hidden" duplicates FR-12's "Focus-first editor layout." One of these is design intent (FR), the other is a verifiable constraint (NFR). The NFR is justified but should be phrased as a measurable constraint (e.g., "panels must be fully collapsible on all views") rather than repeating FR prose. *Fix:* Tighten NFR-V2 to add only what FR-12 doesn't state — e.g., collapsible behavior applies to all views, not just the editor.

- **low** — FR-11 (dark synthwave theme) is both an FR and repeated in NFR-V1 contrast requirement — acceptable split, but the NFR should reference FR-11 directly rather than restating the color scheme. *Fix:* Add a cross-reference "(see FR-11)" to NFR-V1.

---

## 3. Strategic coherence — **thin**

The thesis is clearly articulated ("plaintext and Git are the right foundation; Hymnal stays out of the way"). Features map cleanly to JTBDs and user journeys. The Early/Late V1 build sequence follows the thesis. **However, there are no Success Metrics anywhere in the document.** The thesis cannot be validated, and no counter-metrics exist. This is the PRD's most significant structural gap.

### Findings

- **critical** — Success Metrics section is absent (§ missing) — The essential spine requires a Success Metrics section. The rubric specifically flags it. Without it, there is no definition of "won" for V1, no signal for whether the AI features (Late V1) are worth the investment, and no basis for a retrospective. For a solo meaningful-personal-launch project, even lightweight metrics close this gap. *Fix:* Add §7a (or merge into §7) "Success Metrics" with at minimum: (a) daily active use by the author on *A Choir of Minds* for 30 days post-launch; (b) full manuscript load under NFR-Perf2 threshold; (c) zero unintentional writes to manuscript files. Optional counter-metric: editing session abandoned because Hymnal got in the way > 0 is a failure signal.

- **medium** — No counter-metrics anywhere — Even absent a formal section, no feature in §4 names a "too much" condition. For example, the Gantt view has no stated upper bound on what makes it unusable; the AI chat panel has no failure condition. *Fix:* Add at least one counter-metric to the Success Metrics section: e.g., "opening Hymnal and immediately switching to VS Code instead = Hymnal is failing its core value prop."

---

## 4. Done-ness clarity — **adequate**

The majority of FRs are specific and actionable. The `Consequences:` sub-pattern (used on FR-1, FR-2, FR-3) improves clarity significantly but is applied inconsistently — only three FRs use it. Most FRs are still unambiguous because they name specific behaviors. Two FRs contain vague done-ness language that would block an engineer.

### Findings

- **high** — FR-12: "values comfortable for extended prose writing sessions" is not implementable (§4.2, FR-12) — "Line height, reading width, and font sizing default to values comfortable for extended prose writing sessions" gives an engineer no target. *Fix:* Supply concrete defaults, even as a starting point: e.g., "line height: 1.6–1.8 em; reading width: 65–75 characters; font size: 16–18px (configurable in settings)." These can be tagged `[ASSUMPTION]` if not yet confirmed.

- **medium** — FR-9: "V1 validated patterns at minimum" is not closed (§4.2, FR-9) — The phrase leaves the validation scope indefinitely expansive. An engineer doesn't know if implementing the three listed patterns satisfies V1 or if there's an expected extended set. *Fix:* Change to "V1 validates exactly the following patterns" and list them as a closed set. Note that additional patterns are post-V1 scope.

- **medium** — FR-4: "shown in the tree marked as missing (visually distinct)" (§4.1, FR-4) — "Visually distinct" is not a done-ness criterion. *Fix:* Specify the visual treatment (e.g., "shown with a strikethrough and warning icon") or defer the exact style to the UX spec and reference it explicitly.

- **low** — FR-26: "approximately one month out" (§4.4, FR-26) — Minor vagueness; probably intentional flexibility, but "28 days" or "30 days" would be more testable. *Fix:* Pick a specific number or tag as `[ASSUMPTION]` for architecture to validate.

- **low** — `Consequences:` pattern inconsistently applied (§4.1 vs rest of §4) — FR-1, FR-2, FR-3 use a `Consequences:` sub-section that materially improves done-ness for those FRs. Most subsequent FRs omit it. Not a blocking issue but worth noting for future PRD passes.

---

## 5. Scope honesty — **strong**

Non-Goals (§5) do real work — each exclusion is specific (no preview panel, no macOS, no LeanPub API, no spell-check engine). `[ASSUMPTION]` tags are used in FR-32 and FR-47. Non-users (§2.2) are named. The addendum explicitly flags which content belongs to which downstream doc (architecture, UX). Scope honesty is the PRD's strongest dimension.

### Findings

- **medium** — Assumptions Index (§9) has only 5 entries for 50 FRs — Not every FR requires an assumption, but several FRs contain implicit constraints that aren't surfaced. For example: FR-44 assumes OS credential store APIs are accessible from .NET on both Windows and Linux — this is a real portability question that should be tagged. FR-3's Part detection heuristic assumes `part.md` is a sufficient and unambiguous identifier for a Part folder. *Fix:* Do a pass over §4 specifically looking for platform-portability and file-format assumptions; add 3–5 more entries to §9.

- **low** — FR-16's word count formula is described but not tagged `[ASSUMPTION]` in §9 — The §9 entry for FR-16 is marked `(confirmed)` but it's the only confirmed entry; the others are unconfirmed. The distinction is valuable — the confirmed tag should be explained in a brief §9 preamble so a downstream reader knows what "confirmed" means (e.g., "confirmed = validated against the actual manuscript during PRD discovery"). *Fix:* Add a one-line preamble to §9 explaining confirmed vs. unconfirmed.

---

## 6. Downstream usability — **adequate**

FR IDs are globally numbered FR-1–FR-50 with no visible gaps, stable across the document, and correctly back-referenced in §6 MVP Scope. Glossary terms are used consistently — "Chapter," "Part," "Status," "Phase," "Workspace," "Corkboard," "Gantt view," "Summary," "Issue" all use the capped forms established in §3. The addendum is well-signposted. Two issues reduce dev agent confidence.

### Findings

- **high** — FR-27 / §6.2 creates a scoped-state split that a dev agent must reconcile across two sections — FR-27 defines a read-only constraint ("not draggable in Early V1"). §6.2 supersedes it ("Gantt drag-to-reorder rows (supersedes FR-27 read-only restriction)"). A dev agent reading FR-27 in isolation will implement the Early V1 restriction without knowing §6.2 widens it for Late V1. *Fix:* Add a forward reference in FR-27: "See §6.2 for Late V1 extension of this constraint." This also creates a clear search-and-find path.

- **medium** — Addendum is not explicitly linked from every referencing FR — FR-8 references "See addendum for token-level color assignments." FR-44 describes AI provider integration options without linking the addendum. No other FRs reference the addendum. A dev agent might miss the AI provider detail. *Fix:* Add "(see addendum: AI Provider Integration Options)" to FR-44 to match the pattern established in FR-8.

- **low** — The Product Brief cross-reference is structural but inaccessible — §0 states the PRD "builds on the Hymnal Product Brief (2026-05-27)." The brief is in a separate folder (`_bmad-output/planning-artifacts/briefs/`). No path or link is provided. A contributor cloning the repo may not find it. *Fix:* Add a relative file path to the brief reference in §0.

---

## 7. Completeness of essential spine — **adequate**

| Spine element | Present | Notes |
|---|---|---|
| Vision | ✅ | §1 — specific and earned |
| Target User | ✅ | §2 — JTBDs + Non-Users + User Journeys |
| Glossary | ✅ | §3 — thorough, 16 terms |
| Features + FRs | ✅ | §4, FR-1–FR-50 |
| Non-Goals | ✅ | §5 — substantive |
| MVP Scope | ✅ | §6 — Early/Late V1 split with FR citations |
| Success Metrics | ❌ | **Missing** — no section at all |
| Open Questions | ✅ | §8 — 4 genuine + 1 effectively resolved |
| Assumptions Index | ✅ | §9 — present but thin (5 entries) |

### Findings

- **critical** — Success Metrics are absent from the spine — This is the only missing essential element. At this project tier (meaningful personal launch, solo author) the bar is low — but the section must exist. Without it, there is no retrospective anchor and no way to evaluate whether Late V1 features shipped at the right time. *Fix:* Add §10 "Success Metrics" (or §7a before NFRs). Three metrics suffice: one usage signal (daily active use), one technical signal (NFR-Perf validation), one quality signal (AI Issues surfaced per 10K words by end of Late V1).

---

## Mechanical notes

**Glossary drift:** None detected. All 16 defined terms are used with consistent capitalization and meaning throughout §4–§9 and in the addendum.

**FR ID continuity:** FR-1–FR-50 are contiguous and correctly grouped by feature section. §6 back-references are accurate (spot-checked FR-28–FR-30, FR-38–FR-43, FR-44–FR-50).

**Broken cross-references:**
- `DL-6` in addendum "Rejected Alternatives" — no decision log exists anywhere in the workspace. Orphaned.
- Product Brief path not linked from §0 — relative path should be `../../../../planning-artifacts/briefs/brief-Hymnal-2026-05-27/brief.md` (or use workspace-relative form).

**Assumptions Index roundtrip:** 5 entries in §9 for 50 FRs. FR-32 and FR-47 are the only in-body `[ASSUMPTION]` tags; both appear in §9. No mismatches. However, FR-44 (OS credential store portability on Linux) and FR-3 (Part detection heuristic) each contain implicit assumptions not surfaced in §9. Recommend adding 2–3 entries.

**OQ-4 resolution:** The resolved OQ is cleanly struck through with resolution text inline. This is an acceptable pattern and should be preserved as a record.
