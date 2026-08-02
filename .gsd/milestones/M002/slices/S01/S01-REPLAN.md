# S01 Replan

**Milestone:** M002
**Slice:** S01
**Blocker Task:** T04
**Created:** 2026-05-31T00:26:41.324Z

## Blocker Description

All attempted verification commands for the slice fail during NuGet restore before compilation with `Value cannot be null. (Parameter 'path1')` from `NuGet.targets` on both `Hymnal.Core` and `Hymnal`, even after forcing SDK 10.0.100, deleting obj folders, and running with explicit Windows environment variables. This appears to be an environment/SDK restore-path blocker rather than a code regression.

## What Changed

Preserved completed tasks T01-T04 and added a follow-up verification task to isolate and resolve the restore-path blocker so build/test evidence can be produced in a clean environment.
