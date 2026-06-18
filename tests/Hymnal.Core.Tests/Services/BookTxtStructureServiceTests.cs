using Hymnal.Core.Common;
using Hymnal.Core.Infrastructure;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models;
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

    private static BookTxtStructureService CreateService(
        IMetadataStore? metadataStore = null,
        IExclusionManifestService? exclusionManifestService = null)
    {
        var store = metadataStore ?? new MetadataStore();
        return new BookTxtStructureService(store, exclusionManifestService ?? new ExclusionManifestService(store));
    }

    private static async Task<string[]> LoadExcludedPathsAsync(string workspaceRoot)
    {
        var result = await new ExclusionManifestService(new MetadataStore()).LoadAsync(workspaceRoot);
        Assert.True(result.IsSuccess, result.Error);
        return result.Value!.ExcludedPaths;
    }

    private static async Task SaveRegistryAsync(string workspaceRoot, params ChapterRegistryEntry[] entries)
    {
        var registry = entries.ToDictionary(entry => entry.Uuid, entry => entry);
        await new ChapterRegistryService(new MetadataStore()).SaveAsync(workspaceRoot, registry);
    }

    private static async Task<Dictionary<string, ChapterRegistryEntry>> LoadRegistryAsync(string workspaceRoot)
        => await new ChapterRegistryService(new MetadataStore()).LoadAsync(workspaceRoot);

    private static string AbsolutePath(string workspaceRoot, string relativePath)
        => Path.Combine(workspaceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

    private static string ManifestPath(string workspaceRoot)
        => Path.Combine(workspaceRoot, ".hymnal-data", "exclusions.json");

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
    public async Task IncludeExclude_ExcludeEntryAsync_RemovesBookLineAndAddsManifestPath()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/part.md\npart-one/chapter-one.md"),
            ("part-one/part.md", "{class: part}\n# Part One"),
            ("part-one/chapter-one.md", "# Chapter One"));

        try
        {
            var service = CreateService();

            var result = await service.ExcludeEntryAsync(workspace.BookTxtPath, "part-one/chapter-one.md");

            Assert.True(result.IsSuccess, result.Error);
            Assert.Equal(new[] { "part-one/part.md" }, ReadBookTxtLines(workspace.BookTxtPath));
            Assert.True(File.Exists(Path.Combine(workspace.Root, "part-one", "chapter-one.md")));
            Assert.Equal(new[] { "part-one/chapter-one.md" }, await LoadExcludedPathsAsync(workspace.Root));
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task IncludeExclude_IncludeExistingEntryAsync_InsertsAtIndexAndRemovesManifestPath()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/part.md\npart-one/chapter-two.md"),
            ("part-one/part.md", "{class: part}\n# Part One"),
            ("part-one/chapter-one.md", "# Chapter One"),
            ("part-one/chapter-two.md", "# Chapter Two"));

        try
        {
            var manifest = new ExclusionManifestService(new MetadataStore());
            var exclude = await manifest.ExcludeAsync(workspace.Root, "part-one/chapter-one.md");
            Assert.True(exclude.IsSuccess, exclude.Error);
            var service = CreateService();

            var result = await service.IncludeExistingEntryAsync(workspace.BookTxtPath, "part-one/chapter-one.md", 1);

            Assert.True(result.IsSuccess, result.Error);
            Assert.Equal(new[]
            {
                "part-one/part.md",
                "part-one/chapter-one.md",
                "part-one/chapter-two.md"
            }, ReadBookTxtLines(workspace.BookTxtPath));
            Assert.Empty(await LoadExcludedPathsAsync(workspace.Root));
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task IncludeExclude_IncludeExistingEntryAfterPartAsync_InsertsAfterPartAndRemovesManifestPath()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/part.md\n\npart-one/chapter-two.md"),
            ("part-one/part.md", "{class: part}\n# Part One"),
            ("part-one/chapter-one.md", "# Chapter One"),
            ("part-one/chapter-two.md", "# Chapter Two"));

        try
        {
            var manifest = new ExclusionManifestService(new MetadataStore());
            var exclude = await manifest.ExcludeAsync(workspace.Root, "part-one/chapter-one.md");
            Assert.True(exclude.IsSuccess, exclude.Error);
            var service = CreateService();

            var result = await service.IncludeExistingEntryAfterPartAsync(
                workspace.BookTxtPath,
                "part-one/chapter-one.md",
                "part-one/part.md");

            Assert.True(result.IsSuccess, result.Error);
            Assert.Equal(new[]
            {
                "part-one/part.md",
                string.Empty,
                "part-one/chapter-one.md",
                "part-one/chapter-two.md"
            }, ReadBookTxtLines(workspace.BookTxtPath));
            Assert.Empty(await LoadExcludedPathsAsync(workspace.Root));
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task IncludeExclude_IncludeExistingEntryAsync_RejectsDuplicateBookTxtEntry()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/part.md\npart-one/chapter-one.md"),
            ("part-one/part.md", "{class: part}\n# Part One"),
            ("part-one/chapter-one.md", "# Chapter One"));

        try
        {
            var manifest = new RecordingManifestService();
            var service = CreateService(exclusionManifestService: manifest);

            var result = await service.IncludeExistingEntryAsync(workspace.BookTxtPath, "part-one/chapter-one.md", 1);

            Assert.False(result.IsSuccess);
            Assert.Contains("already exists", result.Error);
            Assert.Equal(0, manifest.IncludeCalls);
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task IncludeExclude_IncludeExistingEntryAsync_RejectsMissingFile()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/part.md"),
            ("part-one/part.md", "{class: part}\n# Part One"));

        try
        {
            var manifest = new RecordingManifestService();
            var service = CreateService(exclusionManifestService: manifest);

            var result = await service.IncludeExistingEntryAsync(workspace.BookTxtPath, "part-one/missing.md", 1);

            Assert.False(result.IsSuccess);
            Assert.Contains("does not exist", result.Error);
            Assert.Equal(0, manifest.IncludeCalls);
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task IncludeExclude_IncludeExistingEntryAsync_RejectsInvalidIndex()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/part.md"),
            ("part-one/part.md", "{class: part}\n# Part One"),
            ("part-one/chapter-one.md", "# Chapter One"));

        try
        {
            var manifest = new RecordingManifestService();
            var service = CreateService(exclusionManifestService: manifest);

            var result = await service.IncludeExistingEntryAsync(workspace.BookTxtPath, "part-one/chapter-one.md", 2);

            Assert.False(result.IsSuccess);
            Assert.Contains("out of range", result.Error);
            Assert.Equal(0, manifest.IncludeCalls);
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task IncludeExclude_IncludeExistingEntryAfterPartAsync_RejectsMissingPartPath()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/part.md"),
            ("part-one/part.md", "{class: part}\n# Part One"),
            ("part-one/chapter-one.md", "# Chapter One"));

        try
        {
            var manifest = new RecordingManifestService();
            var service = CreateService(exclusionManifestService: manifest);

            var result = await service.IncludeExistingEntryAfterPartAsync(
                workspace.BookTxtPath,
                "part-one/chapter-one.md",
                "part-two/part.md");

            Assert.False(result.IsSuccess);
            Assert.Contains("Part entry 'part-two/part.md' was not found", result.Error);
            Assert.Equal(0, manifest.IncludeCalls);
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task IncludeExclude_ExcludeEntryAsync_RejectsPathOutsideWorkspace()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/part.md"),
            ("part-one/part.md", "{class: part}\n# Part One"));

        try
        {
            var manifest = new RecordingManifestService();
            var service = CreateService(exclusionManifestService: manifest);

            var result = await service.ExcludeEntryAsync(workspace.BookTxtPath, "../escape.md");

            Assert.False(result.IsSuccess);
            Assert.Contains("outside the manuscript root", result.Error);
            Assert.Equal(0, manifest.ExcludeCalls);
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task IncludeExclude_ExcludeEntryAsync_PrunesStaleManifestPathsWhenSaving()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/part.md\npart-one/chapter-one.md"),
            ("part-one/part.md", "{class: part}\n# Part One"),
            ("part-one/chapter-one.md", "# Chapter One"));

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ManifestPath(workspace.Root))!);
            await File.WriteAllTextAsync(
                ManifestPath(workspace.Root),
                "{\"schemaVersion\":1,\"excludedPaths\":[\"part-one/stale.md\"]}");
            var service = CreateService();

            var result = await service.ExcludeEntryAsync(workspace.BookTxtPath, "part-one/chapter-one.md");

            Assert.True(result.IsSuccess, result.Error);
            Assert.Equal(new[] { "part-one/chapter-one.md" }, await LoadExcludedPathsAsync(workspace.Root));
            Assert.DoesNotContain("stale.md", await File.ReadAllTextAsync(ManifestPath(workspace.Root)));
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task IncludeExclude_IncludeExistingEntryAsync_PrunesStaleManifestPathsWhenSaving()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/part.md"),
            ("part-one/part.md", "{class: part}\n# Part One"),
            ("part-one/chapter-one.md", "# Chapter One"));

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ManifestPath(workspace.Root))!);
            await File.WriteAllTextAsync(
                ManifestPath(workspace.Root),
                "{\"schemaVersion\":1,\"excludedPaths\":[\"part-one/chapter-one.md\",\"part-one/stale.md\"]}");
            var service = CreateService();

            var result = await service.IncludeExistingEntryAsync(workspace.BookTxtPath, "part-one/chapter-one.md", 1);

            Assert.True(result.IsSuccess, result.Error);
            Assert.Empty(await LoadExcludedPathsAsync(workspace.Root));
            Assert.DoesNotContain("stale.md", await File.ReadAllTextAsync(ManifestPath(workspace.Root)));
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task IncludeExclude_ExcludeEntryAsync_DoesNotUpdateManifestWhenBookTxtWriteFails()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/part.md\npart-one/chapter-one.md"),
            ("part-one/part.md", "{class: part}\n# Part One"),
            ("part-one/chapter-one.md", "# Chapter One"));

        try
        {
            var manifest = new RecordingManifestService();
            var service = CreateService(new ThrowingMetadataStore(), manifest);

            var result = await service.ExcludeEntryAsync(workspace.BookTxtPath, "part-one/chapter-one.md");

            Assert.False(result.IsSuccess);
            Assert.Contains("Book.txt", result.Error);
            Assert.Contains("Book.txt write or validation", result.Error);
            Assert.Equal(0, manifest.ExcludeCalls);
            Assert.Equal(new[] { "part-one/part.md", "part-one/chapter-one.md" }, ReadBookTxtLines(workspace.BookTxtPath));
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task IncludeExclude_ExcludeEntryAsync_ReturnsManifestPhaseFailureAfterBookTxtWrite()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/part.md\npart-one/chapter-one.md"),
            ("part-one/part.md", "{class: part}\n# Part One"),
            ("part-one/chapter-one.md", "# Chapter One"));

        try
        {
            var service = CreateService(exclusionManifestService: new FailingManifestService());

            var result = await service.ExcludeEntryAsync(workspace.BookTxtPath, "part-one/chapter-one.md");

            Assert.False(result.IsSuccess);
            Assert.Contains("manifest save after Book.txt write", result.Error);
            Assert.Equal(new[] { "part-one/part.md" }, ReadBookTxtLines(workspace.BookTxtPath));
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task IncludeExclude_IncludeExistingEntryAsync_ReturnsManifestPhaseFailureAfterBookTxtWrite()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/part.md"),
            ("part-one/part.md", "{class: part}\n# Part One"),
            ("part-one/chapter-one.md", "# Chapter One"));

        try
        {
            var service = CreateService(exclusionManifestService: new FailingManifestService());

            var result = await service.IncludeExistingEntryAsync(workspace.BookTxtPath, "part-one/chapter-one.md", 1);

            Assert.False(result.IsSuccess);
            Assert.Contains("manifest save after Book.txt write", result.Error);
            Assert.Equal(new[] { "part-one/part.md", "part-one/chapter-one.md" }, ReadBookTxtLines(workspace.BookTxtPath));
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task PathMove_MoveEntryAsync_MovesAcrossParts_UpdatesBookTxtRegistryAndManifest()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/part.md\npart-one/chapter-one.md\npart-two/part.md"),
            ("part-one/part.md", "{class: part}\n# Part One"),
            ("part-one/chapter-one.md", "# Chapter One"),
            ("part-two/part.md", "{class: part}\n# Part Two"));

        try
        {
            const string uuid = "chapter-uuid-1";
            await SaveRegistryAsync(workspace.Root, new ChapterRegistryEntry
            {
                Uuid = uuid,
                CurrentPath = "part-one/chapter-one.md",
                Orphaned = false,
                Title = "Chapter One"
            });
            Directory.CreateDirectory(Path.GetDirectoryName(ManifestPath(workspace.Root))!);
            await File.WriteAllTextAsync(
                ManifestPath(workspace.Root),
                "{\"schemaVersion\":1,\"excludedPaths\":[\"part-two/chapter-one.md\"]}");
            var service = CreateService();

            var result = await service.MoveEntryAsync(
                workspace.BookTxtPath,
                "part-one/chapter-one.md",
                "part-two/chapter-one.md",
                2);

            Assert.True(result.IsSuccess, result.Error);
            Assert.False(File.Exists(AbsolutePath(workspace.Root, "part-one/chapter-one.md")));
            Assert.True(File.Exists(AbsolutePath(workspace.Root, "part-two/chapter-one.md")));
            Assert.Equal(new[]
            {
                "part-one/part.md",
                "part-two/part.md",
                "part-two/chapter-one.md"
            }, ReadBookTxtLines(workspace.BookTxtPath));

            var registry = await LoadRegistryAsync(workspace.Root);
            Assert.True(registry.ContainsKey(uuid));
            Assert.Equal("part-two/chapter-one.md", registry[uuid].CurrentPath);
            Assert.Equal("Chapter One", registry[uuid].Title);
            Assert.Empty(await LoadExcludedPathsAsync(workspace.Root));
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task PathMove_MoveEntryAsync_TargetConflictFailsWithoutWrites()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/chapter-one.md\npart-two/chapter-one.md"),
            ("part-one/chapter-one.md", "# Chapter One"),
            ("part-two/chapter-one.md", "# Existing Target"));

        try
        {
            var originalBookTxt = ReadBookTxt(workspace.BookTxtPath);
            await SaveRegistryAsync(workspace.Root, new ChapterRegistryEntry
            {
                Uuid = "chapter-uuid-1",
                CurrentPath = "part-one/chapter-one.md"
            });
            var service = CreateService();

            var result = await service.MoveEntryAsync(
                workspace.BookTxtPath,
                "part-one/chapter-one.md",
                "part-two/chapter-one.md",
                1);

            Assert.False(result.IsSuccess);
            Assert.Contains("target entry already exists", result.Error);
            Assert.Equal(originalBookTxt, ReadBookTxt(workspace.BookTxtPath));
            Assert.True(File.Exists(AbsolutePath(workspace.Root, "part-one/chapter-one.md")));
            Assert.Equal("# Existing Target", File.ReadAllText(AbsolutePath(workspace.Root, "part-two/chapter-one.md")));
            var registry = await LoadRegistryAsync(workspace.Root);
            Assert.Equal("part-one/chapter-one.md", registry["chapter-uuid-1"].CurrentPath);
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task PathMove_MoveEntryAsync_BookTxtWriteFailureRollsFileBackAndReportsPhase()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/chapter-one.md\npart-two/part.md"),
            ("part-one/chapter-one.md", "# Chapter One"),
            ("part-two/part.md", "{class: part}\n# Part Two"));

        try
        {
            var originalBookTxt = ReadBookTxt(workspace.BookTxtPath);
            await SaveRegistryAsync(workspace.Root, new ChapterRegistryEntry
            {
                Uuid = "chapter-uuid-1",
                CurrentPath = "part-one/chapter-one.md"
            });
            var service = CreateService(new PathAwareThrowingMetadataStore(path => path.EndsWith("Book.txt", StringComparison.OrdinalIgnoreCase)));

            var result = await service.MoveEntryAsync(
                workspace.BookTxtPath,
                "part-one/chapter-one.md",
                "part-two/chapter-one.md",
                1);

            Assert.False(result.IsSuccess);
            Assert.Contains("Book.txt write", result.Error);
            Assert.Contains("rollback attempted and manuscript state was restored", result.Error);
            Assert.True(File.Exists(AbsolutePath(workspace.Root, "part-one/chapter-one.md")));
            Assert.False(File.Exists(AbsolutePath(workspace.Root, "part-two/chapter-one.md")));
            Assert.Equal(originalBookTxt, ReadBookTxt(workspace.BookTxtPath));
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task PathMove_MoveEntryAsync_BookTxtWriteFailureReportsRollbackFailure()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/chapter-one.md\npart-two/part.md"),
            ("part-one/chapter-one.md", "# Chapter One"),
            ("part-two/part.md", "{class: part}\n# Part Two"));

        try
        {
            await SaveRegistryAsync(workspace.Root, new ChapterRegistryEntry
            {
                Uuid = "chapter-uuid-1",
                CurrentPath = "part-one/chapter-one.md"
            });
            var sourceAbsolutePath = AbsolutePath(workspace.Root, "part-one/chapter-one.md");
            var service = CreateService(new PathAwareThrowingMetadataStore(
                path => path.EndsWith("Book.txt", StringComparison.OrdinalIgnoreCase),
                beforeThrow: path => Directory.CreateDirectory(sourceAbsolutePath)));

            var result = await service.MoveEntryAsync(
                workspace.BookTxtPath,
                "part-one/chapter-one.md",
                "part-two/chapter-one.md",
                1);

            Assert.False(result.IsSuccess);
            Assert.Contains("Book.txt write", result.Error);
            Assert.Contains("rollback attempted but rollback failed", result.Error);
            Assert.Contains("part-one/chapter-one.md", result.Error);
            Assert.Contains("part-two/chapter-one.md", result.Error);
            Assert.True(Directory.Exists(sourceAbsolutePath));
            Assert.True(File.Exists(AbsolutePath(workspace.Root, "part-two/chapter-one.md")));
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task PathMove_MoveEntryAsync_RegistrySaveFailureRestoresBookTxtAndFile()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/chapter-one.md\npart-two/part.md"),
            ("part-one/chapter-one.md", "# Chapter One"),
            ("part-two/part.md", "{class: part}\n# Part Two"));

        try
        {
            var originalBookTxt = ReadBookTxt(workspace.BookTxtPath);
            await SaveRegistryAsync(workspace.Root, new ChapterRegistryEntry
            {
                Uuid = "chapter-uuid-1",
                CurrentPath = "part-one/chapter-one.md"
            });
            var service = CreateService(new PathAwareThrowingMetadataStore(
                path => path.EndsWith("chapter-registry.json", StringComparison.OrdinalIgnoreCase)));

            var result = await service.MoveEntryAsync(
                workspace.BookTxtPath,
                "part-one/chapter-one.md",
                "part-two/chapter-one.md",
                1);

            Assert.False(result.IsSuccess);
            Assert.Contains("registry update", result.Error);
            Assert.Contains("rollback attempted and manuscript state was restored", result.Error);
            Assert.True(File.Exists(AbsolutePath(workspace.Root, "part-one/chapter-one.md")));
            Assert.False(File.Exists(AbsolutePath(workspace.Root, "part-two/chapter-one.md")));
            Assert.Equal(new[] { "part-one/chapter-one.md", "part-two/part.md" }, ReadBookTxtLines(workspace.BookTxtPath));
            var registry = await LoadRegistryAsync(workspace.Root);
            Assert.Equal("part-one/chapter-one.md", registry["chapter-uuid-1"].CurrentPath);
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task PathMove_MoveEntryAsync_AmbiguousRegistryIdentityFailsBeforeFileMove()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/chapter-one.md"),
            ("part-one/chapter-one.md", "# Chapter One"));

        try
        {
            await SaveRegistryAsync(
                workspace.Root,
                new ChapterRegistryEntry { Uuid = "chapter-uuid-1", CurrentPath = "part-one/chapter-one.md" },
                new ChapterRegistryEntry { Uuid = "chapter-uuid-2", CurrentPath = "part-one/chapter-one.md" });
            var originalBookTxt = ReadBookTxt(workspace.BookTxtPath);
            var service = CreateService();

            var result = await service.MoveEntryAsync(
                workspace.BookTxtPath,
                "part-one/chapter-one.md",
                "part-two/chapter-one.md",
                0);

            Assert.False(result.IsSuccess);
            Assert.Contains("registry validation", result.Error);
            Assert.Contains("ambiguous", result.Error);
            Assert.True(File.Exists(AbsolutePath(workspace.Root, "part-one/chapter-one.md")));
            Assert.False(File.Exists(AbsolutePath(workspace.Root, "part-two/chapter-one.md")));
            Assert.Equal(originalBookTxt, ReadBookTxt(workspace.BookTxtPath));
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task PathMove_MoveEntryAsync_RejectsInvalidReplacementPath()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/chapter-one.md"),
            ("part-one/chapter-one.md", "# Chapter One"));

        try
        {
            var service = CreateService();

            var result = await service.MoveEntryAsync(
                workspace.BookTxtPath,
                "part-one/chapter-one.md",
                "../escape.md",
                0);

            Assert.False(result.IsSuccess);
            Assert.Contains("outside the manuscript root", result.Error);
            Assert.True(File.Exists(AbsolutePath(workspace.Root, "part-one/chapter-one.md")));
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    [Fact]
    public async Task PathMove_MoveEntryAsync_RejectsMissingSourceFile()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/chapter-one.md"));

        try
        {
            var service = CreateService();

            var result = await service.MoveEntryAsync(
                workspace.BookTxtPath,
                "part-one/chapter-one.md",
                "part-two/chapter-one.md",
                0);

            Assert.False(result.IsSuccess);
            Assert.Contains("source file", result.Error);
            Assert.Contains("does not exist", result.Error);
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

    [Fact]
    public async Task CreateNewPartAsync_CreatesPartFileAndInsertsIntoBookTxt()
    {
        var workspace = CreateWorkspace(
            ("Book.txt", "part-one/part.md\npart-one/chapter-one.md"),
            ("part-one/part.md", "{class: part}\n# Part One"),
            ("part-one/chapter-one.md", "# Chapter One"));

        try
        {
            var service = CreateService();

            var result = await service.CreateNewPartAsync(
                workspace.BookTxtPath,
                "part-two/part.md",
                "Part Two",
                index: 2);

            Assert.True(result.IsSuccess, result.Error);
            Assert.Equal(new[]
            {
                "part-one/part.md",
                "part-one/chapter-one.md",
                "part-two/part.md"
            }, ReadBookTxtLines(workspace.BookTxtPath));

            var partPath = Path.Combine(workspace.Root, "part-two", "part.md");
            Assert.True(File.Exists(partPath));
            var content = File.ReadAllText(partPath);
            Assert.Contains("{class: part}", content);
            Assert.Contains("# Part Two", content);
        }
        finally
        {
            Directory.Delete(workspace.Root, recursive: true);
        }
    }

    private sealed class RecordingManifestService : IExclusionManifestService
    {
        public int IncludeCalls { get; private set; }

        public int ExcludeCalls { get; private set; }

        public Task<Result<ExclusionManifest>> LoadAsync(string workspaceRoot)
            => Task.FromResult(Result<ExclusionManifest>.Ok(new ExclusionManifest()));

        public Task<Result<ExclusionManifest>> SaveAsync(string workspaceRoot, ExclusionManifest manifest)
            => Task.FromResult(Result<ExclusionManifest>.Ok(manifest));

        public Task<Result<ExclusionManifest>> ExcludeAsync(string workspaceRoot, string relativePath)
        {
            ExcludeCalls++;
            return Task.FromResult(Result<ExclusionManifest>.Ok(new ExclusionManifest
            {
                ExcludedPaths = new[] { relativePath.Replace('\\', '/') }
            }));
        }

        public Task<Result<ExclusionManifest>> IncludeAsync(string workspaceRoot, string relativePath)
        {
            IncludeCalls++;
            return Task.FromResult(Result<ExclusionManifest>.Ok(new ExclusionManifest()));
        }
    }

    private sealed class FailingManifestService : IExclusionManifestService
    {
        public Task<Result<ExclusionManifest>> LoadAsync(string workspaceRoot)
            => Task.FromResult(Result<ExclusionManifest>.Fail("simulated manifest load failure"));

        public Task<Result<ExclusionManifest>> SaveAsync(string workspaceRoot, ExclusionManifest manifest)
            => Task.FromResult(Result<ExclusionManifest>.Fail("simulated manifest save failure"));

        public Task<Result<ExclusionManifest>> ExcludeAsync(string workspaceRoot, string relativePath)
            => Task.FromResult(Result<ExclusionManifest>.Fail("simulated manifest save failure"));

        public Task<Result<ExclusionManifest>> IncludeAsync(string workspaceRoot, string relativePath)
            => Task.FromResult(Result<ExclusionManifest>.Fail("simulated manifest save failure"));
    }

    private sealed class PathAwareThrowingMetadataStore : IMetadataStore
    {
        private readonly Func<string, bool> _shouldThrow;
        private readonly Action<string>? _beforeThrow;
        private readonly MetadataStore _inner = new();

        public PathAwareThrowingMetadataStore(Func<string, bool> shouldThrow, Action<string>? beforeThrow = null)
        {
            _shouldThrow = shouldThrow;
            _beforeThrow = beforeThrow;
        }

        public Task WriteTextAtomicAsync(string absolutePath, string content)
        {
            if (_shouldThrow(absolutePath))
            {
                _beforeThrow?.Invoke(absolutePath);
                throw new IOException($"simulated atomic write failure for {Path.GetFileName(absolutePath)}");
            }

            return _inner.WriteTextAtomicAsync(absolutePath, content);
        }
    }

    private sealed class ThrowingMetadataStore : IMetadataStore
    {
        public Task WriteTextAtomicAsync(string absolutePath, string content)
            => throw new IOException("simulated atomic write failure");
    }
}
