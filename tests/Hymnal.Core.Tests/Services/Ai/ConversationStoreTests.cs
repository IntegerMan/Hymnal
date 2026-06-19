using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hymnal.Core.Infrastructure;
using Hymnal.Core.Models.Ai;
using Hymnal.Core.Services.Ai;
using Xunit;

namespace Hymnal.Core.Tests.Services.Ai;

public class ConversationStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"hymnal-test-{Guid.NewGuid()}");
    private readonly MetadataStore _metaStore;
    private readonly ConversationStore _sut;

    public ConversationStoreTests()
    {
        Directory.CreateDirectory(_dir);
        _metaStore = new MetadataStore();
        _sut = new ConversationStore(_metaStore);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Conversation MakeConversation(string? id = null, string title = "Test Conv") =>
        new(
            Id: id ?? Guid.NewGuid().ToString(),
            Title: title,
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow,
            Archived: false,
            Role: "WritingCoach",
            ContextTag: new ContextTag("Write", "Chapter", null, null),
            Messages: new List<ConversationMessage>());

    private static ConversationMessage MakeMessage(string content) =>
        new(Guid.NewGuid().ToString(), "user", content, DateTimeOffset.UtcNow, null);

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveConversation_Then_LoadConversation_RoundTrip()
    {
        var conv = MakeConversation();
        await _sut.SaveConversationAsync(_dir, conv);

        var loaded = await _sut.LoadConversationAsync(_dir, conv.Id);

        Assert.NotNull(loaded);
        Assert.Equal(conv.Id, loaded!.Id);
        Assert.Equal(conv.Title, loaded.Title);
    }

    [Fact]
    public async Task SaveConversation_UpdatesIndex()
    {
        var conv = MakeConversation(title: "My Chapter Chat");
        await _sut.SaveConversationAsync(_dir, conv);

        var index = await _sut.LoadIndexAsync(_dir);

        Assert.Single(index);
        Assert.Equal(conv.Id, index[0].Id);
        Assert.Equal("My Chapter Chat", index[0].Title);
    }

    [Fact]
    public async Task LoadConversation_NonExistentId_ReturnsNull()
    {
        var result = await _sut.LoadConversationAsync(_dir, Guid.NewGuid().ToString());
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteConversation_RemovesFileAndIndex()
    {
        var conv = MakeConversation();
        await _sut.SaveConversationAsync(_dir, conv);

        await _sut.DeleteConversationAsync(_dir, conv.Id);

        var loaded = await _sut.LoadConversationAsync(_dir, conv.Id);
        var index = await _sut.LoadIndexAsync(_dir);

        Assert.Null(loaded);
        Assert.Empty(index);
    }

    [Fact]
    public async Task SaveConversation_WithMessages_PreservesMessages()
    {
        var msg = MakeMessage("Hello world");
        var conv = MakeConversation() with
        {
            Messages = new List<ConversationMessage> { msg }
        };

        await _sut.SaveConversationAsync(_dir, conv);
        var loaded = await _sut.LoadConversationAsync(_dir, conv.Id);

        Assert.NotNull(loaded);
        Assert.Single(loaded!.Messages);
        Assert.Equal("Hello world", loaded.Messages[0].Content);
    }

    [Fact]
    public async Task LoadIndexAsync_EmptyWorkspace_ReturnsEmpty()
    {
        var result = await _sut.LoadIndexAsync(_dir);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchAsync_TitleMatch_ReturnsConversation()
    {
        var conv = MakeConversation(title: "Chapter One Discussion");
        await _sut.SaveConversationAsync(_dir, conv);

        var results = await _sut.SearchAsync(_dir, "Chapter One", CancellationToken.None);

        Assert.Single(results);
        Assert.Equal(conv.Id, results[0].Id);
    }

    [Fact]
    public async Task SearchAsync_MessageBodyMatch_ReturnsConversation()
    {
        var msg = MakeMessage("Let's talk about the protagonist's arc");
        var conv = MakeConversation(title: "Random Title") with
        {
            Messages = new List<ConversationMessage> { msg }
        };
        await _sut.SaveConversationAsync(_dir, conv);

        var results = await _sut.SearchAsync(_dir, "protagonist", CancellationToken.None);

        Assert.Single(results);
        Assert.Equal(conv.Id, results[0].Id);
    }

    [Fact]
    public async Task SearchAsync_NoMatch_ReturnsEmpty()
    {
        var conv = MakeConversation(title: "Some Title");
        await _sut.SaveConversationAsync(_dir, conv);

        var results = await _sut.SearchAsync(_dir, "zzznomatch", CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task ArchiveConversation_NotReturnedInNonArchivedSearch()
    {
        var conv = MakeConversation(title: "Archived Discussion");
        var archived = conv with { Archived = true };
        await _sut.SaveConversationAsync(_dir, archived);

        // archived conversations are excluded from search results by default
        var results = await _sut.SearchAsync(_dir, "Archived Discussion", CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task SaveConversationTwice_UpdatesExistingIndexEntry()
    {
        var conv = MakeConversation(title: "Original Title");
        await _sut.SaveConversationAsync(_dir, conv);

        var updated = conv with { Title = "Updated Title" };
        await _sut.SaveConversationAsync(_dir, updated);

        var index = await _sut.LoadIndexAsync(_dir);

        Assert.Single(index);
        Assert.Equal("Updated Title", index[0].Title);
    }

    // ── IDisposable ────────────────────────────────────────────────────────────

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { }
    }
}
