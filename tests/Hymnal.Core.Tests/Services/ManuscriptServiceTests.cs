using Hymnal.Core.Interfaces;
using Hymnal.Core.Services;

namespace Hymnal.Core.Tests.Services;

public class ManuscriptServiceTests
{
    private static string FixturesRoot =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "SampleManuscripts");

    private sealed class FakeNotificationService : INotificationService
    {
        public List<string> InfoMessages { get; } = new();
        public void ShowError(string message) { }
        public void ShowInfo(string message) => InfoMessages.Add(message);
        public void ShowSuccess(string message) { }
    }

    [Fact]
    public async Task LoadWorkspaceAsync_SimpleBook_ReturnsSuccessWithOneChapter()
    {
        var svc = new ManuscriptService(new FakeNotificationService());
        var folderPath = Path.Combine(FixturesRoot, "simple-book");

        var result = await svc.LoadWorkspaceAsync(folderPath);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value!.Nodes.Count);
        svc.Dispose();
    }

    [Fact]
    public async Task LoadWorkspaceAsync_MissingBookTxt_ReturnsFailure()
    {
        var svc = new ManuscriptService(new FakeNotificationService());
        var folderPath = Path.Combine(FixturesRoot, "nonexistent-folder");

        var result = await svc.LoadWorkspaceAsync(folderPath);

        Assert.False(result.IsSuccess);
        Assert.Contains("Book.txt not found", result.Error);
        svc.Dispose();
    }

    [Fact]
    public async Task LoadWorkspaceAsync_MissingEntryFile_NodeIsMarkedMissing()
    {
        var svc = new ManuscriptService(new FakeNotificationService());
        var folderPath = Path.Combine(FixturesRoot, "multi-part-book");

        var result = await svc.LoadWorkspaceAsync(folderPath);

        Assert.True(result.IsSuccess);
        var nodes = result.Value!.Nodes.Items.ToList();
        var chapterOne = nodes.FirstOrDefault(n => n.RelativePath.Contains("chapter-one"));
        Assert.NotNull(chapterOne);
        Assert.False(chapterOne!.IsMissing);
        svc.Dispose();
    }
}
