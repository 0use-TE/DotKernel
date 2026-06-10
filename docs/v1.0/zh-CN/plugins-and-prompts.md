# 插件与提示词

## 插件类

用 `[KernelPlugin("名称")]` 标记类，用 `[KernelFunction]` 暴露工具。

| 目标 | 特性 | 说明 |
|------|------|------|
| 方法 | `[KernelFunction("id")]` | 支持 async |
| 属性 | `[KernelPrompt]` | 作为系统上下文注入 |

## 提示词属性

```csharp
[KernelPlugin("assistant")]
public class AssistantPlugin
{
    [KernelPrompt]
    public string SystemInstructions =>
        "使用 Markdown 回复，尽量简洁。";
}
```

## 流式调用

```csharp
await foreach (var update in kernel.InvokeStreamingAsync(history, cancellationToken))
{
    switch (update)
    {
        case KernelStreamingUpdate.TextDelta d:
            Console.Write(d.Text);
            break;
        case KernelStreamingUpdate.ToolCall t:
            // 工具由内核执行；可在侧栏单独记录历史
            break;
        case KernelStreamingUpdate.Completed:
            break;
    }
}
```

## 源生成器（AOT）

引用 `DotKernel.Generators`，在 partial 类上使用 `[KernelPlugin]`，编译期生成静态注册，便于裁剪。
