namespace Hymnal.Core.Models.Ai;

/// <summary>
/// Named connection parameters for an OpenAI-compatible endpoint. API key is stored separately in ICredentialStore.
/// </summary>
public record ProviderProfile(
    string Id,
    string DisplayName,
    string BaseUrl,
    string ModelId,
    int? ContextWindowTokens);
