using Hymnal.Core.Services;

namespace Hymnal.Core.Tests.Services;

public class OrphanFileDiscoveryServiceTests
{
    private static (string Root, string BookTxtPath) CreateWorkspace(params (string Path, string Content)[] files)
    {
        var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(root);

        foreach (var (relativePath, content) in files)
        {
            var absolutePath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(absolutePath, content);
        }

        return (root, Path.Combine(root, "Book.txt"));
    }

    [Fact]
    public async Task DiscoverAsync_FindsOrphanMdFiles_AndExcludesBookTxtEntries()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/part.md\npart-one/chapter-one.md"),
            ("part-one/part.md", "{class: part}\n# Part One"),
            ("part-one/chapter-one.md", "# Chapter One"),
            ("part-one/chapter-two.md", "# Chapter Two"),
            ("loose-chapter.md", "# Loose"));

        try
        {
            var service = new OrphanFileDiscoveryService();
            var entries = new[] { "part-one/part.md", "part-one/chapter-one.md" };

            var orphans = await service.DiscoverAsync(workspace.Root, entries);

            Assert.Equal(2, orphans.Count);
            Assert.Contains(orphans, o => o.RelativePath == "part-one/chapter-two.md");
            Assert.Contains(orphans, o => o.RelativePath == "loose-chapter.md");
            Assert.Equal("part-one", orphans.Single(o => o.RelativePath == "part-one/chapter-two.md").DetectedPartFolder);
            Assert.Null(orphans.Single(o => o.RelativePath == "loose-chapter.md").DetectedPartFolder);
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task DiscoverAsync_IgnoresHymnalDataDirectory()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "chapter-one.md"),
            ("chapter-one.md", "# Chapter One"),
            (".hymnal-data/notes/hidden.md", "# Hidden"));

        try
        {
            var service = new OrphanFileDiscoveryService();
            var orphans = await service.DiscoverAsync(workspace.Root, new[] { "chapter-one.md" });

            Assert.Empty(orphans);
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }
}
