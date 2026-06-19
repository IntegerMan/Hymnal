using Hymnal.Core.Models.Ai;

namespace Hymnal.Core.Interfaces;

public interface IConversationStore
{
    Task<IReadOnlyList<ConversationMetadata>> LoadIndexAsync(string workspaceRoot);
    Task<Conversation?> LoadConversationAsync(string workspaceRoot, string conversationId);
    Task SaveConversationAsync(string workspaceRoot, Conversation conversation);
    Task SaveIndexEntryAsync(string workspaceRoot, ConversationMetadata entry);
    Task DeleteConversationAsync(string workspaceRoot, string conversationId);
    Task<IReadOnlyList<Conversation>> SearchAsync(string workspaceRoot, string query, CancellationToken ct);
}
