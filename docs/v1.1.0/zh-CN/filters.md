# 过滤器

过滤器包裹工具调用，也可挂钩模型请求。在 `KernelBuilder` 上注册：

```csharp
builder.AddFilter<ToolCallHistoryFilter>();
```

实现 `IKernelFilter`：

| 钩子 | 时机 |
|------|------|
| `OnToolCallAsync` | 每次工具调用前后（`Continue` / `Skip` / `Cancel`） |
| `OnBeforeModelCallAsync` | 每次请求模型前（可改 `Messages` / `ChatOptions`） |
| `OnAfterModelCallAsync` | 每次收到模型响应后 |

```csharp
public sealed class AuditFilter : IKernelFilter
{
    public async ValueTask<ToolCallFilterResult> OnToolCallAsync(
        ToolCallContext context,
        ToolCallFilterDelegate next,
        CancellationToken cancellationToken)
    {
        // 审计 context.FullName / Arguments
        return await next(context, cancellationToken);
    }

    public ValueTask OnBeforeModelCallAsync(ModelCallContext context, CancellationToken cancellationToken)
    {
        // 例如 context.ChatOptions!.Temperature = 0.2f;
        return ValueTask.CompletedTask;
    }
}
```

常见用途：

- 记录审计日志（Avalonia 示例右侧 **调用历史**）
- 演示环境自动 / 手动放行工具
- 限流或策略校验
- 在发模型前调整温度或裁剪消息

示例里工具调用不出现在聊天气泡，只展示助手 Markdown；历史记录在孪生面板底部列表。
