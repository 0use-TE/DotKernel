# Quick Start

DotKernel is a lightweight AI kernel for .NET with attribute-driven plugins, prompts, filters, and live context properties. It targets **Native AOT** and avoids the weight of Microsoft Semantic Kernel.

Documentation version: **v1.0.1** · NuGet: **[DotKernel 1.0.1](https://www.nuget.org/packages/DotKernel/)**

## 1. Install DotKernel

```bash
dotnet add package DotKernel --version 1.0.1
```

The NuGet package includes the Roslyn source generator (analyzers). You do **not** need a separate generator reference.

> **Prefer 1.0.1+.** Version 1.0.0 incorrectly passed `CancellationToken` into sync tool methods that do not declare it (compile error CS1501). Upgrade if you hit that.

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
    .Build();

var result = await kernel.InvokeAsync("What's the weather in Seattle?");
Console.WriteLine(result);
```

`AddChatClient` is required — without it, `Build()` throws.

## 4. Plugins with attributes

Mark a `partial` class; the source generator emits static registration code.

```csharp
[KernelPlugin("Weather")]
public partial class WeatherPlugin
{
    [KernelProperty("temperature_unit", "Unit when reporting weather (C or F)")]
    public string TemperatureUnit { get; set; } = "C";

    [KernelFunction("get_weather")]
    [KernelDescription("Get weather for a city")]
    public string GetWeather([KernelDescription("City name")] string city)
        => $"Sunny in {city} (°{TemperatureUnit})";
}
```

Sync methods without `CancellationToken` are fine (fixed in **1.0.1**). Optional `CancellationToken` is injected when you declare it.

`[KernelProperty]` values are injected as live context on each model call. See [Plugins & Prompts](plugins-and-prompts.md).

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
- [Filters](filters.md) — intercept tool calls
- [Avalonia demo](avalonia-demo.md) — streaming chat + digital twin UI
- [Native AOT](aot-compatibility.md) — trimming and source generators
