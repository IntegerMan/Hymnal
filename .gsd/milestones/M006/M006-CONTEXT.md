# M006: AI Platform and Shared Services

**Gathered:** 2026-06-07
**Status:** Draft — awaiting M005 execution start before planning
**Depends on:** M004

## Project Description

Hymnal is a cross-platform desktop manuscript editor. M006 builds the AI foundation layer that every subsequent AI surface depends on: OS-credential-backed provider configuration, the MEAI `IChatClient` integration, summary generation and storage, the issue store with DynamicData pipeline, and a reusable chat UI component.

M006 can begin in parallel with M005 (both depend only on M004). No M005 features are prerequisites for M006, but M007 (mode-scoped chat) depends on both.

## Why This Milestone

Without a shared AI layer, every mode (Research, Plan, Write, Manage, Review) would need its own credential wiring, provider client, issue storage, and chat component. That creates four or five maintenance surfaces for the same code. M006 ships the platform once; M007–M010 consume it.

## User-Visible Outcome

### When this milestone is complete, the user can:

- Open Settings and configure an AI provider endpoint + model name
- Enter an API key that is stored in the OS credential store (never on disk or in `.hymnal-data/`)
- Trigger summary generation for any chapter, Part, or book; see "Summary generated." confirmation; know summaries persist across sessions
- See AI features show "Configure an AI provider in Settings" when no provider is configured — not a crash, not a silent failure

The shared chat control is not surfaced in any primary workflow yet (that's M007). M006 delivers the wiring only.

## Completion Class

- Contract complete: unit tests cover `SummaryService`, `IssueStore`, `AiEditorialService` with fake `IChatClient`, and credential store round-trip on both platforms
- Integration complete: end-to-end summary generate → store → reload round-trip; issue create → persist → reload; provider config persists API key through restart
- Operational complete: `libsecret` unavailability on Linux produces a warning banner and disables AI features gracefully (no crash, no silent data loss)

## Architectural Decisions

### MEAI IChatClient as the abstraction boundary

**Decision:** `IAiEditorialService` wraps MEAI's `IChatClient`. No direct LiteLLM SDK dependency in `Hymnal.Core`. The MEAI abstraction supports LiteLLM-compatible endpoints as well as native MEAI-supported providers.

**Rationale:** `IChatClient` is the stable seam for testing (fake implementation) and for future provider switching without changing service code.

### API key never on disk

**Decision:** API key storage is `ICredentialStore` only. `AppSettingsStore` holds endpoint URL and model name; API key is not serialized to `settings.json` or any `.hymnal-data/` file.

**Rationale:** Manuscript repos are frequently committed to public Git repos. An accidental API key commit via `settings.json` would be a serious security incident.

### Shared AiChatViewModel is injectable

**Decision:** `AiChatViewModel` is a DI-registered singleton with scope state (CHAPTER / PART / BOOK) and session history per scope. Modes inject it directly — no per-mode chat ViewModel subclasses.

**Rationale:** Keeps the chat component DRY; scope switching is a property change, not a ViewModel swap.

## Open Questions

- OQ-1: Should Hymnal offer to add `.hymnal-data/summaries/` and `.hymnal-data/issues/` to `.gitignore`? Recommended: opt-in toggle in Settings with a default of "exclude from git" (both can contain sensitive manuscript content and grow large).
- `libsecret` on Linux: use `SecretService` NuGet package or raw D-Bus P/Invoke? Recommend `SecretService` NuGet for managed wrapper, with a P/Invoke fallback if the NuGet is unavailable.
- AI structured mode prompt templates: embedded resources in `Hymnal.Core` assembly (easiest, no user config) vs `.hymnal-data/prompts/` (user-overridable). Recommend embedded resources for V1; user overrides post-V1.

## Requirements Covered

- R010 (AI editorial assistance) — platform foundation
