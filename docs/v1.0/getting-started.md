# Quick Start

DotKernel is a lightweight AI kernel for .NET with attribute-driven plugins, prompts, and filters. It targets **Native AOT** and avoids the weight of Microsoft Semantic Kernel.

## Install

```bash
dotnet add package DotKernel
```

Or reference the project in this repo:

```xml
<ProjectReference Include="..\..\src\DotKernel\DotKernel.csproj" />
```

## Minimal kernel

```csharp
using DotKernel;
using Microsoft.Extensions.AI;

var builder = Kernel.CreateBuilder();
builder.Services.AddSingleton<IChatClient>(sp => /* your chat client */);
builder.Plugins.AddFromType<WeatherPlugin>();
var kernel = builder.Build();

var result = await kernel.InvokeAsync("What's the weather in Seattle?");
Console.WriteLine(result);
```

## Plugins with attributes

```csharp
[KernelPlugin("weather")]
public class WeatherPlugin
{
    [KernelFunction("get_weather")]
    [Description("Get weather for a city")]
    public string GetWeather([Description("City name")] string city)
        => $"Sunny in {city}";
}
```

## Next steps

- [Plugins & Prompts](plugins-and-prompts.md) — methods, properties, and system prompts
- [Filters](filters.md) — intercept tool calls
- [Avalonia demo](avalonia-demo.md) — streaming chat + digital twin UI
- [Native AOT](aot-compatibility.md) — trimming and source generators
