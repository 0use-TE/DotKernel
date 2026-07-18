# Quick Start

DotKernel is a lightweight AI kernel for .NET with attribute-driven plugins, prompts, filters, and live context properties. It targets **Native AOT** and avoids the weight of Microsoft Semantic Kernel.

Documentation version: **v1.0**.

## Install

Reference the project in this repo (NuGet package coming later):

```xml
<ProjectReference Include="..\..\src\DotKernel\DotKernel.csproj" />
```

When published:

```bash
dotnet add package DotKernel
```

## Minimal kernel

```csharp
using DotKernel;
using Microsoft.Extensions.AI;

var kernel = KernelBuilder.Create()
    .AddChatClient(chatClient) // your IChatClient
    .AddPlugin(new WeatherPlugin())
    .Build();

var result = await kernel.InvokeAsync("What's the weather in Seattle?");
Console.WriteLine(result);
```

## Plugins with attributes

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

`[KernelProperty]` values are injected as live context on each model call. See [Plugins & Prompts](plugins-and-prompts.md).

## Next steps

- [Plugins & Prompts](plugins-and-prompts.md) — tools, prompts, and context properties
- [Filters](filters.md) — intercept tool calls
- [Avalonia demo](avalonia-demo.md) — streaming chat + digital twin UI
- [Native AOT](aot-compatibility.md) — trimming and source generators
