using Hymnal.Core.Models;
using Hymnal.Core.Services;

namespace Hymnal.Core.Tests.Services;

public class BookTxtParserTests
{
    private static string FixturesRoot =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "SampleManuscripts");

    [Fact]
    public void SimpleBook_ParsesSingleChapter()
    {
        var folder = Path.Combine(FixturesRoot, "simple-book");
        var lines = File.ReadAllLines(Path.Combine(folder, "Book.txt"));

        var nodes = BookTxtParser.Parse(folder, lines);

        Assert.Single(nodes);
        Assert.Equal(NodeKind.Chapter, nodes[0].Kind);
        Assert.False(nodes[0].IsMissing);
    }

    [Fact]
    public void MultiPartBook_ParsesTwoNodes_PartAndChapter()
    {
        var folder = Path.Combine(FixturesRoot, "multi-part-book");
        var lines = File.ReadAllLines(Path.Combine(folder, "Book.txt"));

        var nodes = BookTxtParser.Parse(folder, lines);

        Assert.Equal(2, nodes.Count);
        Assert.Equal(NodeKind.Part, nodes[0].Kind);
        Assert.Equal(NodeKind.Chapter, nodes[1].Kind);
    }

    [Fact]
    public void BlankLines_AreIgnored()
    {
        var lines = new[] { "", "  ", "\t", "" };

        var nodes = BookTxtParser.Parse(Path.GetTempPath(), lines);

        Assert.Empty(nodes);
    }

    [Fact]
    public void MissingFile_MarkedIsMissing()
    {
        var lines = new[] { "does-not-exist.md" };

        var nodes = BookTxtParser.Parse(Path.GetTempPath(), lines);

        Assert.Single(nodes);
        Assert.True(nodes[0].IsMissing);
        Assert.Equal(NodeKind.Chapter, nodes[0].Kind);
    }
}
