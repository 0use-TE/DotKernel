# 快速开始

DotKernel 是面向 .NET 的轻量级 AI 内核，用特性注解注册插件、提示词、过滤器与实时上下文属性，支持 **Native AOT**。

文档版本：**v1.0**（对应包 **1.0.0**）。

> **请改用 [v1.0.1](../../v1.0.1/zh-CN/getting-started.md)。** 1.0.0 可能无法编译未声明 `CancellationToken` 的同步 `[KernelFunction]`（CS1501）。请执行：`dotnet add package DotKernel --version 1.0.1`。

## 1. 安装 DotKernel

```bash
dotnet add package DotKernel --version 1.0.0
```

或引用本仓库项目：

```xml
<ProjectReference Include="..\..\src\DotKernel\DotKernel.csproj" />
<ProjectReference Include="..\..\src\DotKernel.Generators\DotKernel.Generators.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

## 2. 创建 `IChatClient`（以 OpenAI 为例）

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

## 3. 最小内核

```csharp
var kernel = KernelBuilder.Create()
    .AddChatClient(chatClient)
    .AddPlugin(new WeatherPlugin())
    .Build();

var result = await kernel.InvokeAsync("西雅图天气怎么样？");
Console.WriteLine(result);
```

## 4. 特性注解插件

```csharp
[KernelPlugin("Weather")]
public partial class WeatherPlugin
{
    [KernelFunction("get_weather")]
    [KernelDescription("查询城市天气")]
    public string GetWeather([KernelDescription("城市名")] string city)
        => $"{city} 晴天";
}
```

若在 1.0.0 下编译失败，请改看 **[v1.0.1 文档](../../v1.0.1/zh-CN/getting-started.md)**，或给方法加上 `CancellationToken cancellationToken = default`。

## 延伸阅读

- [v1.0.1 快速开始](../../v1.0.1/zh-CN/getting-started.md) — 当前包版本
- [插件与提示词](plugins-and-prompts.md)
- [过滤器](filters.md)
- [Avalonia 示例](avalonia-demo.md)
- [Native AOT](aot-compatibility.md)
