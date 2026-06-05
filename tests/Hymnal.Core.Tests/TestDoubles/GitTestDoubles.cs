using Hymnal.Core.Common;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models;

namespace Hymnal.Core.Tests.TestDoubles;

internal static class GitTestDoubles
{
    public static GitRepositoryStatus VisibleStatus(
        string branch,
        int changeCount,
        int behindRemoteCount = 0,
        int aheadRemoteCount = 0,
        bool hasMergeConflict = false,
        IReadOnlyList<string>? conflictedFiles = null)
    {
        var probe = new GitCommandResult(
            "git",
            new[] { "-C", "workspace", "rev-parse", "--is-inside-work-tree" },
            "workspace",
            0,
            "true\n",
            string.Empty);
        var branchResult = new GitCommandResult(
            "git",
            new[] { "-C", "workspace", "branch", "--show-current" },
            "workspace",
            0,
            branch + "\n",
            string.Empty);
        var porcelain = changeCount == 0
            ? string.Empty
            : string.Join(Environment.NewLine, Enumerable.Range(0, changeCount).Select(i => $" M file{i}.md")) + Environment.NewLine;
        var statusResult = new GitCommandResult(
            "git",
            new[] { "-C", "workspace", "status", "--porcelain" },
            "workspace",
            0,
            porcelain,
            string.Empty);
        var changedFiles = Enumerable.Range(0, changeCount)
            .Select(i => new GitChangedFile($"file{i}.md", "M"))
            .ToList();

        return new GitRepositoryStatus(
            true,
            true,
            branch,
            changeCount,
            changedFiles,
            behindRemoteCount,
            aheadRemoteCount,
            hasMergeConflict,
            conflictedFiles ?? Array.Empty<string>(),
            probe,
            branchResult,
            statusResult);
    }
}

internal class NoOpGitService : IGitService
{
    public virtual Task<Result<GitCommandResult>> CheckGitAvailableAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Result<GitCommandResult>.Ok(new GitCommandResult("git", new[] { "--version" }, null, 0, "git version\n", string.Empty)));

    public virtual Task<Result<GitRepositoryStatus>> GetRepositoryStatusAsync(
        string workspaceRoot,
        bool includeRemoteState = false,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Result<GitRepositoryStatus>.Ok(GitRepositoryStatus.Hidden(
            GitCommandResult.Failure("git", Array.Empty<string>(), workspaceRoot, string.Empty),
            isGitAvailable: false)));

    public virtual Task<Result<GitCommandResult>> FetchAsync(string workspaceRoot, CancellationToken cancellationToken = default)
        => Task.FromResult(Result<GitCommandResult>.Ok(GitCommandResult.Failure("git", Array.Empty<string>(), workspaceRoot, string.Empty)));

    public virtual Task<Result<GitCommandResult>> PullAsync(string workspaceRoot, CancellationToken cancellationToken = default)
        => Task.FromResult(Result<GitCommandResult>.Ok(GitCommandResult.Failure("git", Array.Empty<string>(), workspaceRoot, string.Empty)));

    public virtual Task<Result<GitCommandResult>> PushAsync(string workspaceRoot, CancellationToken cancellationToken = default)
        => Task.FromResult(Result<GitCommandResult>.Ok(GitCommandResult.Failure("git", Array.Empty<string>(), workspaceRoot, string.Empty)));

    public virtual Task<Result<GitCommandResult>> StageAllAndCommitAsync(string workspaceRoot, string commitMessage, CancellationToken cancellationToken = default)
        => Task.FromResult(Result<GitCommandResult>.Ok(GitCommandResult.Failure("git", Array.Empty<string>(), workspaceRoot, string.Empty)));

    public virtual Task<Result<GitCommandResult>> StageAllCommitAndPushAsync(string workspaceRoot, string commitMessage, CancellationToken cancellationToken = default)
        => Task.FromResult(Result<GitCommandResult>.Ok(GitCommandResult.Failure("git", Array.Empty<string>(), workspaceRoot, string.Empty)));

    public virtual Task<Result<GitCommandResult>> StageAllCommitPullAndPushAsync(string workspaceRoot, string commitMessage, CancellationToken cancellationToken = default)
        => Task.FromResult(Result<GitCommandResult>.Ok(GitCommandResult.Failure("git", Array.Empty<string>(), workspaceRoot, string.Empty)));
}
