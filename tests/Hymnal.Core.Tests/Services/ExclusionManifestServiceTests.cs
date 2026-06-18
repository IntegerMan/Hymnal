using System.Text.Json;
using Hymnal.Core.Infrastructure;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models;
using Hymnal.Core.Services;

namespace Hymnal.Core.Tests.Services;

public sealed class ExclusionManifestServiceTests
{
    private static string CreateWorkspace(params (string Path, string Content)[] files)
    {
        var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(root);

        foreach (var (relativePath, content) in files)
        {
            WriteWorkspaceFile(root, relativePath, content);
        }

        return root;
    }

    private static void WriteWorkspaceFile(string root, string relativePath, string content)
    {
        var absolutePath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var directory = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(absolutePath, content);
    }

    private static async Task WriteManifestAsync(string root, params string[] paths)
    {
        var manifestPath = Path.Combine(root, ".hymnal-data", "exclusions.json");
        Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);
        var json = JsonSerializer.Serialize(new ExclusionManifest { ExcludedPaths = paths }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await File.WriteAllTextAsync(manifestPath, json);
    }

    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsManifest()
    {
        var root = CreateWorkspace(("chapter-one.md", "# One"), ("part/chapter-two.md", "# Two"));
        try
        {
            var sut = new ExclusionManifestService(new MetadataStore());

            var saved = await sut.SaveAsync(root, new ExclusionManifest
            {
                ExcludedPaths = new[] { "chapter-one.md", "part/chapter-two.md" }
            });
            var loaded = await sut.LoadAsync(root);

            Assert.True(saved.IsSuccess, saved.Error);
            Assert.True(loaded.IsSuccess, loaded.Error);
            Assert.Equal(new[] { "chapter-one.md", "part/chapter-two.md" }, loaded.Value!.ExcludedPaths);
            Assert.True(File.Exists(Path.Combine(root, ".hymnal-data", "exclusions.json")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task LoadAsync_MissingManifest_ReturnsEmptyManifest()
    {
        var root = CreateWorkspace(("chapter-one.md", "# One"));
        try
        {
            var sut = new ExclusionManifestService(new MetadataStore());

            var result = await sut.LoadAsync(root);

            Assert.True(result.IsSuccess, result.Error);
            Assert.Empty(result.Value!.ExcludedPaths);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_NormalizesAndDeduplicatesCaseVariants()
    {
        var root = CreateWorkspace(("Part/Chapter.md", "# Chapter"));
        try
        {
            var sut = new ExclusionManifestService(new MetadataStore());

            var result = await sut.SaveAsync(root, new ExclusionManifest
            {
                ExcludedPaths = new[] { "Part\\Chapter.md", "part/chapter.md" }
            });

            Assert.True(result.IsSuccess, result.Error);
            var only = Assert.Single(result.Value!.ExcludedPaths);
            Assert.Equal("Part/Chapter.md", only);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task LoadAsync_OmitsStaleEntriesWithoutRewritingManifest()
    {
        var root = CreateWorkspace(("existing.md", "# Existing"));
        try
        {
            await WriteManifestAsync(root, "existing.md", "missing.md");
            var manifestPath = Path.Combine(root, ".hymnal-data", "exclusions.json");
            var before = await File.ReadAllTextAsync(manifestPath);
            var sut = new ExclusionManifestService(new MetadataStore());

            var result = await sut.LoadAsync(root);
            var after = await File.ReadAllTextAsync(manifestPath);

            Assert.True(result.IsSuccess, result.Error);
            Assert.Equal(new[] { "existing.md" }, result.Value!.ExcludedPaths);
            Assert.Equal(before, after);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExcludeAsync_PrunesStaleEntriesWhenSavingMutation()
    {
        var root = CreateWorkspace(("existing.md", "# Existing"), ("new.md", "# New"));
        try
        {
            await WriteManifestAsync(root, "existing.md", "missing.md");
            var sut = new ExclusionManifestService(new MetadataStore());

            var result = await sut.ExcludeAsync(root, "new.md");
            var reloaded = await sut.LoadAsync(root);

            Assert.True(result.IsSuccess, result.Error);
            Assert.Equal(new[] { "existing.md", "new.md" }, result.Value!.ExcludedPaths);
            Assert.Equal(new[] { "existing.md", "new.md" }, reloaded.Value!.ExcludedPaths);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task LoadAsync_EmptyManifest_ReturnsEmptyManifest()
    {
        var root = CreateWorkspace(("chapter-one.md", "# One"));
        try
        {
            await WriteManifestAsync(root);
            var sut = new ExclusionManifestService(new MetadataStore());

            var result = await sut.LoadAsync(root);

            Assert.True(result.IsSuccess, result.Error);
            Assert.Empty(result.Value!.ExcludedPaths);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task IncludeAsync_RemovesPathCaseInsensitively()
    {
        var root = CreateWorkspace(("Part/Chapter.md", "# Chapter"));
        try
        {
            await WriteManifestAsync(root, "Part/Chapter.md");
            var sut = new ExclusionManifestService(new MetadataStore());

            var result = await sut.IncludeAsync(root, "part/chapter.md");

            Assert.True(result.IsSuccess, result.Error);
            Assert.Empty(result.Value!.ExcludedPaths);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task LoadAsync_MalformedJson_ReturnsRecoverableFailure()
    {
        var root = CreateWorkspace(("chapter.md", "# Chapter"));
        try
        {
            var manifestPath = Path.Combine(root, ".hymnal-data", "exclusions.json");
            Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);
            await File.WriteAllTextAsync(manifestPath, "{ not json");
            var sut = new ExclusionManifestService(new MetadataStore());

            var result = await sut.LoadAsync(root);

            Assert.False(result.IsSuccess);
            Assert.Contains("Manifest load failed", result.Error);
            Assert.Contains("JSON", result.Error);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData("/absolute/chapter.md")]
    [InlineData("C:/absolute/chapter.md")]
    [InlineData("../outside.md")]
    [InlineData("part/../outside.md")]
    public async Task ExcludeAsync_InvalidPath_ReturnsFailure(string invalidPath)
    {
        var root = CreateWorkspace(("chapter.md", "# Chapter"));
        try
        {
            var sut = new ExclusionManifestService(new MetadataStore());

            var result = await sut.ExcludeAsync(root, invalidPath);

            Assert.False(result.IsSuccess);
            Assert.Contains("path validation", result.Error);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_InvalidManifestPath_ReturnsFailure()
    {
        var root = CreateWorkspace(("chapter.md", "# Chapter"));
        try
        {
            var sut = new ExclusionManifestService(new MetadataStore());

            var result = await sut.SaveAsync(root, new ExclusionManifest { ExcludedPaths = new[] { "../outside.md" } });

            Assert.False(result.IsSuccess);
            Assert.Contains("Manifest save", result.Error);
            Assert.Contains("path validation", result.Error);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_AtomicWriteFailure_ReturnsFailureAndDoesNotMutateCallerManifest()
    {
        var root = CreateWorkspace(("existing.md", "# Existing"));
        try
        {
            var input = new ExclusionManifest { ExcludedPaths = new[] { "existing.md", "missing.md" } };
            var sut = new ExclusionManifestService(new FailingMetadataStore());

            var result = await sut.SaveAsync(root, input);

            Assert.False(result.IsSuccess);
            Assert.Contains("Manifest save failed", result.Error);
            Assert.Contains("atomic write", result.Error);
            Assert.Equal(new[] { "existing.md", "missing.md" }, input.ExcludedPaths);
            Assert.False(File.Exists(Path.Combine(root, ".hymnal-data", "exclusions.json")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task OrphanDiscovery_RemainsIndependentOfIntentionalExclusions()
    {
        var root = CreateWorkspace(
            ("Book.txt", "registered.md"),
            ("registered.md", "# Registered"),
            ("orphan.md", "# Orphan"));
        try
        {
            var manifestService = new ExclusionManifestService(new MetadataStore());
            var excluded = await manifestService.ExcludeAsync(root, "orphan.md");
            Assert.True(excluded.IsSuccess, excluded.Error);

            var orphanDiscovery = new OrphanFileDiscoveryService();
            var orphans = await orphanDiscovery.DiscoverAsync(root, new[] { "registered.md" });

            var orphan = Assert.Single(orphans);
            Assert.Equal("orphan.md", orphan.RelativePath);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class FailingMetadataStore : IMetadataStore
    {
        public Task WriteTextAtomicAsync(string absolutePath, string content) =>
            throw new IOException("simulated manifest save failure");
    }
}
