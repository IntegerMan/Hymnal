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
        bool includeRemoteState = false,
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

        var allChangedFiles = status.IsSuccess
            ? GitPorcelainParser.Parse(status.Stdout)
            : Array.Empty<GitChangedFile>();
        var changedFiles = GitChangeDisplayFilter.Apply(allChangedFiles);
        var changeCount = changedFiles.Count;

        var conflictedFiles = await GetConflictedFilesAsync(workspaceRoot, cancellationToken).ConfigureAwait(false);
        var hasMergeConflict = conflictedFiles.Count > 0;

        var behindRemoteCount = 0;
        var aheadRemoteCount = 0;
        if (includeRemoteState)
        {
            await FetchAsync(workspaceRoot, cancellationToken).ConfigureAwait(false);
            var divergence = await GetRemoteDivergenceAsync(workspaceRoot, cancellationToken).ConfigureAwait(false);
            behindRemoteCount = divergence.Behind;
            aheadRemoteCount = divergence.Ahead;
        }

        return Result<GitRepositoryStatus>.Ok(new GitRepositoryStatus(
            true,
            true,
            branchName,
            changeCount,
            changedFiles,
            behindRemoteCount,
            aheadRemoteCount,
            hasMergeConflict,
            conflictedFiles,
            repoProbe,
            branch,
            status));
    }

    public async Task<Result<GitCommandResult>> FetchAsync(
        string workspaceRoot,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return Result<GitCommandResult>.Fail("Workspace root is required for Git fetch operations.");

        var fetch = await RunWorkspaceGitAsync(workspaceRoot, new[] { "fetch", "--quiet" }, cancellationToken).ConfigureAwait(false);
        return Result<GitCommandResult>.Ok(fetch);
    }

    public async Task<Result<GitCommandResult>> PullAsync(
        string workspaceRoot,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return Result<GitCommandResult>.Fail("Workspace root is required for Git pull operations.");

        var pull = await RunWorkspaceGitAsync(workspaceRoot, new[] { "pull", "--no-edit" }, cancellationToken).ConfigureAwait(false);
        if (pull.IsSuccess)
            return Result<GitCommandResult>.Ok(pull);

        var conflictedFiles = await GetConflictedFilesAsync(workspaceRoot, cancellationToken).ConfigureAwait(false);
        if (conflictedFiles.Count > 0)
        {
            var message = BuildMergeConflictMessage(conflictedFiles);
            return Result<GitCommandResult>.Fail(message);
        }

        var stderr = !string.IsNullOrWhiteSpace(pull.Stderr) ? pull.Stderr : $"Git pull failed (exit code {pull.ExitCode}).";
        return Result<GitCommandResult>.Fail(stderr);
    }

    public async Task<Result<GitCommandResult>> PushAsync(
        string workspaceRoot,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return Result<GitCommandResult>.Fail("Workspace root is required for Git push operations.");

        var push = await RunWorkspaceGitAsync(workspaceRoot, new[] { "push" }, cancellationToken).ConfigureAwait(false);
        return Result<GitCommandResult>.Ok(push);
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

        var push = await PushAsync(workspaceRoot, cancellationToken).ConfigureAwait(false);
        if (!push.IsSuccess)
            return push;

        return Result<GitCommandResult>.Ok(push.Value!);
    }

    public async Task<Result<GitCommandResult>> StageAllCommitPullAndPushAsync(
        string workspaceRoot,
        string commitMessage,
        CancellationToken cancellationToken = default)
    {
        var commit = await StageAllAndCommitAsync(workspaceRoot, commitMessage, cancellationToken).ConfigureAwait(false);
        if (!commit.IsSuccess || !commit.Value!.IsSuccess)
            return commit;

        var pull = await PullAsync(workspaceRoot, cancellationToken).ConfigureAwait(false);
        if (!pull.IsSuccess)
            return pull;

        var push = await PushAsync(workspaceRoot, cancellationToken).ConfigureAwait(false);
        if (!push.IsSuccess)
            return push;

        return Result<GitCommandResult>.Ok(push.Value!);
    }

    private async Task<(int Behind, int Ahead)> GetRemoteDivergenceAsync(
        string workspaceRoot,
        CancellationToken cancellationToken)
    {
        var upstream = await RunWorkspaceGitAsync(
            workspaceRoot,
            new[] { "rev-parse", "--abbrev-ref", "@{upstream}" },
            cancellationToken).ConfigureAwait(false);

        if (!upstream.IsSuccess || string.IsNullOrWhiteSpace(upstream.Stdout))
            return (0, 0);

        var behindResult = await RunWorkspaceGitAsync(
            workspaceRoot,
            new[] { "rev-list", "--count", "HEAD..@{upstream}" },
            cancellationToken).ConfigureAwait(false);

        var aheadResult = await RunWorkspaceGitAsync(
            workspaceRoot,
            new[] { "rev-list", "--count", "@{upstream}..HEAD" },
            cancellationToken).ConfigureAwait(false);

        var behind = ParseCount(behindResult.Stdout);
        var ahead = ParseCount(aheadResult.Stdout);
        return (behind, ahead);
    }

    private async Task<IReadOnlyList<string>> GetConflictedFilesAsync(
        string workspaceRoot,
        CancellationToken cancellationToken)
    {
        var result = await RunWorkspaceGitAsync(
            workspaceRoot,
            new[] { "diff", "--name-only", "--diff-filter=U" },
            cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.Stdout))
            return Array.Empty<string>();

        return result.Stdout
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(path => path.Replace('\\', '/'))
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string BuildMergeConflictMessage(IReadOnlyList<string> conflictedFiles)
    {
        var fileList = string.Join(Environment.NewLine, conflictedFiles.Select(path => $"  • {path}"));
        return $"Merge conflict in one or more files. Resolve using your preferred Git tooling before syncing:{Environment.NewLine}{fileList}";
    }

    private static int ParseCount(string stdout)
        => int.TryParse(stdout.Trim(), out var count) ? count : 0;

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
}
