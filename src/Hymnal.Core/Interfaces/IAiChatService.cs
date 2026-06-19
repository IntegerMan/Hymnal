using Microsoft.Extensions.AI;

namespace Hymnal.Core.Interfaces;

public interface IAiChatService
{
    bool IsProviderConfigured { get; }

    /// <summary>
    /// Recreates the underlying IChatClient from the current active provider profile.
    /// Called when the active profile or its API key changes.
    /// </summary>
    Task RefreshClientAsync();

    /// <summary>
    /// Streams AI response tokens for the given message history. Yields each text chunk as it arrives.
    /// </summary>
    IAsyncEnumerable<string> StreamAsync(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken ct);
}
