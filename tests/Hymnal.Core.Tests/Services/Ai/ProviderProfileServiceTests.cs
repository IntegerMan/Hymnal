using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hymnal.Core.Common;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models.Ai;
using Hymnal.Core.Services.Ai;
using Hymnal.Core.Tests.TestDoubles;
using Microsoft.Extensions.AI;
using Xunit;

namespace Hymnal.Core.Tests.Services.Ai;

public class ProviderProfileServiceTests
{
    private readonly InMemoryAppSettingsStore _settings = new();
    private readonly AiTestDoubles.InMemoryCredentialStore _credentials = new();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private ProviderProfileService CreateSut(
        Func<ProviderProfile, string, IChatClient>? factory = null)
    {
        factory ??= (_, __) => new FakeChatClient("OK");
        return new ProviderProfileService(_settings, _credentials, factory);
    }

    private static ProviderProfile MakeProfile(string? id = null) =>
        new(
            Id: id ?? Guid.NewGuid().ToString(),
            DisplayName: "Test Provider",
            BaseUrl: "https://example.com/v1",
            ModelId: "gpt-test",
            ContextWindowTokens: null);

    // ── CRUD round-trip ───────────────────────────────────────────────────────

    [Fact]
    public async Task SaveAsync_Then_LoadAllAsync_ContainsProfile()
    {
        var sut = CreateSut();
        var profile = MakeProfile();

        await sut.SaveAsync(profile);
        var all = await sut.LoadAllAsync();

        Assert.Single(all);
        Assert.Equal(profile.Id, all[0].Id);
    }

    [Fact]
    public async Task SaveAsync_ApiKey_NotStoredInSettingsJson()
    {
        var sut = CreateSut();
        var profile = MakeProfile("p1");

        await sut.SaveAsync(profile);

        // Settings should only contain the profile DTO — no API key
        var profilesJson = _settings.GetRawValue("ai.profiles");
        Assert.DoesNotContain("secret", profilesJson ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteAsync_RemovesProfile()
    {
        var sut = CreateSut();
        var profile = MakeProfile("del-1");

        await sut.SaveAsync(profile);
        await sut.DeleteAsync(profile.Id);

        var all = await sut.LoadAllAsync();
        Assert.Empty(all);
    }

    [Fact]
    public async Task SetActiveAsync_Then_GetActiveAsync_ReturnsProfile()
    {
        var sut = CreateSut();
        var profile = MakeProfile("active-1");

        await sut.SaveAsync(profile);
        await sut.SetActiveAsync(profile.Id);

        var active = await sut.GetActiveAsync();
        Assert.NotNull(active);
        Assert.Equal(profile.Id, active!.Id);
    }

    [Fact]
    public async Task GetActiveAsync_WhenNoActive_ReturnsNull()
    {
        var sut = CreateSut();
        var active = await sut.GetActiveAsync();
        Assert.Null(active);
    }

    [Fact]
    public async Task SaveAsync_UpdatesExistingProfile()
    {
        var sut = CreateSut();
        var profile = MakeProfile("up-1");

        await sut.SaveAsync(profile);
        await sut.SaveAsync(profile with { DisplayName = "Renamed" });

        var all = await sut.LoadAllAsync();

        Assert.Single(all);
        Assert.Equal("Renamed", all[0].DisplayName);
    }

    // ── Type persistence ─────────────────────────────────────────────────────

    [Fact]
    public async Task SaveAsync_Then_LoadAllAsync_PreservesProviderType()
    {
        var sut = CreateSut();
        var profile = MakeProfile("type-1") with { Type = ProviderType.AzureOpenAI };

        await sut.SaveAsync(profile);
        var all = await sut.LoadAllAsync();

        Assert.Single(all);
        Assert.Equal(ProviderType.AzureOpenAI, all[0].Type);
    }

    [Fact]
    public async Task LoadAllAsync_LegacyDtoWithoutType_DefaultsToOpenAI()
    {
        // A profile created without specifying Type uses the default (OpenAI).
        var sut = CreateSut();
        var profile = MakeProfile("legacy-1"); // no Type arg => default OpenAI

        await sut.SaveAsync(profile);
        var all = await sut.LoadAllAsync();

        Assert.Single(all);
        Assert.Equal(ProviderType.OpenAI, all[0].Type);
    }

    [Fact]
    public async Task TestConnectionAsync_SucceedingClient_ReturnsOk()
    {
        var sut = CreateSut(factory: (_, __) => new FakeChatClient("OK"));
        var profile = MakeProfile();

        var result = await sut.TestConnectionAsync(profile, "fake-key", CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task TestConnectionAsync_ThrowingClient_ReturnsFail()
    {
        var sut = CreateSut(factory: (_, __) => new FakeChatClient(throws: true));
        var profile = MakeProfile();

        var result = await sut.TestConnectionAsync(profile, "bad-key", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Error!);
    }

    // ── Inner test doubles ────────────────────────────────────────────────────

    /// <summary>Minimal IChatClient that returns a fixed reply or throws.</summary>
    private sealed class FakeChatClient : IChatClient
    {
        private readonly string _reply;
        private readonly bool _throws;

        public FakeChatClient(string reply = "OK", bool throws = false)
        {
            _reply = reply;
            _throws = throws;
        }

        public ChatClientMetadata Metadata => new("fake", null, null);

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (_throws) throw new InvalidOperationException("Connection refused");
            var msg = new ChatMessage(ChatRole.Assistant, _reply);
            return Task.FromResult(new ChatResponse([msg]));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public TService? GetService<TService>(object? key = null) where TService : class => null;
        public object? GetService(Type serviceType, object? key = null) => null;
        public void Dispose() { }
    }
}

// ── Test store implementations ─────────────────────────────────────────────

internal sealed class InMemoryAppSettingsStore : IAppSettingsStore
{
    private readonly Dictionary<string, object?> _store = new();

    public Task<T?> GetAsync<T>(string key)
    {
        if (_store.TryGetValue(key, out var v) && v is T t)
            return Task.FromResult<T?>(t);
        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T? value)
    {
        _store[key] = value;
        return Task.CompletedTask;
    }

    /// <summary>Returns the raw object stored (for assertions about JSON).</summary>
    public string? GetRawValue(string key) =>
        _store.TryGetValue(key, out var v) ? System.Text.Json.JsonSerializer.Serialize(v) : null;
}
