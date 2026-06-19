using Hymnal.Core.Models.Ai;

namespace Hymnal.Core.Infrastructure.Ai;

/// <summary>
/// Shared token-budget and truncation logic for all context builders (spec §5.3).
/// 4 chars ≈ 1 token heuristic when a real tokenizer is not available.
/// </summary>
public abstract class ContextBuilderBase
{
    private const int CharsPerToken = 4;
    private const string TruncationMarker = "\n\n[... truncated to fit context limit ...]";

    protected static int EstimateTokens(string text) => text.Length / CharsPerToken;

    /// <summary>
    /// Truncates <paramref name="content"/> to fit within <paramref name="tokenBudget"/> tokens,
    /// appending the truncation marker if the content was reduced.
    /// </summary>
    protected static string TruncateToTokenBudget(string content, int tokenBudget)
    {
        if (EstimateTokens(content) <= tokenBudget)
            return content;

        var maxChars = tokenBudget * CharsPerToken - TruncationMarker.Length;
        if (maxChars <= 0) return TruncationMarker.TrimStart();
        return content[..maxChars] + TruncationMarker;
    }

    /// <summary>
    /// Assembles the final context string: role system prompt (never truncated) + scope content (truncated).
    /// </summary>
    protected static string AssembleContext(string roleSystemPrompt, string scopeContent, int scopeTokenBudget)
    {
        var truncated = TruncateToTokenBudget(scopeContent, scopeTokenBudget);
        if (string.IsNullOrWhiteSpace(truncated))
            return roleSystemPrompt;
        return roleSystemPrompt.TrimEnd() + "\n\n---\n\n" + truncated.TrimStart();
    }
}
