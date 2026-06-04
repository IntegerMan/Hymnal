using Hymnal.Core.Infrastructure;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models;
using Hymnal.Core.Services;

namespace Hymnal.Core.Tests.Services;

public class SupplementalDocsServiceTests
{
    private static string CreateWorkspace()
    {
        var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(root);
        return root;
    }

    private static string DocsRoot(string workspaceRoot)
        => Path.Combine(workspaceRoot, ".hymnal-data", "docs");

    private static string DocPath(string workspaceRoot, params string[] segments)
        => Path.Combine(new[] { DocsRoot(workspaceRoot) }.Concat(segments).ToArray());

    private static SupplementalDocsService CreateService(IMetadataStore? metadataStore = null)
        => new(metadataStore ?? new MetadataStore());

    [Fact]
    public async Task LoadTreeAsync_CreatesEmptyDocsRoot()
    {
        var workspace = CreateWorkspace();

        try
        {
            var service = CreateService();

            var result = await service.LoadTreeAsync(workspace);

            Assert.True(result.IsSuccess, result.Error);
            Assert.Empty(result.Value!);
            Assert.True(Directory.Exists(DocsRoot(workspace)));
        }
        finally
        {
            Directory.Delete(workspace, recursive: true);
        }
    }

    [Fact]
    public async Task LoadTreeAsync_ProjectsNestedFoldersFirstThenFilesInCaseInsensitiveOrder()
    {
        var workspace = CreateWorkspace();

        try
        {
            Directory.CreateDirectory(DocPath(workspace, "z-folder"));
            Directory.CreateDirectory(DocPath(workspace, "A-folder", "nested"));
            await File.WriteAllTextAsync(DocPath(workspace, "beta.md"), "Beta");
            await File.WriteAllTextAsync(DocPath(workspace, "Alpha.md"), "Alpha");
            await File.WriteAllTextAsync(DocPath(workspace, "A-folder", "nested", "note.md"), "Note");

            var service = CreateService();

            var result = await service.LoadTreeAsync(workspace);

            Assert.True(result.IsSuccess, result.Error);
            var nodes = result.Value!;
            Assert.Equal(new[] { "A-folder", "z-folder", "Alpha.md", "beta.md" }, nodes.Select(node => node.DisplayName));
            Assert.Equal(SupplementalDocNodeKind.Folder, nodes[0].Kind);
            Assert.Equal("A-folder", nodes[0].RelativePath);
            Assert.Equal("A-folder", nodes[0].Key);
            Assert.Single(nodes[0].Children);
            Assert.Equal("A-folder/nested", nodes[0].Children[0].RelativePath);
            Assert.Equal("A-folder/nested/note.md", nodes[0].Children[0].Children.Single().RelativePath);
            Assert.Equal(SupplementalDocNodeKind.File, nodes[2].Kind);
            Assert.Empty(nodes[2].Children);
        }
        finally
        {
            Directory.Delete(workspace, recursive: true);
        }
    }

    [Fact]
    public async Task CreateFolderAsync_CreatesNestedParentAndReturnsProjectedFolder()
    {
        var workspace = CreateWorkspace();

        try
        {
            var service = CreateService();

            var result = await service.CreateFolderAsync(workspace, "Research/Interviews", "Round One");

            Assert.True(result.IsSuccess, result.Error);
            Assert.True(Directory.Exists(DocPath(workspace, "Research", "Interviews", "Round One")));
            Assert.Equal("Round One", result.Value!.DisplayName);
            Assert.Equal("Research/Interviews/Round One", result.Value.RelativePath);
            Assert.Equal(SupplementalDocNodeKind.Folder, result.Value.Kind);
        }
        finally
        {
            Directory.Delete(workspace, recursive: true);
        }
    }

    [Fact]
    public async Task CreateFileAsync_WritesInitialContentThroughMetadataStore()
    {
        var workspace = CreateWorkspace();

        try
        {
            var metadataStore = new RecordingMetadataStore();
            var service = CreateService(metadataStore);

            var result = await service.CreateFileAsync(workspace, "Research", "brief.md", "# Brief");

            Assert.True(result.IsSuccess, result.Error);
            Assert.Equal(DocPath(workspace, "Research", "brief.md"), metadataStore.AbsolutePath);
            Assert.Equal("# Brief", metadataStore.Content);
            Assert.True(File.Exists(DocPath(workspace, "Research", "brief.md")));
            Assert.Equal("# Brief", await File.ReadAllTextAsync(DocPath(workspace, "Research", "brief.md")));
            Assert.Equal("Research/brief.md", result.Value!.RelativePath);
            Assert.Equal(SupplementalDocNodeKind.File, result.Value.Kind);
        }
        finally
        {
            Directory.Delete(workspace, recursive: true);
        }
    }

    [Theory]
    [InlineData("..", "escape.md")]
    [InlineData("Research/../Escape", "escape.md")]
    [InlineData(null, "../escape.md")]
    [InlineData(null, "/escape.md")]
    public async Task CreateOperations_RejectTraversalAndRootedPaths(string? parentRelativePath, string name)
    {
        var workspace = CreateWorkspace();

        try
        {
            var service = CreateService();

            var fileResult = await service.CreateFileAsync(workspace, parentRelativePath, name, "escape");
            var folderResult = await service.CreateFolderAsync(workspace, parentRelativePath, name);

            Assert.False(fileResult.IsSuccess);
            Assert.False(folderResult.IsSuccess);
            Assert.False(File.Exists(Path.Combine(workspace, "escape.md")));
            Assert.False(Directory.Exists(Path.Combine(workspace, "escape.md")));
        }
        finally
        {
            Directory.Delete(workspace, recursive: true);
        }
    }

    [Fact]
    public async Task CreateFileAsync_ReloadShowsFileAndPreservesContent()
    {
        var workspace = CreateWorkspace();

        try
        {
            var service = CreateService();
            var create = await service.CreateFileAsync(workspace, "Worldbuilding", "places.md", "Harbor notes");

            Assert.True(create.IsSuccess, create.Error);

            var reload = await service.LoadTreeAsync(workspace);

            Assert.True(reload.IsSuccess, reload.Error);
            var folder = Assert.Single(reload.Value!);
            Assert.Equal("Worldbuilding", folder.DisplayName);
            var file = Assert.Single(folder.Children);
            Assert.Equal("Worldbuilding/places.md", file.RelativePath);
            Assert.Equal("Harbor notes", await File.ReadAllTextAsync(DocPath(workspace, "Worldbuilding", "places.md")));
        }
        finally
        {
            Directory.Delete(workspace, recursive: true);
        }
    }

    private sealed class RecordingMetadataStore : IMetadataStore
    {
        private readonly MetadataStore _inner = new();

        public string? AbsolutePath { get; private set; }
        public string? Content { get; private set; }

        public async Task WriteTextAtomicAsync(string absolutePath, string content)
        {
            AbsolutePath = absolutePath;
            Content = content;
            await _inner.WriteTextAtomicAsync(absolutePath, content).ConfigureAwait(false);
        }
    }
}
