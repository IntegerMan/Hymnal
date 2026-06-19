namespace Hymnal.Core.Models.Ai;

/// <summary>
/// A single turn in a conversation: user, assistant, or system message.
/// </summary>
public record ConversationMessage(
    string Id,
    string Role,
    string Content,
    DateTimeOffset Timestamp,
    int? TokenCount);
