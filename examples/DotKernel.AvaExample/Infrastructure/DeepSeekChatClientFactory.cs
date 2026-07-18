using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;

namespace DotKernel.AvaExample.Infrastructure;

internal static class DeepSeekChatClientFactory
{
    public static (IChatClient Client, string Status) Create(IConfiguration configuration) =>
        Create(
            configuration["DeepSeek:ApiKey"],
            configuration["DeepSeek:Endpoint"] ?? "https://api.deepseek.com",
            configuration["DeepSeek:ModelId"] ?? "deepseek-chat");

    public static (IChatClient Client, string Status) Create(string? apiKey, string? endpoint, string? modelId)
    {
        endpoint = string.IsNullOrWhiteSpace(endpoint) ? "https://api.deepseek.com" : endpoint.Trim();
        modelId = string.IsNullOrWhiteSpace(modelId) ? "deepseek-chat" : modelId.Trim();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return (new EchoChatClient(), OperatingSystem.IsBrowser()
                ? "Echo (enter API key above)"
                : "Echo (no API key)");
        }

        var client = new OpenAIClient(
            new ApiKeyCredential(apiKey.Trim()),
            new OpenAIClientOptions { Endpoint = new Uri(endpoint) });

        var label = OperatingSystem.IsBrowser() ? "Web" : "Desktop";
        return (client.GetChatClient(modelId).AsIChatClient(), $"{label} · {modelId}");
    }
}
