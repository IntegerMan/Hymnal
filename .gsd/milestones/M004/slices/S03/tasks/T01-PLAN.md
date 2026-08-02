---
estimated_steps: 1
estimated_files: 7
skills_used: []
---

# T01: Implement Git service contract and process-runner seam

Executor skills_used frontmatter: tdd, verify-before-complete. Why: R008 depends on system Git, but ViewModels must not shell out directly and tests need deterministic stdout/stderr/exit-code behavior. Do: add `IGitService` to Core interfaces and small result/status models such as `GitCommandResult` and `GitRepositoryStatus`; add an `IProcessRunner` seam plus a production `GitProcessRunner` using `System.Diagnostics.Process` without invoking a shell; implement `ProcessGitService` methods for Git availability, repository detection at a workspace root, branch/status counting via porcelain output, stage-all + commit, and stage-all + commit + push. The service must use `git -C {workspaceRoot}` for repository commands, never throw for expected Git failures, return structured stdout/stderr/exit code, and preserve raw stderr. Count uncommitted changes from `git status --porcelain` lines so staged, unstaged, untracked, docs, and manuscript changes all count. Done when fake-runner tests prove available/unavailable Git, non-repo hiding input, branch/count parsing, commit command order, push command order, and raw stderr propagation.

## Inputs

- `src/Hymnal.Core/Common/Result.cs`
- `src/Hymnal.Core/Interfaces/INotificationService.cs`
- `src/Hymnal.Core/Infrastructure/MetadataStore.cs`
- `tests/Hymnal.Core.Tests/Services/SupplementalDocsServiceTests.cs`

## Expected Output

- `src/Hymnal.Core/Interfaces/IGitService.cs`
- `src/Hymnal.Core/Interfaces/IProcessRunner.cs`
- `src/Hymnal.Core/Models/GitCommandResult.cs`
- `src/Hymnal.Core/Models/GitRepositoryStatus.cs`
- `src/Hymnal.Core/Infrastructure/GitProcessRunner.cs`
- `src/Hymnal.Core/Services/ProcessGitService.cs`
- `tests/Hymnal.Core.Tests/Services/ProcessGitServiceTests.cs`

## Verification

dotnet test tests/Hymnal.Core.Tests/Hymnal.Core.Tests.csproj --filter FullyQualifiedName~ProcessGitServiceTests

## Observability Impact

Introduces the durable diagnostic payload for every Git operation: command context, exit code, stdout, and raw stderr. This is the foundation for later UI notifications and testable failure localization.
