namespace Hymnal.Core.Models.Ai;

/// <summary>
/// Full conversation: metadata + all messages. Stored in {uuid}.json under .hymnal-data/conversations/.
/// </summary>
public record Conversation(
    string Id,
    string Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool Archived,
    string Role,
    ContextTag ContextTag,
    List<ConversationMessage> Messages)
{
    public int MessageCount => Messages.Count;

    public ConversationMetadata ToMetadata() => new(
        Id, Title, CreatedAt, UpdatedAt, Archived, Role, ContextTag, MessageCount);
}
