namespace Hymnal.Core.Models;

/// <summary>
/// Git status projected for the workspace toolbar.
/// </summary>
public sealed record GitRepositoryStatus(
    bool IsGitAvailable,
    bool IsRepository,
    string? BranchName,
    int UncommittedChangeCount,
    GitCommandResult ProbeResult,
    GitCommandResult? BranchResult,
    GitCommandResult? StatusResult)
{
    public static GitRepositoryStatus Hidden(GitCommandResult probeResult, bool isGitAvailable)
        => new(isGitAvailable, false, null, 0, probeResult, null, null);
}
