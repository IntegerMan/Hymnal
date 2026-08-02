---
id: T01
parent: S03
milestone: M004
key_files:
  - src/Hymnal.Core/Interfaces/IGitService.cs
  - src/Hymnal.Core/Interfaces/IProcessRunner.cs
  - src/Hymnal.Core/Models/GitCommandResult.cs
  - src/Hymnal.Core/Models/GitRepositoryStatus.cs
  - src/Hymnal.Core/Infrastructure/GitProcessRunner.cs
  - src/Hymnal.Core/Services/ProcessGitService.cs
  - tests/Hymnal.Core.Tests/Services/ProcessGitServiceTests.cs
key_decisions:
  - Git commands are run through an IProcessRunner seam and return GitCommandResult with raw stderr/exit-code diagnostics.
  - Repository commands use git -C {workspaceRoot} argument lists instead of setting a shell command string or invoking a shell.
duration: 
verification_result: mixed
completed_at: 2026-06-04T05:44:25.540Z
blocker_discovered: false
---

# T01: Added a structured Git service contract, direct process-runner seam, production Git process runner, ProcessGitService, and fake-runner tests covering repository status and commit/push workflows.

**Added a structured Git service contract, direct process-runner seam, production Git process runner, ProcessGitService, and fake-runner tests covering repository status and commit/push workflows.**

## What Happened

Implemented the Core Git integration seam requested by the task. Added GitCommandResult to carry executable, argument list, working directory, exit code, stdout, and raw stderr; added GitRepositoryStatus for toolbar-facing repository visibility, branch, and uncommitted count projection. Added IProcessRunner and GitProcessRunner using System.Diagnostics.Process with UseShellExecute=false, ArgumentList arguments, redirected stdout/stderr, and no shell invocation. Added IGitService and ProcessGitService for Git availability checks, git -C workspace repository detection, branch parsing, porcelain status line counting, stage-all plus commit, and stage-all plus commit plus push. Expected Git/process failures are returned as structured command results instead of being thrown, preserving raw stderr for future notifications. Added deterministic ProcessGitServiceTests with a fake runner to prove available/unavailable Git behavior, non-repo hiding, branch/count parsing, command ordering for commit and push, skip behavior after stage/commit failures, and raw stderr propagation.

## Verification

Ran the task-specific test filter for ProcessGitServiceTests through the Windows .NET SDK because gsd_exec's isolated bash environment could not find dotnet. The filtered test suite passed: 10 tests, 0 failures. Also retained the failed gsd_exec attempt as environment evidence showing /bin/bash: dotnet: command not found.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter FullyQualifiedName~ProcessGitServiceTests` | 127 | ❌ environment failure: gsd_exec bash could not find dotnet | 3303ms |
| 2 | `"/c/Program Files/dotnet/dotnet" test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter FullyQualifiedName~ProcessGitServiceTests` | 0 | ✅ pass: 10 ProcessGitServiceTests passed, 0 failed | 5951ms |

## Deviations

Used the harness shell's discovered Windows .NET SDK path for passing verification after the required gsd_exec verification command failed because dotnet was unavailable in its bash environment.

## Known Issues

Existing unrelated xUnit analyzer warnings in CorkboardViewModelTests appeared during the first filtered test build; they were not introduced or changed by this task.

## Files Created/Modified

- `src/Hymnal.Core/Interfaces/IGitService.cs`
- `src/Hymnal.Core/Interfaces/IProcessRunner.cs`
- `src/Hymnal.Core/Models/GitCommandResult.cs`
- `src/Hymnal.Core/Models/GitRepositoryStatus.cs`
- `src/Hymnal.Core/Infrastructure/GitProcessRunner.cs`
- `src/Hymnal.Core/Services/ProcessGitService.cs`
- `tests/Hymnal.Core.Tests/Services/ProcessGitServiceTests.cs`
