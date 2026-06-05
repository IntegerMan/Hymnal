using Hymnal.Core.Common;
using Hymnal.Core.Models;

namespace Hymnal.Core.Interfaces;

/// <summary>
/// Git operations used by the desktop shell. Implementations return structured command diagnostics
/// and do not throw for expected Git/process failures.
/// </summary>
public interface IGitService
{
    Task<Result<GitCommandResult>> CheckGitAvailableAsync(CancellationToken cancellationToken = default);

    Task<Result<GitRepositoryStatus>> GetRepositoryStatusAsync(
        string workspaceRoot,
        bool includeRemoteState = false,
        CancellationToken cancellationToken = default);

    Task<Result<GitCommandResult>> FetchAsync(
        string workspaceRoot,
        CancellationToken cancellationToken = default);

    Task<Result<GitCommandResult>> PullAsync(
        string workspaceRoot,
        CancellationToken cancellationToken = default);

    Task<Result<GitCommandResult>> PushAsync(
        string workspaceRoot,
        CancellationToken cancellationToken = default);

    Task<Result<GitCommandResult>> StageAllAndCommitAsync(
        string workspaceRoot,
        string commitMessage,
        CancellationToken cancellationToken = default);

    Task<Result<GitCommandResult>> StageAllCommitAndPushAsync(
        string workspaceRoot,
        string commitMessage,
        CancellationToken cancellationToken = default);

    Task<Result<GitCommandResult>> StageAllCommitPullAndPushAsync(
        string workspaceRoot,
        string commitMessage,
        CancellationToken cancellationToken = default);
}
