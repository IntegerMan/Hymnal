using Hymnal.Core.Infrastructure;

namespace Hymnal.Core.Tests.Infrastructure;

public class MetadataStoreTests
{
    private static string MakeTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(path);
        return path;
    }

    [Fact]
    public async Task WriteTextAtomicAsync_CreatesFile()
    {
        // Arrange
        var dir = MakeTempDir();
        try
        {
            var target = Path.Combine(dir, "output.md");
            var store = new MetadataStore();

            // Act
            await store.WriteTextAtomicAsync(target, "# Hello");

            // Assert
            Assert.True(File.Exists(target));
            Assert.Equal("# Hello", await File.ReadAllTextAsync(target));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task WriteTextAtomicAsync_CreatesParentDirectory()
    {
        // Arrange: target inside a non-existent subdirectory
        var baseDir = MakeTempDir();
        try
        {
            var target = Path.Combine(baseDir, "sub", "nested", "output.md");
            var store = new MetadataStore();

            // Act
            await store.WriteTextAtomicAsync(target, "content");

            // Assert
            Assert.True(File.Exists(target));
        }
        finally
        {
            Directory.Delete(baseDir, recursive: true);
        }
    }

    [Fact]
    public async Task WriteTextAtomicAsync_OverwritesExistingFile()
    {
        // Arrange
        var dir = MakeTempDir();
        try
        {
            var target = Path.Combine(dir, "chapter.md");
            await File.WriteAllTextAsync(target, "original");
            var store = new MetadataStore();

            // Act
            await store.WriteTextAtomicAsync(target, "updated");

            // Assert
            Assert.Equal("updated", await File.ReadAllTextAsync(target));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
