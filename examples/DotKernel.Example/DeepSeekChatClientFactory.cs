using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;

namespace DotKernel.Example;

internal static class DeepSeekChatClientFactory
{
    public static IChatClient Create(IConfiguration configuration)
    {
        var apiKey = configuration["DeepSeek:ApiKey"];
        var endpoint = configuration["DeepSeek:Endpoint"];
        var modelId = configuration["DeepSeek:ModelId"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.WriteLine("未配置 DeepSeek:ApiKey，使用本地 EchoChatClient 模拟。");
            Console.WriteLine("配置: dotnet user-secrets set \"DeepSeek:ApiKey\" \"<key>\" --project examples/DotKernel.Example");
            return new EchoChatClient();
        }

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(modelId))
        {
            throw new InvalidOperationException("DeepSeek:Endpoint 与 DeepSeek:ModelId 必须在 appsettings.json 或 user-secrets 中配置。");
        }

        Console.WriteLine($"已连接 DeepSeek ({modelId})");
        var client = new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = new Uri(endpoint) });

        return client.GetChatClient(modelId).AsIChatClient();
    }
}
