using Hymnal.Core.Models;
using Hymnal.Core.Services;

namespace Hymnal.Core.Tests.Services;

public class GitChangeTreeBuilderTests
{
    [Fact]
    public void Build_GroupsFilesIntoFolderHierarchy()
    {
        var files = new[]
        {
            new GitChangedFile("manuscript/ch01.md", "M"),
            new GitChangedFile("manuscript/ch02.md", "A"),
            new GitChangedFile("Book.txt", "M")
        };

        var tree = GitChangeTreeBuilder.Build(files);

        Assert.Equal(2, tree.Count);
        var manuscriptFolder = Assert.Single(tree, node => node.DisplayName == "manuscript");
        Assert.True(manuscriptFolder.IsFolder);
        Assert.Equal(2, manuscriptFolder.Children.Count);
        Assert.Contains(manuscriptFolder.Children, node => node.RelativePath == "manuscript/ch01.md");
    }
}
