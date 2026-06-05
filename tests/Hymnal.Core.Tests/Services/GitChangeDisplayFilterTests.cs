using Hymnal.Core.Models;
using Hymnal.Core.Services;

namespace Hymnal.Core.Tests.Services;

public class GitChangeDisplayFilterTests
{
    [Fact]
    public void Apply_ExcludesLockFilesOnly()
    {
        var files = new[]
        {
            new GitChangedFile("manuscript/ch01.md", "M"),
            new GitChangedFile(".hymnal-data/wordcount-history.json", "M"),
            new GitChangedFile(".hymnal-data/targets.lock", "M"),
            new GitChangedFile(".hymnal-data/notes/ch01.md", "M")
        };

        var filtered = GitChangeDisplayFilter.Apply(files);

        Assert.Equal(3, filtered.Count);
        Assert.Contains(filtered, file => file.RelativePath == "manuscript/ch01.md");
        Assert.Contains(filtered, file => file.RelativePath == ".hymnal-data/wordcount-history.json");
        Assert.Contains(filtered, file => file.RelativePath == ".hymnal-data/notes/ch01.md");
        Assert.DoesNotContain(filtered, file => file.RelativePath == ".hymnal-data/targets.lock");
    }
}
