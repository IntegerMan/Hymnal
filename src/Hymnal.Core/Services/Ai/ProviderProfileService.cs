using System.Text.Json;
using Hymnal.Core.Common;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models.Ai;
using Microsoft.Extensions.AI;

namespace Hymnal.Core.Services.Ai;

/// <summary>
/// Manages provider profile CRUD.
/// Non-secret fields live in IAppSettingsStore under "ai.profiles" / "ai.activeProfileId".
/// API keys are stored in ICredentialStore under "hymnal.provider.{id}.apikey".
/// </summary>
public sealed class ProviderProfileService : IProviderProfileService
{
    private const string ProfilesKey = "ai.profiles";
    private const string ActiveProfileIdKey = "ai.activeProfileId";

    private readonly IAppSettingsStore _settings;
    private readonly ICredentialStore _credentials;
    private readonly Func<ProviderProfile, string, IChatClient> _clientFactory;

    public ProviderProfileService(
        IAppSettingsStore settings,
        ICredentialStore credentials,
        Func<ProviderProfile, string, IChatClient> clientFactory)
    {
        _settings = settings;
        _credentials = credentials;
        _clientFactory = clientFactory;
    }

    public async Task<IReadOnlyList<ProviderProfile>> LoadAllAsync()
    {
        var list = await _settings.GetAsync<List<ProviderProfileDto>>(ProfilesKey)
            .ConfigureAwait(false);
        if (list is null) return Array.Empty<ProviderProfile>();
        return list.Select(dto => dto.ToModel()).ToList();
    }

    public async Task<ProviderProfile?> GetActiveAsync()
    {
        var activeId = await _settings.GetAsync<string>(ActiveProfileIdKey).ConfigureAwait(false);
        if (string.IsNullOrEmpty(activeId)) return null;
        var all = await LoadAllAsync().ConfigureAwait(false);
        return all.FirstOrDefault(p => p.Id == activeId);
    }

    public async Task SaveAsync(ProviderProfile profile)
    {
        var all = (await LoadAllAsync().ConfigureAwait(false)).ToList();
        var idx = all.FindIndex(p => p.Id == profile.Id);
        if (idx >= 0)
            all[idx] = profile;
        else
            all.Add(profile);

        await _settings.SetAsync(ProfilesKey, all.Select(ProviderProfileDto.From).ToList())
            .ConfigureAwait(false);
    }

    public async Task DeleteAsync(string profileId)
    {
        var all = (await LoadAllAsync().ConfigureAwait(false))
            .Where(p => p.Id != profileId)
            .ToList();
        await _settings.SetAsync(ProfilesKey, all.Select(ProviderProfileDto.From).ToList())
            .ConfigureAwait(false);
        await _credentials.DeleteAsync(ApiKeyCredentialKey(profileId)).ConfigureAwait(false);

        // Clear active if it was this profile
        var activeId = await _settings.GetAsync<string>(ActiveProfileIdKey).ConfigureAwait(false);
        if (activeId == profileId)
            await _settings.SetAsync<string?>(ActiveProfileIdKey, null).ConfigureAwait(false);
    }

    public async Task SetActiveAsync(string profileId) =>
        await _settings.SetAsync(ActiveProfileIdKey, profileId).ConfigureAwait(false);

    public async Task<Result<Unit>> TestConnectionAsync(
        ProviderProfile profile, string apiKey, CancellationToken ct)
    {
        try
        {
            var client = _clientFactory(profile, apiKey);
            // Send a minimal 1-token test request
            var result = await client.GetResponseAsync(
                "Say OK",
                new ChatOptions { MaxOutputTokens = 1 },
                ct).ConfigureAwait(false);
            return Result<Unit>.Ok(Unit.Default);
        }
        catch (OperationCanceledException)
        {
            return Result<Unit>.Fail("Test cancelled.");
        }
        catch (Exception ex)
        {
            return Result<Unit>.Fail(ex.Message);
        }
    }

    // ── Credential key helper ─────────────────────────────────────────────

    public static string ApiKeyCredentialKey(string profileId) =>
        $"hymnal.provider.{profileId}.apikey";

    // ── Private DTO for settings serialization (no API key) ──────────────

    private sealed record ProviderProfileDto(
        string Id,
        string DisplayName,
        string BaseUrl,
        string ModelId,
        int? ContextWindowTokens,
        ProviderType Type = ProviderType.OpenAI)
    {
        public static ProviderProfileDto From(ProviderProfile p) =>
            new(p.Id, p.DisplayName, p.BaseUrl, p.ModelId, p.ContextWindowTokens, p.Type);

        public ProviderProfile ToModel() =>
            new(Id, DisplayName, BaseUrl, ModelId, ContextWindowTokens, Type);
    }
}
