namespace Hymnal.Core.Models;

/// <summary>
/// Git status projected for the workspace toolbar.
/// </summary>
public sealed record GitRepositoryStatus(
    bool IsGitAvailable,
    bool IsRepository,
    string? BranchName,
    int UncommittedChangeCount,
    IReadOnlyList<GitChangedFile> ChangedFiles,
    int BehindRemoteCount,
    int AheadRemoteCount,
    bool HasMergeConflict,
    IReadOnlyList<string> ConflictedFiles,
    GitCommandResult ProbeResult,
    GitCommandResult? BranchResult,
    GitCommandResult? StatusResult)
{
    public static GitRepositoryStatus Hidden(GitCommandResult probeResult, bool isGitAvailable)
        => new(
            isGitAvailable,
            false,
            null,
            0,
            Array.Empty<GitChangedFile>(),
            0,
            0,
            false,
            Array.Empty<string>(),
            probeResult,
            null,
            null);
}
