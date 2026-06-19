using Hymnal.Core.Interfaces;
using Hymnal.Core.Models.Ai;

namespace Hymnal.Core.Infrastructure.Ai;

/// <summary>
/// Builds context for Manage / Gantt view: book overview + chapter titles.
/// </summary>
public sealed class ManageContextBuilder : ContextBuilderBase, IContextBuilder
{
    private readonly IManuscriptContextReader _reader;

    public ManageContextBuilder(IManuscriptContextReader reader)
    {
        _reader = reader;
    }

    public async Task<string> BuildContextAsync(ContextRequest request, CancellationToken ct)
    {
        var scopeContent = await BuildManagementOverviewAsync(request, ct).ConfigureAwait(false);
        return AssembleContext(request.RoleSystemPrompt, scopeContent, request.ScopeTokenBudget);
    }

    private async Task<string> BuildManagementOverviewAsync(ContextRequest r, CancellationToken ct)
    {
        var bookOrder = await _reader.ReadBookOrderAsync(r.BookTxtPath, ct).ConfigureAwait(false);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("## Manuscript Overview");
        sb.AppendLine();

        foreach (var entry in bookOrder)
        {
            ct.ThrowIfCancellationRequested();
            if (entry.IsPart)
            {
                sb.AppendLine($"### {entry.Title}");
                sb.AppendLine();
                continue;
            }
            sb.AppendLine($"- {entry.Title}");
        }
        return sb.ToString();
    }
}
