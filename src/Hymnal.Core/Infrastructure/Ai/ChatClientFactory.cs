using System;
using System.ClientModel;
using Anthropic;
using Azure.AI.OpenAI;
using Hymnal.Core.Models.Ai;
using Microsoft.Extensions.AI;
using OpenAI;

namespace Hymnal.Core.Infrastructure.Ai;

/// <summary>
/// Constructs an IChatClient from a ProviderProfile and API key, switching on ProviderType.
/// Ollama uses a placeholder key because the OpenAI SDK requires a non-empty value.
/// </summary>
public static class ChatClientFactory
{
    private const string OllamaPlaceholderKey = "ollama";

    public static IChatClient Create(ProviderProfile profile, string apiKey)
    {
        return profile.Type switch
        {
            ProviderType.OpenAI => BuildOpenAiClient(profile, apiKey),
            ProviderType.Ollama => BuildOpenAiClient(profile,
                string.IsNullOrEmpty(apiKey) ? OllamaPlaceholderKey : apiKey),
            ProviderType.AzureOpenAI => BuildAzureClient(profile, apiKey),
            ProviderType.Anthropic => BuildAnthropicClient(profile, apiKey),
            _ => throw new NotSupportedException($"Provider type '{profile.Type}' is not supported.")
        };
    }

    private static IChatClient BuildOpenAiClient(ProviderProfile profile, string apiKey)
    {
        var opts = new OpenAIClientOptions { Endpoint = new Uri(profile.BaseUrl) };
        return new OpenAIClient(new ApiKeyCredential(apiKey), opts)
            .GetChatClient(profile.ModelId)
            .AsIChatClient();
    }

    private static IChatClient BuildAzureClient(ProviderProfile profile, string apiKey)
    {
        return new AzureOpenAIClient(new Uri(profile.BaseUrl), new ApiKeyCredential(apiKey))
            .GetChatClient(profile.ModelId)
            .AsIChatClient();
    }

    private static IChatClient BuildAnthropicClient(ProviderProfile profile, string apiKey)
    {
        var client = new AnthropicClient(new Anthropic.Core.ClientOptions
        {
            ApiKey = apiKey,
            BaseUrl = profile.BaseUrl
        });
        return client.AsIChatClient(profile.ModelId);
    }
}
