using Hymnal.Core.Services;

namespace Hymnal.Core.Tests.Services;

public class GitSyncSummaryTests
{
    [Fact]
    public void Format_PluralizesLocalAndRemoteIndicators()
    {
        Assert.Equal("Up to date", GitSyncSummary.Format(0, 0, 0, false));
        Assert.Equal("1 uncommitted change", GitSyncSummary.Format(1, 0, 0, false));
        Assert.Equal("3 uncommitted changes · 2 commits to pull", GitSyncSummary.Format(3, 2, 0, false));
        Assert.Equal("Merge conflict", GitSyncSummary.Format(0, 0, 0, true));
        Assert.Equal("Already up to date", GitSyncSummary.FormatPullResult(0));
        Assert.Equal("3 commits pulled", GitSyncSummary.FormatPullResult(3));
        Assert.Equal("Pulling…", GitSyncSummary.Pulling);
    }
}
