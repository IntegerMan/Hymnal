using Hymnal.Core.Models;
using Hymnal.Core.Services;

namespace Hymnal.Core.Tests.Services;

public class GitChangedFileDisplayTests
{
    [Fact]
    public void FormatLabel_IncludesRelativePathAndStatus()
    {
        var label = GitChangedFileDisplay.FormatLabel(new GitChangedFile(".hymnal-data/phases.json", "M"));

        Assert.Equal(".hymnal-data/phases.json  modified", label);
    }
}
