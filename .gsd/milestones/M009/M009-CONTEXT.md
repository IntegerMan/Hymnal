# M009: Brainstorm and Mind Map

**Gathered:** 2026-06-07
**Status:** Draft — awaiting M006 completion before planning
**Depends on:** M006

## Project Description

M009 adds a visual ideation canvas to Hymnal — a mind-map surface persisted under `.hymnal-data/brainstorm/` that stays connected to the manuscript. Authors can build character webs, plot outlines, and research thread maps, link nodes to chapters or supplemental docs, and export maps as Markdown outlines.

The AI stretch goal (S05: AI-assisted node expansion) uses `AiChatViewModel` from M006, making M006 the only hard prerequisite. M005 is not required (mind maps don't depend on manuscript structure), though having stable UUIDs from M005 makes chapter linking more robust.

## Why This Milestone

Authors brainstorm before, during, and after writing. A mind-map surface inside Hymnal keeps ideation output in the project folder and directly linkable to the chapters it informs — no context switch to a separate app like MindMeister, Miro, or XMind.

## User-Visible Outcome

### When this milestone is complete, the user can:

- Create a mind map named "Character Web" from a mind-map surface in Hymnal
- Add nodes, draw edges, and rearrange the canvas freely
- Link a node to Chapter 3 (by UUID); double-click the link to open Chapter 3 in the editor
- Close Hymnal and reopen; the mind map is exactly as they left it
- Export the mind map as a Markdown outline to `.hymnal-data/docs/brainstorm-character-web.md`

## Completion Class

- Contract complete: `MindMapStore` round-trip and export formatting are unit-tested
- Integration complete: chapter link resolves to current chapter path through `ChapterRegistryService`; export writes appear in supplemental docs tree
- Operational complete: canvas handles 100+ nodes without perceptible lag

## Architectural Decisions

### Custom Avalonia Canvas vs third-party

**Decision:** Evaluate available Avalonia graph/diagram controls (e.g., `Avalonia.Diagram`, `NodeEditor.Avalonia`) before committing to a fully custom renderer. If a maintained Avalonia-compatible library exists, use it. If not, build a minimal custom Canvas following the same DrawingContext pattern as the Gantt renderer.

**Rationale:** Custom canvas for mind maps is high-effort. The Gantt renderer precedent shows it's achievable, but a maintained library would save significant implementation time.

### Persistence format

**Decision:** Each mind map is a separate JSON file under `.hymnal-data/brainstorm/{slug}.json` with `schemaVersion`. An index file `.hymnal-data/brainstorm/index.json` lists all maps with metadata. Node positions are stored as floating-point `x, y` pairs relative to a fixed origin.

**Rationale:** One-file-per-map keeps each map independently readable and avoids a monolithic file that grows with every edit. `schemaVersion` enables future migration if the node schema changes.

## Nav Decision (deferred)

The navigation placement question — BRAINSTORM title-bar tab vs nested under PLAN vs accessible from PLAN sidebar — is explicitly deferred to the M009 planning phase. The plan file records this as a required decision before S01 can start.

## Requirements Covered

- New R015 (brainstorm / mind-map persistence)
