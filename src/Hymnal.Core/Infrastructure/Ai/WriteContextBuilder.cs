using Hymnal.Core.Interfaces;
using Hymnal.Core.Models.Ai;

namespace Hymnal.Core.Infrastructure.Ai;

/// <summary>
/// Builds context for Write view: CHAPTER / PART / BOOK scope.
/// </summary>
public sealed class WriteContextBuilder : ContextBuilderBase, IContextBuilder
{
    private readonly IManuscriptContextReader _reader;

    public WriteContextBuilder(IManuscriptContextReader reader)
    {
        _reader = reader;
    }

    public async Task<string> BuildContextAsync(ContextRequest request, CancellationToken ct)
    {
        var scopeContent = request.Scope switch
        {
            ConversationScope.Chapter => await BuildChapterContextAsync(request, ct),
            ConversationScope.Part    => await BuildPartContextAsync(request, ct),
            ConversationScope.Book    => await BuildBookContextAsync(request, ct),
            _ => string.Empty,
        };

        return AssembleContext(request.RoleSystemPrompt, scopeContent, request.ScopeTokenBudget);
    }

    private async Task<string> BuildChapterContextAsync(ContextRequest r, CancellationToken ct)
    {
        // Prefer live (possibly unsaved) editor text; fall back to disk
        if (!string.IsNullOrEmpty(r.LiveActiveChapterText))
            return r.LiveActiveChapterText;

        if (string.IsNullOrEmpty(r.ActiveChapterRelativePath))
            return await BuildBookContextAsync(r, ct);

        return await _reader.ReadChapterTextAsync(r.WorkspaceRoot, r.ActiveChapterRelativePath, ct)
            .ConfigureAwait(false);
    }

    private async Task<string> BuildPartContextAsync(ContextRequest r, CancellationToken ct)
    {
        var bookOrder = await _reader.ReadBookOrderAsync(r.BookTxtPath, ct).ConfigureAwait(false);
        if (!bookOrder.Any()) return string.Empty;

        // Find the part that contains the active chapter
        string? activePart = null;
        foreach (var entry in bookOrder)
        {
            if (entry.IsPart) activePart = entry.RelativePath;
            if (!entry.IsPart && entry.RelativePath == r.ActiveChapterRelativePath) break;
        }

        // Collect all chapters under the same part
        var partChapters = new List<(string RelativePath, string Title)>();
        string? currentPart = null;
        bool inTargetPart = activePart is null; // if no part structure, include all

        foreach (var entry in bookOrder)
        {
            if (entry.IsPart)
            {
                currentPart = entry.RelativePath;
                if (activePart is null || currentPart == activePart)
                    inTargetPart = true;
                else
                    inTargetPart = false;
            }
            else if (inTargetPart)
            {
                partChapters.Add((entry.RelativePath, entry.Title));
            }
        }

        var sb = new System.Text.StringBuilder();
        foreach (var (relPath, title) in partChapters)
        {
            ct.ThrowIfCancellationRequested();
            sb.AppendLine($"# {title}");
            sb.AppendLine();
            // Use live text for the active chapter
            var text = relPath == r.ActiveChapterRelativePath && !string.IsNullOrEmpty(r.LiveActiveChapterText)
                ? r.LiveActiveChapterText
                : await _reader.ReadChapterTextAsync(r.WorkspaceRoot, relPath, ct).ConfigureAwait(false);
            sb.AppendLine(text);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private async Task<string> BuildBookContextAsync(ContextRequest r, CancellationToken ct)
    {
        var bookOrder = await _reader.ReadBookOrderAsync(r.BookTxtPath, ct).ConfigureAwait(false);
        var sb = new System.Text.StringBuilder();

        foreach (var entry in bookOrder)
        {
            ct.ThrowIfCancellationRequested();
            if (entry.IsPart)
            {
                sb.AppendLine($"## {entry.Title}");
                sb.AppendLine();
                continue;
            }

            sb.AppendLine($"### {entry.Title}");
            // For BOOK scope: title + first paragraph only
            var text = entry.RelativePath == r.ActiveChapterRelativePath && !string.IsNullOrEmpty(r.LiveActiveChapterText)
                ? r.LiveActiveChapterText
                : await _reader.ReadChapterTextAsync(r.WorkspaceRoot, entry.RelativePath, ct).ConfigureAwait(false);

            var firstParagraph = ExtractFirstParagraph(text);
            if (!string.IsNullOrEmpty(firstParagraph))
                sb.AppendLine(firstParagraph);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static string ExtractFirstParagraph(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        using var reader = new StringReader(text);
        var sb = new System.Text.StringBuilder();
        bool inParagraph = false;
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                if (inParagraph) break; // end of first paragraph
                continue;
            }
            if (line.TrimStart().StartsWith("#")) continue; // skip headings
            inParagraph = true;
            sb.AppendLine(line);
        }
        return sb.ToString().TrimEnd();
    }
}
