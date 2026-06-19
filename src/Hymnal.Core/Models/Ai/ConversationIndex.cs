namespace Hymnal.Core.Models.Ai;

/// <summary>
/// Envelope for index.json — metadata-only listing of all conversations.
/// </summary>
public record ConversationIndex(
    int SchemaVersion,
    List<ConversationMetadata> Conversations);
