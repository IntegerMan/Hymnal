using Hymnal.Core.Common;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models;

namespace Hymnal.Core.Services;

/// <summary>
/// Git service backed by an injected process runner. All repository commands use
/// <c>git -C {workspaceRoot}</c> and preserve raw stdout/stderr for diagnostics.
/// </summary>
public sealed class ProcessGitService : IGitService
{
    private const string GitExecutable = "git";
    private readonly IProcessRunner _processRunner;

    public ProcessGitService(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    public async Task<Result<GitCommandResult>> CheckGitAvailableAsync(CancellationToken cancellationToken = default)
    {
        var result = await RunGitAsync(new[] { "--version" }, null, cancellationToken).ConfigureAwait(false);
        return Result<GitCommandResult>.Ok(result);
    }

    public async Task<Result<GitRepositoryStatus>> GetRepositoryStatusAsync(
        string workspaceRoot,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
        {
            var failure = GitCommandResult.Failure(
                GitExecutable,
                Array.Empty<string>(),
                null,
                "Workspace root is required to inspect Git status.");
            return Result<GitRepositoryStatus>.Ok(GitRepositoryStatus.Hidden(failure, isGitAvailable: false));
        }

        var availability = await CheckGitAvailableAsync(cancellationToken).ConfigureAwait(false);
        var availabilityResult = availability.Value!;
        if (!availabilityResult.IsSuccess)
            return Result<GitRepositoryStatus>.Ok(GitRepositoryStatus.Hidden(availabilityResult, isGitAvailable: false));

        var repoProbe = await RunWorkspaceGitAsync(
            workspaceRoot,
            new[] { "rev-parse", "--is-inside-work-tree" },
            cancellationToken).ConfigureAwait(false);

        if (!repoProbe.IsSuccess || !repoProbe.Stdout.Trim().Equals("true", StringComparison.OrdinalIgnoreCase))
            return Result<GitRepositoryStatus>.Ok(GitRepositoryStatus.Hidden(repoProbe, isGitAvailable: true));

        var branch = await RunWorkspaceGitAsync(
            workspaceRoot,
            new[] { "branch", "--show-current" },
            cancellationToken).ConfigureAwait(false);

        var status = await RunWorkspaceGitAsync(
            workspaceRoot,
            new[] { "status", "--porcelain" },
            cancellationToken).ConfigureAwait(false);

        var branchName = branch.Stdout.Trim();
        if (string.IsNullOrEmpty(branchName))
            branchName = "HEAD";

        var changeCount = status.IsSuccess ? CountPorcelainStatusLines(status.Stdout) : 0;

        return Result<GitRepositoryStatus>.Ok(new GitRepositoryStatus(
            true,
            true,
            branchName,
            changeCount,
            repoProbe,
            branch,
            status));
    }

    public async Task<Result<GitCommandResult>> StageAllAndCommitAsync(
        string workspaceRoot,
        string commitMessage,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateCommitInputs(workspaceRoot, commitMessage);
        if (!validation.IsSuccess)
            return validation;

        var stage = await StageAllAsync(workspaceRoot, cancellationToken).ConfigureAwait(false);
        if (!stage.IsSuccess)
            return Result<GitCommandResult>.Ok(stage);

        var commit = await CommitAsync(workspaceRoot, commitMessage, cancellationToken).ConfigureAwait(false);
        return Result<GitCommandResult>.Ok(commit);
    }

    public async Task<Result<GitCommandResult>> StageAllCommitAndPushAsync(
        string workspaceRoot,
        string commitMessage,
        CancellationToken cancellationToken = default)
    {
        var commit = await StageAllAndCommitAsync(workspaceRoot, commitMessage, cancellationToken).ConfigureAwait(false);
        if (!commit.IsSuccess || !commit.Value!.IsSuccess)
            return commit;

        var push = await RunWorkspaceGitAsync(workspaceRoot, new[] { "push" }, cancellationToken).ConfigureAwait(false);
        return Result<GitCommandResult>.Ok(push);
    }

    private async Task<GitCommandResult> StageAllAsync(string workspaceRoot, CancellationToken cancellationToken)
        => await RunWorkspaceGitAsync(workspaceRoot, new[] { "add", "--all" }, cancellationToken).ConfigureAwait(false);

    private async Task<GitCommandResult> CommitAsync(
        string workspaceRoot,
        string commitMessage,
        CancellationToken cancellationToken)
        => await RunWorkspaceGitAsync(workspaceRoot, new[] { "commit", "-m", commitMessage }, cancellationToken).ConfigureAwait(false);

    private async Task<GitCommandResult> RunWorkspaceGitAsync(
        string workspaceRoot,
        IReadOnlyList<string> gitArguments,
        CancellationToken cancellationToken)
    {
        var arguments = new[] { "-C", workspaceRoot }.Concat(gitArguments).ToArray();
        return await RunGitAsync(arguments, null, cancellationToken).ConfigureAwait(false);
    }

    private async Task<GitCommandResult> RunGitAsync(
        IReadOnlyList<string> arguments,
        string? workingDirectory,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _processRunner.RunAsync(GitExecutable, arguments, workingDirectory, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GitCommandResult.Failure(GitExecutable, arguments, workingDirectory, ex.Message);
        }
    }

    private static Result<GitCommandResult> ValidateCommitInputs(string workspaceRoot, string commitMessage)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return Result<GitCommandResult>.Fail("Workspace root is required for Git commit operations.");

        if (string.IsNullOrWhiteSpace(commitMessage))
            return Result<GitCommandResult>.Fail("Commit message is required.");

        return Result<GitCommandResult>.Ok(GitCommandResult.Failure(GitExecutable, Array.Empty<string>(), null, string.Empty));
    }

    private static int CountPorcelainStatusLines(string stdout)
        => stdout
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Count(line => !string.IsNullOrWhiteSpace(line));
}
