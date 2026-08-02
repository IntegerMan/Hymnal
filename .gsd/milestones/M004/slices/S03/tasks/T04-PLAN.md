---
estimated_steps: 1
estimated_files: 5
skills_used: []
---

# T04: Verify real local-repo commit and push workflow

Executor skills_used frontmatter: tdd, verify-before-complete. Why: fake process tests cannot prove the system-Git contract works against an actual repository, and M004 success criteria explicitly require local-repo Git workflow evidence. Do: add focused integration tests that create a temp workspace repository with `Book.txt` and `manuscript/`, configure local test Git identity, make an initial commit, create a bare temp remote, and exercise `ProcessGitService` through the production `GitProcessRunner`. The test should modify a manuscript or docs file, assert nonzero uncommitted count and current branch, run Commit only and assert count clears plus a commit exists with the Hymnal message, modify again, run Commit & Push and assert the bare remote receives the commit. Also include a failure-path integration or fake-runner assertion that a push failure preserves raw stderr into the ViewModel notification if not already fully covered. Gate real-Git tests with an explicit availability check that skips only when system Git is truly absent in the environment; otherwise failures should be real failures. Finish with an app build command so AXAML and DI composition are compiled. Done when the local-repo tests pass, the app builds, and the slice verification commands listed in the slice plan have current evidence.

## Inputs

- `src/Hymnal.Core/Interfaces/IGitService.cs`
- `src/Hymnal.Core/Infrastructure/GitProcessRunner.cs`
- `src/Hymnal.Core/Services/ProcessGitService.cs`
- `src/Hymnal/ViewModels/GitPanelViewModel.cs`
- `tests/Hymnal.Core.Tests/Services/ProcessGitServiceTests.cs`

## Expected Output

- `tests/Hymnal.Core.Tests/Services/ProcessGitServiceLocalRepoTests.cs`
- `tests/Hymnal.Core.Tests/ViewModels/GitPanelViewModelTests.cs`
- `src/Hymnal.Core/Services/ProcessGitService.cs`
- `src/Hymnal.Core/Infrastructure/GitProcessRunner.cs`
- `src/Hymnal/ViewModels/GitPanelViewModel.cs`

## Verification

dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter FullyQualifiedName~ProcessGitServiceLocalRepoTests

## Observability Impact

Produces operational proof artifacts in test output: temp-repo status, commit history assertions, push result assertions, and raw stderr notification checks. This is the final diagnostic guardrail for the subprocess/watcher boundary.
