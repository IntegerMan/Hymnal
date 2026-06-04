using Hymnal.Core.Common;
using Hymnal.Core.Infrastructure;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Services;

namespace Hymnal.Core.Tests.Services;

public class BookTxtStructureServiceTests
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

    private static BookTxtStructureService CreateService(IMetadataStore? metadataStore = null)
        => new(metadataStore ?? new MetadataStore());

    private static string ReadBookTxt(string bookTxtPath) => File.ReadAllText(bookTxtPath);

    private static string[] ReadBookTxtLines(string bookTxtPath) => File.ReadAllLines(bookTxtPath);

    [Fact]
    public async Task ReorderEntryAsync_MovesEntryWithinPart()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/part.md\npart-one/chapter-one.md\npart-one/chapter-two.md"),
            ("part-one/part.md", "{class: part}\n# Part One"),
            ("part-one/chapter-one.md", "# Chapter One"),
            ("part-one/chapter-two.md", "# Chapter Two"));

        try
        {
            var service = CreateService();

            var result = await service.ReorderEntryAsync(workspace.BookTxtPath, "part-one/chapter-one.md", 2);

            Assert.True(result.IsSuccess, result.Error);
            Assert.Equal(new[]
            {
                "part-one/part.md",
                "part-one/chapter-two.md",
                "part-one/chapter-one.md"
            }, ReadBookTxtLines(workspace.BookTxtPath));
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task ReorderEntryAsync_MovesEntryAcrossParts_AndKeepsBlankLines()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/part.md\n\npart-one/chapter-one.md\npart-one/chapter-two.md\n\npart-two/part.md\npart-two/chapter-three.md"),
            ("part-one/part.md", "{class: part}\n# Part One"),
            ("part-one/chapter-one.md", "# Chapter One"),
            ("part-one/chapter-two.md", "# Chapter Two"),
            ("part-two/part.md", "{class: part}\n# Part Two"),
            ("part-two/chapter-three.md", "# Chapter Three"));

        try
        {
            var service = CreateService();

            var result = await service.ReorderEntryAsync(workspace.BookTxtPath, "part-two/chapter-three.md", 1);

            Assert.True(result.IsSuccess, result.Error);
            Assert.Equal(new[]
            {
                "part-one/part.md",
                string.Empty,
                "part-two/chapter-three.md",
                "part-one/chapter-one.md",
                "part-one/chapter-two.md",
                string.Empty,
                "part-two/part.md"
            }, ReadBookTxtLines(workspace.BookTxtPath));
            Assert.True(File.Exists(Path.Combine(workspace.Root, "part-two", "chapter-three.md")));
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task AddExistingEntryAsync_InsertsExistingFileAtIndex()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/part.md\npart-one/chapter-one.md\npart-one/chapter-three.md"),
            ("part-one/part.md", "{class: part}\n# Part One"),
            ("part-one/chapter-one.md", "# Chapter One"),
            ("part-one/chapter-three.md", "# Chapter Three"),
            ("part-one/chapter-two.md", "# Chapter Two"));

        try
        {
            var service = CreateService();

            var result = await service.AddExistingEntryAsync(workspace.BookTxtPath, "part-one/chapter-two.md", 2);

            Assert.True(result.IsSuccess, result.Error);
            Assert.Equal(new[]
            {
                "part-one/part.md",
                "part-one/chapter-one.md",
                "part-one/chapter-two.md",
                "part-one/chapter-three.md"
            }, ReadBookTxtLines(workspace.BookTxtPath));
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task AddExistingEntryAfterPartAsync_InsertsAfterPartDivider()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/part.md\n\npart-one/chapter-one.md"),
            ("part-one/part.md", "{class: part}\n# Part One"),
            ("part-one/chapter-one.md", "# Chapter One"),
            ("part-one/chapter-two.md", "# Chapter Two"));

        try
        {
            var service = CreateService();

            var result = await service.AddExistingEntryAfterPartAsync(workspace.BookTxtPath, "part-one/chapter-two.md", "part-one/part.md");

            Assert.True(result.IsSuccess, result.Error);
            Assert.Equal(new[]
            {
                "part-one/part.md",
                string.Empty,
                "part-one/chapter-two.md",
                "part-one/chapter-one.md"
            }, ReadBookTxtLines(workspace.BookTxtPath));
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task CreateNewChapterAsync_CreatesFileAndBookEntry()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/part.md\npart-one/chapter-one.md"),
            ("part-one/part.md", "{class: part}\n# Part One"),
            ("part-one/chapter-one.md", "# Chapter One"));

        try
        {
            var service = CreateService();

            var result = await service.CreateNewChapterAsync(workspace.BookTxtPath, "part-one/chapter-two.md", "# Chapter Two", 2);

            Assert.True(result.IsSuccess, result.Error);
            Assert.True(File.Exists(Path.Combine(workspace.Root, "part-one", "chapter-two.md")));
            Assert.Equal(new[]
            {
                "part-one/part.md",
                "part-one/chapter-one.md",
                "part-one/chapter-two.md"
            }, ReadBookTxtLines(workspace.BookTxtPath));
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task RenameEntryAsync_ReplacesPathEntry()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/part.md\npart-one/chapter-one.md"),
            ("part-one/part.md", "{class: part}\n# Part One"),
            ("part-one/chapter-one.md", "# Chapter One"),
            ("part-one/chapter-renamed.md", "# Chapter Renamed"));

        try
        {
            var service = CreateService();

            var result = await service.RenameEntryAsync(workspace.BookTxtPath, "part-one/chapter-one.md", "part-one/chapter-renamed.md");

            Assert.True(result.IsSuccess, result.Error);
            Assert.Equal(new[]
            {
                "part-one/part.md",
                "part-one/chapter-renamed.md"
            }, ReadBookTxtLines(workspace.BookTxtPath));
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task RemoveEntryAsync_RemovesBookLineWithoutDeletingFile()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/part.md\npart-one/chapter-one.md"),
            ("part-one/part.md", "{class: part}\n# Part One"),
            ("part-one/chapter-one.md", "# Chapter One"));

        try
        {
            var service = CreateService();

            var result = await service.RemoveEntryAsync(workspace.BookTxtPath, "part-one/chapter-one.md");

            Assert.True(result.IsSuccess, result.Error);
            Assert.Equal(new[] { "part-one/part.md" }, ReadBookTxtLines(workspace.BookTxtPath));
            Assert.True(File.Exists(Path.Combine(workspace.Root, "part-one", "chapter-one.md")));
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task DeleteChapterFileAsync_RemovesBookLineAndFile()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/part.md\npart-one/chapter-one.md"),
            ("part-one/part.md", "{class: part}\n# Part One"),
            ("part-one/chapter-one.md", "# Chapter One"));

        try
        {
            var service = CreateService();

            var result = await service.DeleteChapterFileAsync(workspace.BookTxtPath, "part-one/chapter-one.md");

            Assert.True(result.IsSuccess, result.Error);
            Assert.Equal(new[] { "part-one/part.md" }, ReadBookTxtLines(workspace.BookTxtPath));
            Assert.False(File.Exists(Path.Combine(workspace.Root, "part-one", "chapter-one.md")));
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task ReadNormalizedEntriesAsync_FailsWhenBookTxtIsMissing()
    {
        var workspace = CreateWorkspace();

        try
        {
            var service = CreateService();

            var result = await service.ReadNormalizedEntriesAsync(workspace.BookTxtPath);

            Assert.False(result.IsSuccess);
            Assert.Contains(workspace.BookTxtPath, result.Error);
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task ReadNormalizedEntriesAsync_FailsOnDuplicateEntries()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/part.md\npart-one/part.md"),
            ("part-one/part.md", "{class: part}\n# Part One"));

        try
        {
            var service = CreateService();

            var result = await service.ReadNormalizedEntriesAsync(workspace.BookTxtPath);

            Assert.False(result.IsSuccess);
            Assert.Contains("Duplicate Book.txt entry", result.Error);
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task AddExistingEntryAsync_RejectsTraversalOutsideManuscriptRoot()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/part.md"),
            ("part-one/part.md", "{class: part}\n# Part One"));

        try
        {
            var service = CreateService();

            var result = await service.AddExistingEntryAsync(workspace.BookTxtPath, "../escape.md", 1);

            Assert.False(result.IsSuccess);
            Assert.Contains("outside the manuscript root", result.Error);
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task AddExistingEntryAsync_RejectsMissingFile()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/part.md"),
            ("part-one/part.md", "{class: part}\n# Part One"));

        try
        {
            var service = CreateService();

            var result = await service.AddExistingEntryAsync(workspace.BookTxtPath, "part-one/chapter-missing.md", 1);

            Assert.False(result.IsSuccess);
            Assert.Contains("does not exist", result.Error);
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task ReorderEntryAsync_RejectsUnknownPath()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/part.md\npart-one/chapter-one.md"),
            ("part-one/part.md", "{class: part}\n# Part One"),
            ("part-one/chapter-one.md", "# Chapter One"));

        try
        {
            var service = CreateService();

            var result = await service.ReorderEntryAsync(workspace.BookTxtPath, "part-one/chapter-missing.md", 0);

            Assert.False(result.IsSuccess);
            Assert.Contains("was not found", result.Error);
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task DeleteChapterFileAsync_RejectsMissingFile()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/part.md\npart-one/chapter-one.md"),
            ("part-one/part.md", "{class: part}\n# Part One"));

        try
        {
            var service = CreateService();

            var result = await service.DeleteChapterFileAsync(workspace.BookTxtPath, "part-one/chapter-one.md");

            Assert.False(result.IsSuccess);
            Assert.Contains("does not exist", result.Error);
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task ReorderEntryAsync_LeavesOriginalBookTxtWhenAtomicWriteFails()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/part.md\npart-one/chapter-one.md"),
            ("part-one/part.md", "{class: part}\n# Part One"),
            ("part-one/chapter-one.md", "# Chapter One"));

        try
        {
            var original = ReadBookTxt(workspace.BookTxtPath);
            var service = CreateService(new ThrowingMetadataStore());

            var result = await service.ReorderEntryAsync(workspace.BookTxtPath, "part-one/chapter-one.md", 0);

            Assert.False(result.IsSuccess);
            Assert.Equal(original, ReadBookTxt(workspace.BookTxtPath));
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    private sealed class ThrowingMetadataStore : IMetadataStore
    {
        public Task WriteTextAtomicAsync(string absolutePath, string content)
            => throw new IOException("simulated atomic write failure");
    }
}
