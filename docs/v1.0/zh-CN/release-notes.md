# 发行说明

## 1.0.0

首个正式版：

- 特性注解插件、提示词、过滤器，以及 `[KernelProperty]` 实时上下文
- `KernelBuilder` / `AddChatClient` / `InvokeAsync` / 流式调用
- 源生成器随 NuGet 包作为 analyzers 分发
- 面向 Native AOT 的设计

> 已知问题：未声明 `CancellationToken` 的同步 `[KernelFunction]` 可能编译失败（CS1501）。已在 **[1.0.1 / v1.0.1 文档](../../v1.0.1/zh-CN/release-notes.md)** 修复。
