# Hymnal

## What This Is

Hymnal is a cross-platform .NET 10 desktop writing application for a solo author (Matt Eland, writing *A Choir of Minds*) who works in a LeanPub Markua/Git workflow. It opens a manuscript folder, parses `Book.txt` as the authoritative chapter manifest, and provides an integrated environment for prose writing, chapter lifecycle tracking, progress visualization, supplemental project docs, in-app Git workflow, and AI-assisted editorial feedback. All Hymnal metadata lives in `.hymnal-data/` at the workspace root. Hymnal writes back to `Book.txt` only on explicit user actions.

## Core Value

The author can open Hymnal, write a chapter, track its status and word count, review progress in Gantt and corkboard views, manage project reference docs, and commit progress to Git — all without leaving the app.

## Project Shape

- **Complexity:** complex
- **Why:** Custom Gantt renderer, custom Corkboard card layout, reactive word-count pipeline across three view levels, two-project compile-enforced MVVM boundary, platform-conditional credential store, AI provider abstraction — all in a single-author desktop app with a hard June 2026 deadline.

## Current State

Repository is active and implemented — this is no longer a planning-only scaffold.

- M001–M004 are delivered and validated. M004 (Corkboard, Supplemental Docs, and Git Panel) completed on 2026-06-18 with milestone validation plus direct desktop confirmation for the native Avalonia UAT gap.
- The current working tree contains app code under `src/`, tests under `tests/`, and milestone artifacts under `.gsd/`.
- Closeout verification for native .NET work should prefer slice/task evidence and Windows SDK test/build artifacts when the WSL-backed `gsd_exec` lane cannot run `dotnet` reliably.

## Architecture / Key Patterns

- .NET 10, Avalonia UI 12.0, ReactiveUI MVVM, AvaloniaEdit, DynamicData
- Two-project solution: `src/Hymnal/` (UI layer) + `src/Hymnal.Core/` (pure .NET, zero Avalonia reference)
- `Result<T>` for all async service returns; atomic writes (write-temp-then-rename) everywhere
- `SourceCache<T, TKey>` (DynamicData) for manuscript chapters and issues collections
- `ICredentialStore` platform-conditional: `WindowsCredentialStore` / `LinuxSecretServiceStore`
- `.hymnal-data/` JSON files with `schemaVersion` on every file; camelCase properties, ISO 8601 dates
- `INotificationService` banners (error/info/success); no modal dialogs for non-destructive errors
- `IGitService` → `ProcessGitService` (system `git` binary via PATH, no bundled libgit2)
- `IAiEditorialService` → `IChatClient` (MEAI) abstraction; LiteLLM or native MEAI endpoint
- Synthwave dark theme: purple primary, yellow/pink/orange accents, WCAG AA contrast throughout
- In ReactiveUI view models, avoid self-referential OAPH initialization chains for live UI state; Chapter Info target/proximity indicators are authored as plain backing fields fed from live `ChapterViewModel` subscriptions

## Capability Contract

See `.gsd/REQUIREMENTS.md` for the explicit capability contract, requirement status, and coverage mapping.

## Milestone Sequence

- [x] M001: Scaffold, Workspace, and Editor — Solution scaffold + workspace/Book.txt parsing + sidebar tree + Markua editor with save/load
- [x] M002: Status Tracking and Word Count — Chapter status lifecycle, phase date pre-fill, live word count, targets, proximity indicators; status dots in sidebar
- [x] M003: Gantt View and Project Management — Phase timeline renderer, inline date editing, Part rollup rows, time axis, progress fill
- [x] M004: Corkboard, Supplemental Docs, and Git Panel — Early V1 corkboard cards, supplemental docs sidebar, Git stage-all/commit/push panel
- [ ] M005: Manuscript Structure — Sidebar chapter management, corkboard drag-reorder and structural editing, Gantt drag-reorder (absorbs M007 sidebar work)
- [ ] M006: AI Platform and Shared Services — Credential store, provider config, MEAI IChatClient, summaries, issue store, shared chat UI component
- [ ] M007: Mode-Scoped AI Chat — Per-mode contextual AI in Research, Plan, Write, and Manage surfaces
- [ ] M008: Review Mode and Structured Analysis — REVIEW center tab (activate ShellMode.Edit), issue inbox, structured story analysis (Story Grid, act structures), review reports
- [ ] M009: Brainstorm and Mind Map — Visual ideation canvas persisted under `.hymnal-data/brainstorm/`
- [ ] M010: Market and Publishing — Marketing activity tracker, optional LeanPub API integration
