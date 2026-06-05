namespace Hymnal.Core.Services;

/// <summary>
/// Formats Git toolbar sync summary text from local/remote divergence counts.
/// </summary>
public static class GitSyncSummary
{
    public static string Format(
        int uncommittedChangeCount,
        int behindRemoteCount,
        int aheadRemoteCount,
        bool hasMergeConflict)
    {
        if (hasMergeConflict)
            return "Merge conflict";

        var parts = new List<string>();
        if (uncommittedChangeCount > 0)
        {
            parts.Add(uncommittedChangeCount == 1
                ? "1 uncommitted change"
                : $"{uncommittedChangeCount:N0} uncommitted changes");
        }

        if (behindRemoteCount > 0)
        {
            parts.Add(behindRemoteCount == 1
                ? "1 commit to pull"
                : $"{behindRemoteCount:N0} commits to pull");
        }

        if (aheadRemoteCount > 0)
        {
            parts.Add(aheadRemoteCount == 1
                ? "1 commit to push"
                : $"{aheadRemoteCount:N0} commits to push");
        }

        return parts.Count == 0 ? "Up to date" : string.Join(" · ", parts);
    }

    public static string Pulling => "Pulling…";

    public static string FormatPullResult(int commitsPulled)
        => commitsPulled switch
        {
            0 => "Already up to date",
            1 => "1 commit pulled",
            _ => $"{commitsPulled:N0} commits pulled"
        };
}
