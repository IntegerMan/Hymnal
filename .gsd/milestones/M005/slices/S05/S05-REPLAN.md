# S05 Replan

**Milestone:** M005
**Slice:** S05
**Blocker Task:** T02
**Created:** 2026-06-18T17:12:33.598Z

## Blocker Description

Closeout review of the cross-Part drop path uncovered two blockers after task completion: the current S05 integration harness no longer compiles because TestContext.Dispose() calls Workspace.Dispose() even though WorkspaceViewModel is not disposable, and the shipped cross-Part move logic still has unverified robustness gaps — nested Part destinations can be truncated when deriving replacement paths, and a post-commit exclusion-manifest failure can leave disk state changed while DropCardAsync reports failure without reloading.

## What Changed

Reopened T04 and extended the slice plan with hardening work: first restore the broken corkboard integration harness so focused verification can compile again, then add a new task to fix nested-Part replacement-path derivation and post-commit manifest-failure recovery in the cross-Part move path with automated coverage.
