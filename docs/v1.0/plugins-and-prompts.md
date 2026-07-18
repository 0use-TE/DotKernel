# Plugins & Prompts

## Plugin class

Mark a **partial** class with `[KernelPlugin("name")]` and expose members with attributes. The source generator emits `Register()`.

| Target | Attribute | Notes |
|--------|-----------|-------|
| Method | `[KernelFunction("id")]` | Becomes an AI tool (async OK) |
| Property | `[KernelPrompt("id")]` | Prompt template (`{{$var}}`) |
| Property | `[KernelProperty("name", "description")]` | Live context injected each turn |
| Property | `[PromptVariable]` | Fills `{{$Name}}` in prompts |

## Live context — `[KernelProperty]`

Marked properties are **not** tools. On every model call, DotKernel reads their current values and prepends a temporary system message (not stored in `ChatHistory`):

```csharp
[KernelPlugin("Twin")]
public partial class BuildingTwinPlugin(BuildingTwinState state)
{
    [KernelProperty("agv_zone", "Zone where the AGV currently is")]
    public string AgvZone => state.AgvZoneId;

    [KernelProperty("total_actions", "Twin mutations this session")]
    public int TotalActions => state.TotalActions;
}
```

Example injected text:

```text
## Live context
- Twin.agv_zone (Zone where the AGV currently is): storage
- Twin.total_actions (Twin mutations this session): 3
```

Helpers: `kernel.GetPropertyContext()`, `kernel.RenderPropertyContext()`.

## Prompt properties

```csharp
[KernelPlugin("assistant")]
public partial class AssistantPlugin
{
    [KernelPrompt("system", Role = PromptRole.System)]
    public string SystemInstructions =>
        "Reply in Markdown. Be concise.";
}
```

## Streaming

```csharp
await foreach (var update in kernel.InvokeStreamingAsync(input, history, cancellationToken))
{
    switch (update.Kind)
    {
        case KernelStreamingUpdateKind.TextDelta:
            Console.Write(update.TextDelta);
            break;
        case KernelStreamingUpdateKind.ToolCallCompleted:
            // Log update.ToolName / ToolResult separately if needed
            break;
        case KernelStreamingUpdateKind.Completed:
            break;
    }
}
```

## Source generator (AOT)

Reference `DotKernel` (generator ships in the package). Use `[KernelPlugin]` on **partial** classes — registration is emitted at compile time for trimming-friendly builds.
