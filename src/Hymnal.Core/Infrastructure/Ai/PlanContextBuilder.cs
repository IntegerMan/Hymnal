using Hymnal.Core.Interfaces;
using Hymnal.Core.Models.Ai;

namespace Hymnal.Core.Infrastructure.Ai;

/// <summary>
/// Builds context for Plan / Corkboard view: structural book overview (chapter titles + first paragraphs).
/// </summary>
public sealed class PlanContextBuilder : ContextBuilderBase, IContextBuilder
{
    private readonly IManuscriptContextReader _reader;

    public PlanContextBuilder(IManuscriptContextReader reader)
    {
        _reader = reader;
    }

    public async Task<string> BuildContextAsync(ContextRequest request, CancellationToken ct)
    {
        var scopeContent = await BuildStructuralOverviewAsync(request, ct).ConfigureAwait(false);
        return AssembleContext(request.RoleSystemPrompt, scopeContent, request.ScopeTokenBudget);
    }

    private async Task<string> BuildStructuralOverviewAsync(ContextRequest r, CancellationToken ct)
    {
        var bookOrder = await _reader.ReadBookOrderAsync(r.BookTxtPath, ct).ConfigureAwait(false);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("## Manuscript Structure Overview");
        sb.AppendLine();

        foreach (var entry in bookOrder)
        {
            ct.ThrowIfCancellationRequested();
            if (entry.IsPart)
            {
                sb.AppendLine($"### Part: {entry.Title}");
                sb.AppendLine();
                continue;
            }

            sb.AppendLine($"**{entry.Title}**");
            var text = entry.RelativePath == r.ActiveChapterRelativePath && !string.IsNullOrEmpty(r.LiveActiveChapterText)
                ? r.LiveActiveChapterText
                : await _reader.ReadChapterTextAsync(r.WorkspaceRoot, entry.RelativePath, ct).ConfigureAwait(false);

            var firstLine = ExtractFirstMeaningfulLine(text);
            if (!string.IsNullOrEmpty(firstLine))
                sb.AppendLine($"> {firstLine}");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static string ExtractFirstMeaningfulLine(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        using var reader = new StringReader(text);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.TrimStart().StartsWith("#")) continue;
            if (line.Trim() == "{class: part}") continue;
            return line.Trim();
        }
        return string.Empty;
    }
}
