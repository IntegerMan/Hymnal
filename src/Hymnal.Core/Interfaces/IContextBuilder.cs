using Hymnal.Core.Models.Ai;

namespace Hymnal.Core.Interfaces;

/// <summary>
/// Assembles the read-only prompt prefix (role system prompt + scope content)
/// that is prepended to the conversation history before sending to the model.
/// </summary>
public interface IContextBuilder
{
    /// <summary>
    /// Returns the assembled context string for the given request.
    /// Never writes to any file.
    /// </summary>
    Task<string> BuildContextAsync(ContextRequest request, CancellationToken ct);
}
