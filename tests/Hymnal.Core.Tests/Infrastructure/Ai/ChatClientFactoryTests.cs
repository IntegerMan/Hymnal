using System;
using Hymnal.Core.Infrastructure.Ai;
using Hymnal.Core.Models.Ai;
using Microsoft.Extensions.AI;
using Xunit;

namespace Hymnal.Core.Tests.Infrastructure.Ai;

/// <summary>
/// Verifies that ChatClientFactory.Create constructs a non-null IChatClient for each
/// provider type without making any network calls (construction only).
/// </summary>
public class ChatClientFactoryTests
{
    private static ProviderProfile MakeProfile(ProviderType type, string baseUrl, string modelId) =>
        new(Guid.NewGuid().ToString(), "Test", baseUrl, modelId, null, type);

    [Fact]
    public void Create_OpenAI_ReturnsNonNullClient()
    {
        var profile = MakeProfile(ProviderType.OpenAI, "https://api.openai.com/v1", "gpt-4o");
        IChatClient client = ChatClientFactory.Create(profile, "test-key");
        Assert.NotNull(client);
    }

    [Fact]
    public void Create_Ollama_ReturnsNonNullClient()
    {
        var profile = MakeProfile(ProviderType.Ollama, "http://localhost:11434/v1", "llama3.1");
        IChatClient client = ChatClientFactory.Create(profile, string.Empty);
        Assert.NotNull(client);
    }

    [Fact]
    public void Create_Ollama_WithEmptyKey_DoesNotThrow()
    {
        var profile = MakeProfile(ProviderType.Ollama, "http://localhost:11434/v1", "llama3.1");
        var ex = Record.Exception(() => ChatClientFactory.Create(profile, string.Empty));
        Assert.Null(ex);
    }

    [Fact]
    public void Create_AzureOpenAI_ReturnsNonNullClient()
    {
        var profile = MakeProfile(ProviderType.AzureOpenAI, "https://my-resource.openai.azure.com", "my-deployment");
        IChatClient client = ChatClientFactory.Create(profile, "test-key");
        Assert.NotNull(client);
    }

    [Fact]
    public void Create_Anthropic_ReturnsNonNullClient()
    {
        var profile = MakeProfile(ProviderType.Anthropic, "https://api.anthropic.com", "claude-sonnet-4-5");
        IChatClient client = ChatClientFactory.Create(profile, "test-key");
        Assert.NotNull(client);
    }

    [Fact]
    public void Create_UnknownType_ThrowsNotSupported()
    {
        var profile = MakeProfile((ProviderType)999, "https://example.com", "model");
        Assert.Throws<NotSupportedException>(() => ChatClientFactory.Create(profile, "key"));
    }
}
