# M007: Mode-Scoped AI Chat

**Gathered:** 2026-06-07
**Status:** Draft — awaiting M005 and M006 completion before planning
**Depends on:** M005, M006

## Project Description

M007 connects the shared AI platform (M006) to each authoring mode's surface. Rather than a single generic AI panel, each mode bundles the most relevant context for the work happening in that mode and presents it alongside the author's primary view.

The `ShellMode` enum already reserves slots for Research, Plan, Write, Manage. Each mode's right column (or right-rail slot for Write) will host the shared `AiChatViewModel` pre-configured with a mode-appropriate context bundle.

## Why This Milestone

The author's experience goal is "no context switch to an external AI tool." M007 makes that real for each authoring surface:
- **Research:** ask questions about the supplemental doc you're reading
- **Plan:** ask about manuscript structure and chapter balance from the corkboard view
- **Write:** quick editorial questions while drafting, grounded in the chapter's summary
- **Manage:** ask about schedule gaps or status distribution while reviewing the Gantt

## User-Visible Outcome

### When this milestone is complete, the user can:

- Switch to Research mode, open a supplemental doc, click the AI chat area, and ask questions about the doc's content
- Switch to Plan mode, see the manuscript overview, and ask "which chapters are falling behind on word count?"
- While writing, open the right panel to an AI chat slot and ask "does this chapter feel consistent with Part 1's tone?" — the active chapter summary is pre-loaded as context
- Switch to Manage mode and ask "which phases are overdue?" with phase date data as context

## Completion Class

- Contract complete: context builders for each mode are unit-tested against representative ViewModel state
- Integration complete: switching modes does not bleed conversation history; each scope (CHAPTER/PART/BOOK) maintains independent history across mode switches
- Operational complete: with no provider configured, all mode chats show the "Configure an AI provider" prompt without attempting a network call

## Architectural Decisions

### One AiChatViewModel, many context builders

**Decision:** Each mode injects the singleton `AiChatViewModel` but supplies a `IContextBuilder` implementation that assembles the mode-specific prompt prefix. Context builders are registered per mode in DI.

**Rationale:** Keeps the chat UI component DRY. Mode-specific logic is isolated to the context builder, not duplicated in each mode's ViewModel.

### Context token budget

**Decision:** Context builders cap output at a configurable token limit (default 4,000 tokens) using truncation with a "... [truncated]" marker. The limit is a setting in AppSettingsStore.

**Rationale:** Prevents provider errors from oversized context bundles, especially for supplemental docs and corkboard overviews on large manuscripts.

## Requirements Covered

- R010 (AI editorial assistance) — mode chat surfaces
