# Quick Start

DotKernel is a lightweight AI kernel for .NET with attribute-driven plugins, prompts, filters, and live context properties. It targets **Native AOT** and avoids the weight of Microsoft Semantic Kernel.

Documentation version: **v1.1.0** · NuGet: **[DotKernel 1.1.0](https://www.nuget.org/packages/DotKernel/)**

## 1. Install DotKernel

```bash
dotnet add package DotKernel --version 1.1.0
```

The NuGet package includes the Roslyn source generator (analyzers). You do **not** need a separate generator reference.

> Use **1.0.1+** (sync `[KernelFunction]` without `CancellationToken`). Prefer **1.1.0** for DI injection, `KernelInvokeOptions`, and `InvokeFunctionAsync`.

### Develop against this repo

```xml
<ProjectReference Include="..\..\src\DotKernel\DotKernel.csproj" />
<ProjectReference Include="..\..\src\DotKernel.Generators\DotKernel.Generators.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

## 2. Create an `IChatClient` (OpenAI example)

DotKernel talks to models through [`Microsoft.Extensions.AI`](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai)’s `IChatClient`. You bring the client; DotKernel does not ship a provider.

For OpenAI (or any OpenAI-compatible endpoint), install:

```bash
dotnet add package Microsoft.Extensions.AI.OpenAI
```

Then build a client and pass it to `AddChatClient`:

```csharp
using System.ClientModel;
using DotKernel;
using Microsoft.Extensions.AI;
using OpenAI;

// Official OpenAI
var openAi = new OpenAIClient("sk-...");
IChatClient chatClient = openAi.GetChatClient("gpt-4o-mini").AsIChatClient();

// Or any OpenAI-compatible API (DeepSeek, Azure OpenAI, local gateway, …)
var compatible = new OpenAIClient(
    new ApiKeyCredential("your-api-key"),
    new OpenAIClientOptions { Endpoint = new Uri("https://api.deepseek.com") });
IChatClient chatClient = compatible.GetChatClient("deepseek-chat").AsIChatClient();
```

Any other `IChatClient` works the same way (Azure, Ollama adapters, custom wrappers, etc.).

## 3. Minimal kernel

```csharp
using DotKernel;
using Microsoft.Extensions.AI;

var kernel = KernelBuilder.Create()
    .AddChatClient(chatClient)   // from step 2
    .AddPlugin(new WeatherPlugin())
    .ConfigureDefaults(o =>
    {
        o.MaxToolCallRounds = 16;
        o.ChatOptions = new ChatOptions { Temperature = 0.3f };
    })
    .Build();

var result = await kernel.InvokeAsync("What's the weather in Seattle?");
Console.WriteLine(result);

// Call a tool directly (no model round-trip)
var weather = await kernel.InvokeFunctionAsync(
    "Weather.get_weather",
    new Dictionary<string, object?> { ["city"] = "Seattle" });
```

`AddChatClient` is required — without it, `Build()` throws.

Optional DI for function parameters:

```csharp
builder.UseServiceProvider(services);
// then: public string Run(string input, IServiceProvider sp) { ... }
```

## 4. Plugins with attributes

Mark a `partial` class; the source generator emits static registration code.

```csharp
[KernelPlugin("Weather")]
public partial class WeatherPlugin
{
    [KernelProperty("temperature_unit", "Unit when reporting weather (C or F)")]
    public string TemperatureUnit { get; set; } = "C";

    [KernelPrompt("system", Role = PromptRole.System)]
    public string SystemPrompt => "You are a concise weather assistant.";

    [KernelFunction("get_weather")]
    [KernelDescription("Get weather for a city")]
    public string GetWeather([KernelDescription("City name")] string city)
        => $"Sunny in {city} (°{TemperatureUnit})";
}
```

Sync methods without `CancellationToken` are fine. Optional injected parameters: `CancellationToken`, `Kernel`, `IChatClient`, `IServiceProvider`.

`[KernelPrompt(..., Role = System)]` and `[KernelProperty]` are injected on each model call (temporary — not written into `ChatHistory`). See [Plugins & Prompts](plugins-and-prompts.md).

## Complete console sample

```csharp
using System.ClientModel;
using DotKernel;
using Microsoft.Extensions.AI;
using OpenAI;

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException("Set OPENAI_API_KEY.");

IChatClient chatClient = new OpenAIClient(apiKey)
    .GetChatClient("gpt-4o-mini")
    .AsIChatClient();

var kernel = KernelBuilder.Create()
    .AddChatClient(chatClient)
    .AddPlugin(new WeatherPlugin())
    .Build();

Console.WriteLine(await kernel.InvokeAsync("What's the weather in Seattle?"));

[KernelPlugin("Weather")]
public partial class WeatherPlugin
{
    [KernelFunction("get_weather")]
    [KernelDescription("Get weather for a city")]
    public string GetWeather([KernelDescription("City name")] string city)
        => $"Sunny in {city}";
}
```

## Next steps

- [Release notes](release-notes.md) — package versions
- [Plugins & Prompts](plugins-and-prompts.md) — tools, prompts, and context properties
- [Filters](filters.md) — intercept tool and model calls
- [Avalonia demo](avalonia-demo.md) — streaming chat + digital twin UI
- [Native AOT](aot-compatibility.md) — trimming and source generators
