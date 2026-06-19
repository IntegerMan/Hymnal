using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models.Ai;
using Hymnal.Core.Services.Ai;
using Hymnal.Core.Tests.TestDoubles;
using Microsoft.Extensions.AI;
using Xunit;

namespace Hymnal.Core.Tests.Services.Ai;

public class AiChatServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AiChatService CreateSut(
        IProviderProfileService profiles,
        ICredentialStore credentials,
        Func<ProviderProfile, string, IChatClient>? factory = null)
    {
        factory ??= (_, __) => new QueuedFakeChatClient();
        return new AiChatService(profiles, credentials, factory);
    }

    private static ProviderProfile MakeProfile() =>
        new("p1", "Test", "https://api.example.com/v1", "model-x", null);

    // ── IsProviderConfigured ──────────────────────────────────────────────────

    [Fact]
    public async Task IsProviderConfigured_AfterRefresh_FalseWhenNoActiveProfile()
    {
        var profiles = new AiTestDoubles.InMemoryProviderProfileService();
        var creds = new AiTestDoubles.InMemoryCredentialStore();
        var sut = CreateSut(profiles, creds);

        await sut.RefreshClientAsync();

        Assert.False(sut.IsProviderConfigured);
    }

    [Fact]
    public async Task IsProviderConfigured_FalseWhenApiKeyIsNull()
    {
        var profiles = new AiTestDoubles.InMemoryProviderProfileService();
        var profile = MakeProfile();
        await profiles.SaveAsync(profile);
        await profiles.SetActiveAsync(profile.Id);

        var creds = new AiTestDoubles.InMemoryCredentialStore();
        // No API key stored — credential is null
        var sut = CreateSut(profiles, creds);

        await sut.RefreshClientAsync();

        Assert.False(sut.IsProviderConfigured);
    }

    [Fact]
    public async Task IsProviderConfigured_TrueWhenEmptyStringApiKey()
    {
        var profiles = new AiTestDoubles.InMemoryProviderProfileService();
        var profile = MakeProfile();
        await profiles.SaveAsync(profile);
        await profiles.SetActiveAsync(profile.Id);

        var creds = new AiTestDoubles.InMemoryCredentialStore();
        // Empty string = valid for local endpoints like Ollama
        await creds.StoreAsync(ProviderProfileService.ApiKeyCredentialKey(profile.Id), string.Empty);

        var sut = CreateSut(profiles, creds);
        await sut.RefreshClientAsync();

        Assert.True(sut.IsProviderConfigured);
    }

    [Fact]
    public async Task IsProviderConfigured_TrueWhenRealApiKeyStored()
    {
        var profiles = new AiTestDoubles.InMemoryProviderProfileService();
        var profile = MakeProfile();
        await profiles.SaveAsync(profile);
        await profiles.SetActiveAsync(profile.Id);

        var creds = new AiTestDoubles.InMemoryCredentialStore();
        await creds.StoreAsync(ProviderProfileService.ApiKeyCredentialKey(profile.Id), "sk-real-key");

        var sut = CreateSut(profiles, creds);
        await sut.RefreshClientAsync();

        Assert.True(sut.IsProviderConfigured);
    }

    // ── StreamAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task StreamAsync_YieldsChunksInOrder()
    {
        var profiles = new AiTestDoubles.InMemoryProviderProfileService();
        var profile = MakeProfile();
        await profiles.SaveAsync(profile);
        await profiles.SetActiveAsync(profile.Id);

        var creds = new AiTestDoubles.InMemoryCredentialStore();
        await creds.StoreAsync(ProviderProfileService.ApiKeyCredentialKey(profile.Id), "key");

        var fakeClient = new QueuedFakeChatClient("Hello", " ", "world");
        var sut = CreateSut(profiles, creds, factory: (_, __) => fakeClient);
        await sut.RefreshClientAsync();

        var messages = new List<ChatMessage> { new(ChatRole.User, "Test") };
        var chunks = new List<string>();

        await foreach (var chunk in sut.StreamAsync(messages, CancellationToken.None))
            chunks.Add(chunk);

        Assert.Equal(new[] { "Hello", " ", "world" }, chunks);
    }

    [Fact]
    public async Task StreamAsync_Cancellation_StopsEarly()
    {
        var profiles = new AiTestDoubles.InMemoryProviderProfileService();
        var profile = MakeProfile();
        await profiles.SaveAsync(profile);
        await profiles.SetActiveAsync(profile.Id);

        var creds = new AiTestDoubles.InMemoryCredentialStore();
        await creds.StoreAsync(ProviderProfileService.ApiKeyCredentialKey(profile.Id), "key");

        // Infinite chunk producer
        var fakeClient = new InfiniteChunkChatClient();
        var sut = CreateSut(profiles, creds, factory: (_, __) => fakeClient);
        await sut.RefreshClientAsync();

        var messages = new List<ChatMessage> { new(ChatRole.User, "Test") };
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in sut.StreamAsync(messages, cts.Token))
            { /* consume */ }
        });
    }

    [Fact]
    public async Task StreamAsync_WhenNotConfigured_Throws()
    {
        var profiles = new AiTestDoubles.InMemoryProviderProfileService();
        var creds = new AiTestDoubles.InMemoryCredentialStore();
        var sut = CreateSut(profiles, creds);
        // No RefreshClientAsync called — not configured

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in sut.StreamAsync([], CancellationToken.None))
            { /* consume */ }
        });
    }

    // ── Inner test doubles ────────────────────────────────────────────────────

    private sealed class QueuedFakeChatClient : IChatClient
    {
        private readonly IEnumerable<string> _chunks;

        public QueuedFakeChatClient(params string[] chunks) =>
            _chunks = chunks.Length == 0 ? ["OK"] : chunks;

        public ChatClientMetadata Metadata => new("fake", null, null);

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var msg = new ChatMessage(ChatRole.Assistant, string.Concat(_chunks));
            return Task.FromResult(new ChatResponse([msg]));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var chunk in _chunks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return new ChatResponseUpdate(ChatRole.Assistant, chunk);
                await Task.Yield();
            }
        }

        public TService? GetService<TService>(object? key = null) where TService : class => null;
        public object? GetService(Type serviceType, object? key = null) => null;
        public void Dispose() { }
    }

    private sealed class InfiniteChunkChatClient : IChatClient
    {
        public ChatClientMetadata Metadata => new("fake", null, null);

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> _,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return new ChatResponseUpdate(ChatRole.Assistant, "chunk");
                await Task.Delay(10, cancellationToken);
            }
        }

        public TService? GetService<TService>(object? key = null) where TService : class => null;
        public object? GetService(Type serviceType, object? key = null) => null;
        public void Dispose() { }
    }
}
