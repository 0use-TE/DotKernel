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
        var endpoint = configuration["DeepSeek:Endpoint"] ?? "https://api.deepseek.com";
        var modelId = configuration["DeepSeek:ModelId"] ?? "deepseek-chat";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.WriteLine("No DeepSeek:ApiKey — using EchoChatClient.");
            Console.WriteLine("Configure: dotnet user-secrets set \"DeepSeek:ApiKey\" \"<key>\" --project examples/DotKernel.Example");
            Console.WriteLine("Or: DOTKERNEL_DeepSeek__ApiKey=<key>");
            return new EchoChatClient();
        }

        Console.WriteLine($"Connected to DeepSeek ({modelId})");
        var client = new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = new Uri(endpoint) });

        return client.GetChatClient(modelId).AsIChatClient();
    }
}
