using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;

namespace DotKernel.AvaExample.Infrastructure;

internal static class DeepSeekChatClientFactory
{
    public static (IChatClient Client, string Status) Create(IConfiguration configuration)
    {
        var apiKey = configuration["DeepSeek:ApiKey"];
        var endpoint = configuration["DeepSeek:Endpoint"];
        var modelId = configuration["DeepSeek:ModelId"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return (new EchoChatClient(), OperatingSystem.IsBrowser()
                ? "Echo (set DeepSeek:ApiKey in CI)"
                : "Echo (no DeepSeek:ApiKey)");
        }

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(modelId))
        {
            throw new InvalidOperationException("DeepSeek:Endpoint and DeepSeek:ModelId must be configured.");
        }

        var client = new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = new Uri(endpoint) });

        var label = OperatingSystem.IsBrowser() ? "Web · DeepSeek" : "DeepSeek";
        return (client.GetChatClient(modelId).AsIChatClient(), $"{label} · {modelId}");
    }
}
