# M008: Review Mode and Structured Analysis

**Gathered:** 2026-06-07
**Status:** Draft — awaiting M006 and M007 completion before planning
**Depends on:** M006, M007

## Project Description

M008 activates the disabled REVIEW tab (`ShellMode.Edit` in code) as a first-class editorial command center. It delivers the issue inbox (FR-49), inline issue indicators (FR-50), and extends the AI editorial query types (FR-47) beyond the original Proofing / Consistency / Line Editing modes with structured story-structure analysis templates: Three-Act Structure, Story Grid beat sheet, and Hero's Journey.

The hybrid architecture chosen in roadmap planning (center REVIEW tab for triage/analysis + per-mode quick chat from M007) means M008's center panel is the aggregation surface — it sees issues from all sources, runs whole-manuscript analyses, and shows review-queue status across chapters. Mode chats handle quick in-context questions; the Review tab handles deep structural reads and issue management.

## Why This Milestone

The original PRD PRD §4.8 (FR-47) planned "Proofing, Consistency, and Line Editing" structured modes in Late V1. The roadmap revamp adds Story Grid and act-structure templates as first-class deliverables based on the author's stated interest in evaluating the manuscript against standardized storytelling techniques.

Story Grid and Three-Act Structure evaluations are particularly valuable at the whole-book or Part scope — which is exactly the analytical view the Review center tab enables.

## User-Visible Outcome

### When this milestone is complete, the user can:

- Click REVIEW in the title bar and see the editorial command center
- View all AI-flagged issues in one inbox, filter by scope/type/state, click any line-level issue to jump there in the editor
- See a chapter queue of chapters in Reviewing status with open issue counts
- Click "Run Analysis → Story Grid" and receive a beat-sheet evaluation with findings as Issue records and a narrative report
- Click "Run Analysis → Three-Act Structure" for an act-boundary and pacing evaluation
- See colored margin indicators on lines with open issues in the editor; click a marker to open the issue in Review
- Select an issue in Review and see its full detail in the right panel

## Completion Class

- Contract complete: template prompt building and Issue[] parsing are unit-tested; issue inbox filtering is unit-tested; margin indicator line-binding is unit-tested
- Integration complete: analysis run → Issues stored → inbox shows → margin indicator renders → click navigates
- Operational complete: AI provider unavailable shows a graceful error in the analysis panel; no crash, no silent failure

## Architectural Decisions

### IAnalysisTemplate interface

**Decision:** Analysis templates implement `IAnalysisTemplate` with three methods: `BuildPrompt(scope, context)` → string, `ParseResponse(text)` → `(Issue[], narrativeReport)`, and metadata (`Name`, `Description`, `SupportedScopes`). Templates are registered in DI as `IEnumerable<IAnalysisTemplate>` and shown in a list in the Review panel.

**Rationale:** Extensible without modifying the Review ViewModel. Adding a new template = adding a new class + DI registration.

### JSON-mode responses for structured Issue parsing

**Decision:** Where the provider supports JSON mode, analysis templates request a JSON response conforming to the Issue schema. Where JSON mode is unavailable, a regex-based fallback parser is used. Partially parsed responses are accepted and flagged with a `parse_quality: partial` metadata field.

**Rationale:** Structured parsing is unreliable against free-text AI responses. JSON mode dramatically improves Issue[] extraction quality.

### ShellMode.Edit → Review

**Decision:** Rename the nav button label from "EDIT" to "REVIEW" and enable it. The `ShellMode.Edit` enum value is unchanged (too many binding references to change safely); the display label is "REVIEW". A `ReviewViewModel` is added to the center-panel dispatch switch.

**Rationale:** The enum value name is a code artifact; the user sees the nav button label. Renaming the enum would touch converters, tests, and AXAML; not worth it for a label change.

## Requirements Covered

- R010 (AI editorial assistance) — FR-46–50 implementation
- New R016 (structured story analysis — Story Grid, act structures)
