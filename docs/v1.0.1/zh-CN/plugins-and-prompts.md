# 插件与提示词

## 插件类

在 **partial** 类上使用 `[KernelPlugin("名称")]`，用特性暴露成员；源生成器会生成 `Register()`。

| 目标 | 特性 | 说明 |
|------|------|------|
| 方法 | `[KernelFunction("id")]` | 注册为 AI 工具（支持 async） |
| 属性 | `[KernelPrompt("id")]` | **提示词模板**（字符串里可写 `{{$变量}}`） |
| 属性 | `[PromptVariable]` | **模板参数**：填入同类型模板里的 `{{$属性名}}` |
| 属性 | `[KernelProperty("名称", "说明")]` | 每轮注入的实时上下文（与提示词模板无关） |

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

## 提示词模板 — `[KernelPrompt]` 与 `[PromptVariable]`

二者配合使用，用来做**可复用的提示词字符串渲染**，**不是**给模型调用的工具。

| 特性 | 作用 |
|------|------|
| `[KernelPrompt("id")]` | 标在字符串属性上：属性的值就是模板正文；`"id"` 是模板名 |
| `[PromptVariable]` | 标在同类型其它属性上：这些属性的当前值用来替换模板里的 `{{$属性名}}` |

占位符写法：`{{$Name}}`（`$` 后面跟属性名，大小写不敏感）。

### 无变量的固定提示词

只标 `[KernelPrompt]` 即可，模板里可以没有 `{{$...}}`：

```csharp
[KernelPlugin("assistant")]
public partial class AssistantPlugin
{
    [KernelPrompt("system", Role = PromptRole.System)]
    public string SystemInstructions =>
        "使用 Markdown 回复，尽量简洁。";
}
```

`Role` 可选：`System` / `User` / `Assistant`，表示这条提示词的角色。

### 带变量的模板（推荐用 `[KernelPromptClass]`）

专门放「模板 + 参数」的类用 `[KernelPromptClass]`（同样必须是 `partial`）：

```csharp
[KernelPromptClass("Email")]
public partial class EmailPrompt
{
    // 模板参数 → 对应 {{$Recipient}}、{{$Tone}}
    [PromptVariable(Description = "收件人")]
    public string Recipient { get; set; } = "";

    [PromptVariable(Description = "语气", Default = "正式")]
    public string Tone { get; set; } = "正式";

    // 模板本身 → 名字叫 "draft"
    [KernelPrompt("draft")]
    public string DraftTemplate =>
        "给{{$Recipient}}写一封{{$Tone}}邮件：{{$input}}";
}
```

渲染：

```csharp
var email = new EmailPrompt { Recipient = "李经理", Tone = "简洁" };

// 额外变量（没有对应属性时）通过字典传入，例如 {{$input}}
var text = kernel.RenderPrompt<EmailPrompt>("draft", new Dictionary<string, string?>
{
    ["input"] = "申请延期一周",
}, email);

// text == "给李经理写一封简洁邮件：申请延期一周"
```

替换规则：

1. 先读实例上所有 `[PromptVariable]` 属性（可用 `Default`）
2. 再用调用时传入的字典覆盖 / 补充（如 `input`）
3. 模板里未提供值的 `{{$xxx}}` 会原样保留

也可用完整名：`kernel.RenderPrompt("Email.draft", variables, instance)`。

**和 `[KernelProperty]` 的区别**：`PromptVariable` 只在你调用 `RenderPrompt` 时填模板；`KernelProperty` 是每次 `InvokeAsync` 自动塞进对话的实时状态。

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
