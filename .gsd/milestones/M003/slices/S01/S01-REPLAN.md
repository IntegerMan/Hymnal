# S01 Replan

**Milestone:** M003
**Slice:** S01
**Blocker Task:** T02
**Created:** 2026-06-01T18:32:52.432Z

## Blocker Description

Reviewer found two blocker-level gaps in the shipped Gantt foundation: (1) workspaces with no valid phase dates render an empty-state placeholder instead of muted chapter rows/gaps, and (2) Gantt rows do not refresh when existing ChapterViewModel phase data changes after in-session edits.

## What Changed

Add follow-up tasks to make the read-only Gantt surface robust for missing-date workspaces and to keep the projection in sync when phase metadata is edited without changing the workspace node collection.
