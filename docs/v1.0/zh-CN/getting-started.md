# 快速开始

DotKernel 是面向 .NET 的轻量级 AI 内核，用特性注解注册插件、提示词与过滤器，支持 **Native AOT**，比微软 Semantic Kernel 更轻。

## 安装

```bash
dotnet add package DotKernel
```

或引用本仓库项目：

```xml
<ProjectReference Include="..\..\src\DotKernel\DotKernel.csproj" />
```

## 最小内核

```csharp
using DotKernel;
using Microsoft.Extensions.AI;

var builder = Kernel.CreateBuilder();
builder.Services.AddSingleton<IChatClient>(sp => /* 你的聊天客户端 */);
builder.Plugins.AddFromType<WeatherPlugin>();
var kernel = builder.Build();

var result = await kernel.InvokeAsync("西雅图天气怎么样？");
Console.WriteLine(result);
```

## 特性注解插件

```csharp
[KernelPlugin("weather")]
public class WeatherPlugin
{
    [KernelFunction("get_weather")]
    [Description("查询城市天气")]
    public string GetWeather([Description("城市名")] string city)
        => $"{city} 晴天";
}
```

## 延伸阅读

- [插件与提示词](plugins-and-prompts.md)
- [过滤器](filters.md)
- [Avalonia 示例](avalonia-demo.md)
- [Native AOT](aot-compatibility.md)
