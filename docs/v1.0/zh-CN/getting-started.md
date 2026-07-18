# 快速开始

DotKernel 是面向 .NET 的轻量级 AI 内核，用特性注解注册插件、提示词、过滤器与实时上下文属性，支持 **Native AOT**，比微软 Semantic Kernel 更轻。

文档版本：**v1.0**。

## 安装

先引用本仓库项目（NuGet 稍后发布）：

```xml
<ProjectReference Include="..\..\src\DotKernel\DotKernel.csproj" />
```

发布后：

```bash
dotnet add package DotKernel
```

## 最小内核

```csharp
using DotKernel;
using Microsoft.Extensions.AI;

var kernel = KernelBuilder.Create()
    .AddChatClient(chatClient) // 你的 IChatClient
    .AddPlugin(new WeatherPlugin())
    .Build();

var result = await kernel.InvokeAsync("西雅图天气怎么样？");
Console.WriteLine(result);
```

## 特性注解插件

```csharp
[KernelPlugin("Weather")]
public partial class WeatherPlugin
{
    [KernelProperty("temperature_unit", "报气温时使用的单位（C 或 F）")]
    public string TemperatureUnit { get; set; } = "C";

    [KernelFunction("get_weather")]
    [KernelDescription("查询城市天气")]
    public string GetWeather([KernelDescription("城市名")] string city)
        => $"{city} 晴天 (°{TemperatureUnit})";
}
```

`[KernelProperty]` 会在每次请求模型时注入实时上下文。详见 [插件与提示词](plugins-and-prompts.md)。

## 延伸阅读

- [插件与提示词](plugins-and-prompts.md)
- [过滤器](filters.md)
- [Avalonia 示例](avalonia-demo.md)
- [Native AOT](aot-compatibility.md)
