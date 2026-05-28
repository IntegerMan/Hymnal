---
title: "Product Brief: Hymnal"
status: draft
created: 2026-05-27
updated: 2026-05-27
---

# Product Brief: Hymnal

## Executive Summary

Hymnal is a cross-platform .NET desktop application for solo authors who write and self-publish books using LeanPub's Markua format. It opens a manuscript folder as a workspace and gives the author a single, coherent environment for writing prose, tracking chapter and book progress, and — as the work matures — interrogating the manuscript with AI editorial assistance.

The core insight is that Markua authors already have a filesystem-native, Git-friendly project structure; they do not need their work moved into a proprietary database. Hymnal meets authors where they are: it reads `Book.txt`, respects the folder layout, and writes only standard `.md` files back. The authoring experience is deliberately low-ceremony — a clean syntax-highlighted editor, not a word processor — and the project management layer sits alongside it without getting in the way.

Hymnal is MIT-licensed, open source, and built by its primary user writing a real novel — *A Choir of Minds* — as the application is developed.

## The Problem

Authors writing for LeanPub in Markua format face a tooling gap. Their manuscripts live in plain files, organized by part and chapter, driven by a `Book.txt` manifest. That structure is clean and portable, but it offers nothing in the way of project management: no chapter status, no word-count roll-ups, no visibility into how far along each chapter is in its lifecycle, and no way to see across the whole book at a glance.

The typical workaround is a patchwork of tools: a general-purpose text editor for writing, a spreadsheet for tracking status and word counts, and calendar reminders or handwritten notes for phase scheduling. Context switches between these tools interrupt the writing flow. Progress visibility is manual, stale, and fragile.

Full-featured writing tools like Scrivener exist but require importing the manuscript into a proprietary format — abandoning the clean Markua/Git workflow entirely. VS Code extensions can add project-management features, but a VS Code extension is not an application a non-technical author can install and own.

## The Solution

Hymnal opens any Markua manuscript folder and immediately understands its structure: parts, chapters, front matter, back matter, ordered exactly as `Book.txt` defines. From there it provides three integrated layers:

**Writing.** A focused Markdown editor with Markua-aware syntax highlighting — bold, headers, and standard Markdown elements are colorized, and LeanPub-specific Markua constructs (part markers, blurb shortcodes, `{sample: true}`, and similar) are highlighted and validated inline. No toolbars, no rendered preview panel — just the text, with enough visual signal to write confidently in Markua.

**Project management.** Each chapter carries a status (Outlining → Drafting → Editing → Polishing → Reviewing → Done) and a word count tracked live from the file. Chapters are grouped by part and can be reordered or excluded — changes that write back to `Book.txt`. Word count is visible per chapter, per part, and for the full book, with optional range targets at any level.

**Progress visualization.** A modified Gantt view shows one row per chapter with distinct colored phase boxes — one per lifecycle phase (draft, edit, polish, review) — each with manual start and end dates and a progress indicator. Setting a chapter's status can pre-populate the relevant date, but all dates are directly editable. This gives the author a single-screen picture of where the whole manuscript stands in its lifecycle.

**AI editorial assistance (late V1).** Once the core writing and tracking experience is solid, Hymnal adds a configurable AI layer. The application generates and stores chapter, part, and book summaries; these summaries serve as lightweight context for an in-app chat interface where the author can ask editorial questions — readability, grammar, internal inconsistency, developmental concerns, line-level notes — against a single chapter or the full book.

AI findings surface as a structured issues list alongside the chat output. Each issue carries a type (readability, grammar, inconsistency, developmental, line editing, etc.), a description, a created date, a state (open / resolved / dismissed), and an optional location at any granularity — part, chapter, paragraph, or line. Issues are navigable by scope. Within the editor, line-level issues appear as margin indicators on the relevant lines; chapter- and part-level issues surface as summary indicators at the top of their scope. The AI provider is configurable (LiteLLM, Microsoft Extensions for AI / Agent Framework, or similar) so the author brings their own key and service.

## Who This Serves

**Primary:** The solo author writing and self-publishing in Markua format who wants to stay in plaintext and Git, but needs project-management visibility and a writing environment that understands the format. The initial and most demanding user is the author of Hymnal itself, writing *A Choir of Minds*.

**Secondary:** Other solo Leanpub authors who are comfortable with a developer-adjacent workflow (folder-as-project, Markdown, Git) but are not themselves developers. The MIT license and open-source posture means this audience can adopt, fork, or contribute without barriers.

Co-authors and writing teams are explicitly out of scope for V1.

## Scope

**In for V1 — core (early):**
- Open a folder as a workspace; parse `Book.txt` to build the chapter/part tree
- Navigate chapters and parts in a sidebar
- Low-ceremony Markdown editor with Markua syntax highlighting and inline validation
- Chapter status lifecycle with manual tracking
- Status changes optionally pre-populate phase dates (always editable)
- Live word count per chapter, per part, and per book
- Optional word count targets at chapter, part, and book level
- Chapter reordering and exclusion (writes back to `Book.txt`)
- Modified Gantt view: per-chapter phase timeline with colored boxes, manual dates, progress

**In for V1 — core (late):**
- Configurable AI provider (LiteLLM / MEAI / Microsoft Agent Framework)
- Auto-generated and stored summaries at chapter, part, and book level
- In-app AI chat with summary context for editorial queries
- Tracked issues list from AI analysis, organized by book / part / chapter

**Out of scope for V1:**
- Rendered Markdown preview panel
- Multi-book project management
- Collaboration or multi-author support
- Built-in Git operations (the author uses Git externally)
- LeanPub publishing / API integration
- Mobile or web versions

## Success Criteria

- The author uses Hymnal as their primary writing and project-management environment while writing *A Choir of Minds* — no spreadsheet supplement required
- Chapter status, word count, and phase timeline are accurate and trustworthy at a glance without manual maintenance overhead
- The editor does not get in the way: writing in Hymnal feels equivalent to writing in a good plaintext editor, with Markua validation as a bonus
- The complete authoring workflow — writing, tracking, AI review, and Git commit/push — requires no context switch to another application
- Late V1 AI features reduce the friction of getting an editorial read on a chapter without leaving the application

## Vision

Hymnal is, first and foremost, a personal tool built to serve one author writing one book — and then the next. Its open-source MIT license means other Markua authors can adopt or adapt it without friction, but the product is not trying to be a platform or a market. The near-term roadmap follows the author's own needs: richer AI workflows (character consistency, timeline analysis), multi-book support when the first book ships, and deeper Markua tooling as the format evolves. The measure of success is simple: the author finishes the book using Hymnal, and the tool is better for the next book because of what was learned writing the first.
