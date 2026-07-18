# 发行说明

## 1.0.1

**NuGet：** `dotnet add package DotKernel --version 1.0.1`

- **修复：** 源生成器不再给未声明 `CancellationToken` 的 `[KernelFunction]` 多传参数（修复快速开始里同步天气示例的 CS1501）。
- 注入参数（`CancellationToken`、`Kernel`、`IChatClient`）仅在方法签名中存在时才会传入。
- 新增 `Kernel.ChatClient` 属性，便于可选注入。

## 1.0.0

首个正式版：

- 特性注解插件、提示词、过滤器，以及 `[KernelProperty]` 实时上下文
- `KernelBuilder` / `AddChatClient` / `InvokeAsync` / 流式调用
- 源生成器随 NuGet 包作为 analyzers 分发
- 面向 Native AOT 的设计
