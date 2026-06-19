using System.Runtime.CompilerServices;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models.Ai;
using Microsoft.Extensions.AI;

namespace Hymnal.Core.Services.Ai;

/// <summary>
/// Manages the active IChatClient lifecycle and streams AI responses.
/// The client is recreated whenever the active provider profile changes.
/// </summary>
public sealed class AiChatService : IAiChatService
{
    private readonly IProviderProfileService _profiles;
    private readonly ICredentialStore _credentials;
    private readonly Func<ProviderProfile, string, IChatClient> _clientFactory;

    private IChatClient? _client;
    private bool _isConfigured;

    public AiChatService(
        IProviderProfileService profiles,
        ICredentialStore credentials,
        Func<ProviderProfile, string, IChatClient> clientFactory)
    {
        _profiles = profiles;
        _credentials = credentials;
        _clientFactory = clientFactory;
    }

    public bool IsProviderConfigured => _isConfigured;

    public async Task RefreshClientAsync()
    {
        _client = null;
        _isConfigured = false;

        var profile = await _profiles.GetActiveAsync().ConfigureAwait(false);
        if (profile is null) return;

        var apiKey = await _credentials
            .RetrieveAsync(ProviderProfileService.ApiKeyCredentialKey(profile.Id))
            .ConfigureAwait(false);

        // null means never set — treat as unconfigured; empty string is valid for local endpoints
        if (apiKey is null) return;

        _client = _clientFactory(profile, apiKey);
        _isConfigured = true;
    }

    public async IAsyncEnumerable<string> StreamAsync(
        IReadOnlyList<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken ct)
    {
        if (_client is null)
            throw new InvalidOperationException("No AI provider is configured.");

        await foreach (var update in _client.GetStreamingResponseAsync(messages, null, ct)
                           .ConfigureAwait(false))
        {
            var text = update.Text;
            if (!string.IsNullOrEmpty(text))
                yield return text;
        }
    }
}
