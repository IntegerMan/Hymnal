---
id: T04
parent: S03
milestone: M004
key_files:
  - tests/Hymnal.Core.Tests/Services/ProcessGitServiceLocalRepoTests.cs
  - tests/Hymnal.Core.Tests/ViewModels/GitPanelViewModelTests.cs
key_decisions:
  - Real Git integration tests set upstream with `git push -u origin main` before exercising the plain push path.
  - Real Git commit assertions use explicit Hymnal timestamped messages because ProcessGitService rejects blank commit messages.
  - Push-failure coverage now asserts the ViewModel notification path preserves raw stderr, not just the service result.
duration: 
verification_result: passed
completed_at: 2026-06-04T14:02:44.296Z
blocker_discovered: false
---

# T04: Proved the Git toolbar’s real local-repo commit/push workflow with a temp Git repo and bare remote, and added push-failure raw-stderr UI coverage.

**Proved the Git toolbar’s real local-repo commit/push workflow with a temp Git repo and bare remote, and added push-failure raw-stderr UI coverage.**

## What Happened

Added a focused integration test that creates a temporary workspace repo with Book.txt and manuscript content, configures local Git identity, seeds an initial commit, wires a bare temp remote, and then exercises ProcessGitService through the production GitProcessRunner against a real repository. The test verifies branch detection, dirty-file counting, commit-only cleanup, commit message persistence with the Hymnal timestamped format, and commit-and-push propagation to the bare remote. I also extended the GitPanelViewModel tests with a push-failure path that asserts raw stderr is surfaced through notifications and the refresh cycle still runs afterward. The integration test uses an explicit availability check and short-circuits when system Git is absent; on this machine Git was present and the full real-repo flow executed successfully.

## Verification

Verified the requested real local-repo flow with `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter FullyQualifiedName~ProcessGitServiceLocalRepoTests`, which passed against a real temp repo and bare remote, and confirmed the app shell compiles with `dotnet build src/Hymnal/Hymnal.csproj`.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter FullyQualifiedName~ProcessGitServiceLocalRepoTests` | 0 | ✅ pass | 3000ms |
| 2 | `dotnet build src/Hymnal/Hymnal.csproj` | 0 | ✅ pass | 1900ms |

## Deviations

Used a short-circuit return for missing Git instead of a formal xUnit runtime skip because the installed xUnit 2 package surface in this project does not expose a runtime skip constructor. The integration test also supplies explicit Hymnal-style commit messages because the service rejects blank commit messages by design.

## Known Issues

None discovered in the verified flow.

## Files Created/Modified

- `tests/Hymnal.Core.Tests/Services/ProcessGitServiceLocalRepoTests.cs`
- `tests/Hymnal.Core.Tests/ViewModels/GitPanelViewModelTests.cs`
