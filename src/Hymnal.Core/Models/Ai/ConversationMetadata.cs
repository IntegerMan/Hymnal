namespace Hymnal.Core.Models.Ai;

/// <summary>
/// Lightweight metadata entry stored in the conversation index. No message content.
/// </summary>
public record ConversationMetadata(
    string Id,
    string Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool Archived,
    string Role,
    ContextTag ContextTag,
    int MessageCount);
