---
title: "Reconciliation: Brief Addendum → PRD"
created: 2026-05-27
---

# Reconciliation: Brief Addendum → PRD

## Input: Brief Addendum (brief-Hymnal-2026-05-27/addendum.md)

---

### Gaps found

- **[high]** Gantt target proximity indicator absent — The brief addendum states explicitly: *"Progress indicators in the Gantt and sidebar should reflect target proximity when targets are set."* The PRD carries the sidebar half (FR-19: "a visual proximity indicator… is shown in the sidebar"), but the Gantt view requirements (FR-20–FR-27) contain no FR for word count display or target proximity on the Gantt rows or Part rollup rows. This should land as an additional consequence in FR-19 or as a dedicated FR in §4.4 Gantt Phase View.

- **[medium]** In-editor top-of-chapter/part issue indicators weakened to sidebar-only — The brief addendum specifies a distinct in-editor visual treatment: *"Chapter-level issues: summary indicator at the top of the chapter"* and *"Part-level issues: summary indicator at the top of the part scope."* This describes indicator placement within the editor buffer itself (at the top of the chapter file, and at the top of the part scope in context). FR-50 maps chapter- and part-level issues only to "summary badges in the sidebar at the appropriate scope" — the in-editor positional treatment is silently dropped. If the intent was sidebar-only, that is a deliberate narrowing that should be noted; if the intent was editor placement, it is missing from FR-50.

- **[medium]** Issues panel described as scope-organized navigation; FR-49 specifies flat filtering — The brief addendum describes the issues panel as *"organized by scope (book → part → chapter)"*, implying a hierarchical tree-navigation model. FR-49 specifies the panel is "filterable by scope (book / part / chapter), type, and state" — which describes filter controls on a flat list. These are meaningfully different UX shapes. The distinction should be decided explicitly before the UX spec is written.

- **[low]** Workflow north star not anchored as a requirement — The brief addendum contains a stated design conviction that has no equivalent FR or NFR: *"No spreadsheet, no external project tracker, no context switch to another writing tool."* This appears only as vision prose in §1 of the PRD ("provides a unified environment…") and as flavor in UJ-1. It should be cited explicitly in the non-functional requirements (or as a UX constraint) so it can be evaluated against feature decisions during Late V1 (e.g., whether AI results that open in a browser tab violate it).

- **[low]** AI Summary regeneration scope and storage option not fully surfaced — The brief addendum specifies summaries *"should be regenerable on demand and stored as sidecar files alongside the manuscript (or in a project metadata folder)."* FR-45 states they are regenerable and stored in `.hymnal-data/summaries/` — the project metadata path. The sidecar-alongside-manuscript option is not documented as considered and rejected; it is simply absent. This is low-risk since the PRD addendum's `.hymnal-data/` schema is authoritative, but the rationale for choosing metadata folder over sidecar is not recorded anywhere in the PRD artifacts.

---

### Well-covered

The AI Issues model (field table: `id`, `type`, `description`, `state`, `created_date`, `location`) translated fully and accurately into FR-48, with the enum values for `type` and `state` carried verbatim. Word count target nuances — live observation, optional at all levels, range support at book level, single-or-range at chapter/part — are faithfully reflected across FR-16, FR-18, and the §3 Glossary "Target" entry. Rejected alternatives (rendered preview, multi-book library, collaboration) are captured in both §5 Non-Goals and the PRD addendum's Rejected Alternatives section, with the Git-scope change explicitly documented as a deliberate reopening.
