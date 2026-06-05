using Hymnal.Core.Models;
using Hymnal.Core.Services;

namespace Hymnal.Core.Tests.Services;

public class GitPorcelainParserTests
{
    [Fact]
    public void Parse_EmptyOutput_ReturnsEmptyList()
    {
        var files = GitPorcelainParser.Parse(string.Empty);
        Assert.Empty(files);
    }

    [Fact]
    public void Parse_ModifiedAddedUntrackedAndRename()
    {
        const string stdout = """
             M manuscript/ch01.md
            A  docs/outline.md
            ?? manuscript/ch02.md
            R  old.md -> new.md
            """;

        var files = GitPorcelainParser.Parse(stdout);

        Assert.Equal(4, files.Count);
        Assert.Equal("manuscript/ch01.md", files[0].RelativePath);
        Assert.Equal("M", files[0].StatusCode);
        Assert.Equal("docs/outline.md", files[1].RelativePath);
        Assert.Equal("A", files[1].StatusCode);
        Assert.Equal("manuscript/ch02.md", files[2].RelativePath);
        Assert.Equal("??", files[2].StatusCode);
        Assert.Equal("new.md", files[3].RelativePath);
        Assert.Equal("R", files[3].StatusCode);
    }
}
