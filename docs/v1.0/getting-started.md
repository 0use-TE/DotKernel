# Quick Start

DotKernel is a lightweight AI kernel for .NET with attribute-driven plugins, prompts, filters, and live context properties. It targets **Native AOT** and avoids the weight of Microsoft Semantic Kernel.

Documentation version: **v1.0** (package **1.0.0**).

> **Prefer [v1.0.1](../v1.0.1/getting-started.md).** Package 1.0.0 can fail compiling sync `[KernelFunction]` methods without `CancellationToken` (CS1501). Upgrade with `dotnet add package DotKernel --version 1.0.1`.

## 1. Install DotKernel

```bash
dotnet add package DotKernel --version 1.0.0
```

Or reference the project in this repo:

```xml
<ProjectReference Include="..\..\src\DotKernel\DotKernel.csproj" />
<ProjectReference Include="..\..\src\DotKernel.Generators\DotKernel.Generators.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

## 2. Create an `IChatClient` (OpenAI example)

```bash
dotnet add package Microsoft.Extensions.AI.OpenAI
```

```csharp
using DotKernel;
using Microsoft.Extensions.AI;
using OpenAI;

IChatClient chatClient = new OpenAIClient("sk-...")
    .GetChatClient("gpt-4o-mini")
    .AsIChatClient();
```

## 3. Minimal kernel

```csharp
var kernel = KernelBuilder.Create()
    .AddChatClient(chatClient)
    .AddPlugin(new WeatherPlugin())
    .Build();

var result = await kernel.InvokeAsync("What's the weather in Seattle?");
Console.WriteLine(result);
```

## 4. Plugins with attributes

```csharp
[KernelPlugin("Weather")]
public partial class WeatherPlugin
{
    [KernelFunction("get_weather")]
    [KernelDescription("Get weather for a city")]
    public string GetWeather([KernelDescription("City name")] string city)
        => $"Sunny in {city}";
}
```

If this sample fails to compile on 1.0.0, switch to **[v1.0.1 docs](../v1.0.1/getting-started.md)** or add `CancellationToken cancellationToken = default` to the method.

## Next steps

- [v1.0.1 Quick Start](../v1.0.1/getting-started.md) — current package
- [Plugins & Prompts](plugins-and-prompts.md)
- [Filters](filters.md)
- [Avalonia demo](avalonia-demo.md)
- [Native AOT](aot-compatibility.md)
