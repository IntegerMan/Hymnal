namespace Hymnal.Core.Models.Ai;

/// <summary>
/// Named connection parameters for a provider endpoint. API key is stored separately in ICredentialStore.
/// </summary>
public record ProviderProfile(
    string Id,
    string DisplayName,
    string BaseUrl,
    string ModelId,
    int? ContextWindowTokens,
    ProviderType Type = ProviderType.OpenAI);
