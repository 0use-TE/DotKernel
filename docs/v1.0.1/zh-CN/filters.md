# 过滤器

过滤器包裹工具调用，在 `KernelBuilder` 上注册：

```csharp
builder.Filters.Add<ToolCallHistoryFilter>();
```

实现 `IKernelFilter` 或管道钩子，可以：

- 记录审计日志（Avalonia 示例右侧 **调用历史**）
- 演示环境自动放行工具
- 限流或策略校验

示例里工具调用不出现在聊天气泡，只展示助手 Markdown；历史记录在孪生面板底部列表。
