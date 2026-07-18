# 快速开始

DotKernel 是面向 .NET 的轻量级 AI 内核，用特性注解注册插件、提示词、过滤器与实时上下文属性，支持 **Native AOT**，比微软 Semantic Kernel 更轻。

文档版本：**v1.0**。

## 1. 安装 DotKernel

先引用本仓库项目（NuGet 稍后发布）：

```xml
<ProjectReference Include="..\..\src\DotKernel\DotKernel.csproj" />
```

同时把源生成器挂成 Analyzer（`[KernelPlugin]` / `[KernelFunction]` 需要它）：

```xml
<ProjectReference Include="..\..\src\DotKernel.Generators\DotKernel.Generators.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

发布后：

```bash
dotnet add package DotKernel
```

## 2. 创建 `IChatClient`（以 OpenAI 为例）

DotKernel 通过 [`Microsoft.Extensions.AI`](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai) 的 `IChatClient` 调用模型。**客户端由你提供**，内核本身不内置任何厂商 SDK。

接入 OpenAI（或兼容 OpenAI 协议的接口）时，先安装：

```bash
dotnet add package Microsoft.Extensions.AI.OpenAI
```

创建客户端，再交给 `AddChatClient`：

```csharp
using System.ClientModel;
using DotKernel;
using Microsoft.Extensions.AI;
using OpenAI;

// 官方 OpenAI
var openAi = new OpenAIClient("sk-...");
IChatClient chatClient = openAi.GetChatClient("gpt-4o-mini").AsIChatClient();

// 或任意 OpenAI 兼容接口（DeepSeek、Azure OpenAI、本地网关等）
var compatible = new OpenAIClient(
    new ApiKeyCredential("your-api-key"),
    new OpenAIClientOptions { Endpoint = new Uri("https://api.deepseek.com") });
IChatClient chatClient = compatible.GetChatClient("deepseek-chat").AsIChatClient();
```

其它实现了 `IChatClient` 的适配器（Azure、Ollama、自封装等）用法相同。

## 3. 最小内核

```csharp
using DotKernel;
using Microsoft.Extensions.AI;

var kernel = KernelBuilder.Create()
    .AddChatClient(chatClient)   // 上一步创建的客户端
    .AddPlugin(new WeatherPlugin())
    .Build();

var result = await kernel.InvokeAsync("西雅图天气怎么样？");
Console.WriteLine(result);
```

必须调用 `AddChatClient`，否则 `Build()` 会抛异常。

## 4. 特性注解插件

类需为 `partial`，由源生成器生成静态注册代码。

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

## 完整控制台示例

```csharp
using System.ClientModel;
using DotKernel;
using Microsoft.Extensions.AI;
using OpenAI;

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException("请设置环境变量 OPENAI_API_KEY。");

IChatClient chatClient = new OpenAIClient(apiKey)
    .GetChatClient("gpt-4o-mini")
    .AsIChatClient();

var kernel = KernelBuilder.Create()
    .AddChatClient(chatClient)
    .AddPlugin(new WeatherPlugin())
    .Build();

Console.WriteLine(await kernel.InvokeAsync("西雅图天气怎么样？"));

[KernelPlugin("Weather")]
public partial class WeatherPlugin
{
    [KernelFunction("get_weather")]
    [KernelDescription("查询城市天气")]
    public string GetWeather([KernelDescription("城市名")] string city)
        => $"{city} 晴天";
}
```

## 延伸阅读

- [插件与提示词](plugins-and-prompts.md)
- [过滤器](filters.md)
- [Avalonia 示例](avalonia-demo.md)
- [Native AOT](aot-compatibility.md)
