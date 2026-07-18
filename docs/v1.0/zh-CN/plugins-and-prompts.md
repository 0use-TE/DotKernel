# 插件与提示词

## 插件类

在 **partial** 类上使用 `[KernelPlugin("名称")]`，用特性暴露成员；源生成器会生成 `Register()`。

| 目标 | 特性 | 说明 |
|------|------|------|
| 方法 | `[KernelFunction("id")]` | 注册为 AI 工具（支持 async） |
| 属性 | `[KernelPrompt("id")]` | 提示词模板（`{{$var}}`） |
| 属性 | `[KernelProperty("名称", "说明")]` | 每轮注入的实时上下文 |
| 属性 | `[PromptVariable]` | 填充模板中的 `{{$Name}}` |

## 实时上下文 — `[KernelProperty]`

标记的属性**不是**工具。每次请求模型前，DotKernel 读取当前值，临时插入一条 System 消息（**不写入** `ChatHistory`）：

```csharp
[KernelPlugin("Twin")]
public partial class BuildingTwinPlugin(BuildingTwinState state)
{
    [KernelProperty("agv_zone", "AGV 当前所在区域")]
    public string AgvZone => state.AgvZoneId;

    [KernelProperty("total_actions", "本会话孪生变更次数")]
    public int TotalActions => state.TotalActions;
}
```

注入示例：

```text
## Live context
- Twin.agv_zone (AGV 当前所在区域): storage
- Twin.total_actions (本会话孪生变更次数): 3
```

也可手动：`kernel.GetPropertyContext()`、`kernel.RenderPropertyContext()`。

## 提示词属性

```csharp
[KernelPlugin("assistant")]
public partial class AssistantPlugin
{
    [KernelPrompt("system", Role = PromptRole.System)]
    public string SystemInstructions =>
        "使用 Markdown 回复，尽量简洁。";
}
```

## 流式调用

```csharp
await foreach (var update in kernel.InvokeStreamingAsync(input, history, cancellationToken))
{
    switch (update.Kind)
    {
        case KernelStreamingUpdateKind.TextDelta:
            Console.Write(update.TextDelta);
            break;
        case KernelStreamingUpdateKind.ToolCallCompleted:
            // 可在侧栏记录 ToolName / ToolResult
            break;
        case KernelStreamingUpdateKind.Completed:
            break;
    }
}
```

## 源生成器（AOT）

引用 `DotKernel`（包内含生成器）。在 **partial** 类上使用 `[KernelPlugin]`，编译期静态注册，便于裁剪。
