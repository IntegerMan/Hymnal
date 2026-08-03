# M010: Market and Publishing

**Gathered:** 2026-06-07
**Status:** Draft — awaiting M005 completion and PRD addendum before planning
**Depends on:** M005

## Project Description

M010 gives the author a marketing activity tracker inside Hymnal (no spreadsheet, no separate project manager) and optionally connects to the LeanPub API for book status visibility and preview build triggering.

M005 is the dependency because stable chapter UUID identity is a foundation for future per-chapter marketing metadata (e.g., "Chapter 1 is in the sample excerpt"). LeanPub integration uses `ICredentialStore` from M006, but M006 is not a hard prerequisite for the activity tracker (S01–S02 can start independently).

## Why This Milestone

The author's primary tool goal (PRD SM-4) is to use Hymnal as the complete writing environment for *A Choir of Minds* including submission to LeanPub. A marketing activity tracker keeps launch planning alongside the manuscript. LeanPub API integration (S03–S04) closes the loop from writing → publishing without leaving Hymnal.

Note: LeanPub API integration reopens a PRD non-goal. A PRD addendum is a prerequisite for S03/S04 implementation.

## User-Visible Outcome

### When this milestone is complete, the user can:

- Track marketing tasks (e.g., "Write launch blog post", "Post to writing community", "Email newsletter") with channel, due date, and status
- See a simple dashboard: pending activities, upcoming by date, completion counts
- (Stretch) Enter a LeanPub API token and see book metadata (preview URL, build status) in the Market surface
- (Stretch) Trigger a LeanPub preview build and see the build status update

## Completion Class

- Contract complete: `MarketingActivityStore` round-trip and filtering logic are unit-tested
- Integration complete: activities persist across workspace reload; LeanPub API client returns structured data from a fake HTTP handler
- Operational complete: LeanPub API unavailable or bad token shows a graceful error; no crash

## Architectural Decisions

### Marketing data stays in .hymnal-data/

**Decision:** Marketing activities are stored in `.hymnal-data/market/activities.json`. This keeps them with the workspace and accessible to Git if the author tracks their publishing workflow in the same repo.

**Rationale:** Consistent with all other Hymnal metadata. The author can choose to gitignore this directory if preferred.

### LeanPub API token via ICredentialStore

**Decision:** The LeanPub API token is stored via `ICredentialStore` under the service name `hymnal-leanpub`. The LeanPub book slug and endpoint URL are in `AppSettingsStore` (not secret).

**Rationale:** Same pattern as AI provider API key. Secrets never on disk.

## Nav Decision (deferred)

Navigation placement (MARKET title-bar tab vs nested under MANAGE) is deferred to the M010 planning phase.

## PRD Addendum Required

Before S03/S04 implementation, a PRD addendum must update §5 and add FR-51+ for:
- Marketing activity tracker (R017)
- LeanPub read-only metadata sync (R018, optional tier)
- LeanPub preview build trigger (R018 stretch)

## Requirements Covered

- New R017 (marketing activity tracker)
- New R018 (LeanPub integration — optional tier)
