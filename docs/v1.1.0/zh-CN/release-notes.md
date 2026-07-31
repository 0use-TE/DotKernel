# 发行说明

## 1.1.0

**NuGet：** `dotnet add package DotKernel --version 1.1.0`

- **`UseServiceProvider`：** 向 `[KernelFunction]` 参数注入 `IServiceProvider`
- **`KernelInvokeOptions`：** 透传 `ChatOptions`（温度、MaxTokens、模型名等），配置 `MaxToolCallRounds`，以及开关 System 提示词 / 实时上下文注入
- **`InvokeFunctionAsync`：** 按 `Plugin.function` 或工具名直接调用，不经过模型
- **System 提示词：** `[KernelPrompt(..., Role = System)]` 会在每次请求模型时自动注入（可关闭）
- **过滤器：** `IKernelFilter` 新增 `OnBeforeModelCallAsync` / `OnAfterModelCallAsync`（默认空实现）
- **内省：** `kernel.Functions` / `Prompts` / `Properties`
- **绑定：** 更强的参数转换（int / enum / 对象 JSON）与复杂返回值的 JSON 序列化
- **分析器：** DK001–DK003 定位到类声明；**DK004** 警告未标 `[KernelPlugin]` 却使用了 `[KernelFunction]`

## 1.0.1

**NuGet：** `dotnet add package DotKernel --version 1.0.1`

- **修复：** 源生成器不再给未声明 `CancellationToken` 的 `[KernelFunction]` 多传参数（修复 CS1501）。
- 注入参数（`CancellationToken`、`Kernel`、`IChatClient`）仅在方法签名中存在时才会传入。
- 新增 `Kernel.ChatClient` 属性，便于可选注入。

## 1.0.0

首个正式版：

- 特性注解插件、提示词、过滤器，以及 `[KernelProperty]` 实时上下文
- `KernelBuilder` / `AddChatClient` / `InvokeAsync` / 流式调用
- 源生成器随 NuGet 包作为 analyzers 分发
- 面向 Native AOT 的设计
