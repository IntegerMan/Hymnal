using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hymnal.Core.Infrastructure.Ai;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models.Ai;
using Xunit;

namespace Hymnal.Core.Tests.Infrastructure.Ai;

public class ContextBuilderTests
{
    // ── Fakes ──────────────────────────────────────────────────────────────────

    private sealed class FixedManuscriptContextReader : IManuscriptContextReader
    {
        private readonly Dictionary<string, string> _chapters;
        private readonly IReadOnlyList<(string RelativePath, string Title, bool IsPart)> _bookOrder;

        public FixedManuscriptContextReader(
            Dictionary<string, string>? chapters = null,
            IReadOnlyList<(string RelativePath, string Title, bool IsPart)>? bookOrder = null)
        {
            _chapters = chapters ?? new Dictionary<string, string>();
            _bookOrder = bookOrder ?? Array.Empty<(string, string, bool)>();
        }

        public Task<string> ReadChapterTextAsync(string _, string relPath, CancellationToken ct) =>
            Task.FromResult(_chapters.TryGetValue(relPath, out var text) ? text : string.Empty);

        public Task<IReadOnlyList<(string RelativePath, string Title, bool IsPart)>> ReadBookOrderAsync(
            string _, CancellationToken ct) =>
            Task.FromResult(_bookOrder);
    }

    private static ContextRequest MakeRequest(
        ConversationScope scope = ConversationScope.Chapter,
        string? activeChapter = "chapter1.md",
        string? liveText = null)
    {
        return new ContextRequest(
            Scope: scope,
            WorkspaceRoot: "/workspace",
            ManuscriptRoot: "/workspace/manuscript",
            BookTxtPath: "/workspace/manuscript/Book.txt",
            ActiveChapterRelativePath: activeChapter,
            LiveActiveChapterText: liveText,
            RoleSystemPrompt: "You are a writing coach.");
    }

    // ── WriteContextBuilder ────────────────────────────────────────────────────

    [Fact]
    public async Task WriteBuilder_ChapterScope_WithLiveText_IncludesLiveText()
    {
        var reader = new FixedManuscriptContextReader();
        var builder = new WriteContextBuilder(reader);
        var request = MakeRequest(ConversationScope.Chapter, liveText: "Live unsaved content here.");

        var context = await builder.BuildContextAsync(request, CancellationToken.None);

        Assert.Contains("Live unsaved content here.", context);
    }

    [Fact]
    public async Task WriteBuilder_ChapterScope_NoLiveText_ReadsFromDisk()
    {
        var chapters = new Dictionary<string, string>
        {
            ["chapter1.md"] = "Chapter text from disk."
        };
        var reader = new FixedManuscriptContextReader(chapters);
        var builder = new WriteContextBuilder(reader);
        var request = MakeRequest(ConversationScope.Chapter);

        var context = await builder.BuildContextAsync(request, CancellationToken.None);

        Assert.Contains("Chapter text from disk.", context);
    }

    [Fact]
    public async Task WriteBuilder_BookScope_IncludesAllChapterTitles()
    {
        var bookOrder = new[]
        {
            ("chapter1.md", "Chapter One", false),
            ("chapter2.md", "Chapter Two", false),
        };
        var chapters = new Dictionary<string, string>
        {
            ["chapter1.md"] = "Content of chapter one.",
            ["chapter2.md"] = "Content of chapter two.",
        };
        var reader = new FixedManuscriptContextReader(chapters, bookOrder);
        var builder = new WriteContextBuilder(reader);
        var request = MakeRequest(ConversationScope.Book);

        var context = await builder.BuildContextAsync(request, CancellationToken.None);

        Assert.Contains("Chapter One", context);
        Assert.Contains("Chapter Two", context);
    }

    [Fact]
    public async Task WriteBuilder_Truncation_AppendsMarker()
    {
        var bigText = new string('x', 50_000); // Very large content
        var chapters = new Dictionary<string, string> { ["chapter1.md"] = bigText };
        var reader = new FixedManuscriptContextReader(chapters);
        var builder = new WriteContextBuilder(reader);

        // Very small budget forces truncation
        var request = new ContextRequest(
            Scope: ConversationScope.Chapter,
            WorkspaceRoot: "/workspace",
            ManuscriptRoot: "/workspace/manuscript",
            BookTxtPath: "/workspace/manuscript/Book.txt",
            ActiveChapterRelativePath: "chapter1.md",
            LiveActiveChapterText: null,
            RoleSystemPrompt: "You are a coach.",
            ScopeTokenBudget: 10); // Only ~40 chars of content

        var context = await builder.BuildContextAsync(request, CancellationToken.None);

        Assert.Contains("truncated", context, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WriteBuilder_ContainsRoleSystemPrompt()
    {
        var reader = new FixedManuscriptContextReader();
        var builder = new WriteContextBuilder(reader);
        var request = MakeRequest(liveText: "some text");

        var context = await builder.BuildContextAsync(request, CancellationToken.None);

        Assert.Contains("You are a writing coach.", context);
    }

    // ── PlanContextBuilder ─────────────────────────────────────────────────────

    [Fact]
    public async Task PlanBuilder_IncludesStructuralOverview()
    {
        var bookOrder = new[]
        {
            ("part1.md", "Part One", true),
            ("chapter1.md", "Opening", false),
        };
        var reader = new FixedManuscriptContextReader(bookOrder: bookOrder);
        var builder = new PlanContextBuilder(reader);
        var request = MakeRequest(ConversationScope.Book);

        var context = await builder.BuildContextAsync(request, CancellationToken.None);

        Assert.Contains("Part One", context);
        Assert.Contains("Opening", context);
    }

    // ── ManageContextBuilder ───────────────────────────────────────────────────

    [Fact]
    public async Task ManageBuilder_IncludesBookOverview()
    {
        var bookOrder = new[]
        {
            ("chapter1.md", "First Chapter", false),
        };
        var reader = new FixedManuscriptContextReader(bookOrder: bookOrder);
        var builder = new ManageContextBuilder(reader);
        var request = MakeRequest(ConversationScope.Book);

        var context = await builder.BuildContextAsync(request, CancellationToken.None);

        Assert.Contains("First Chapter", context);
    }
}
