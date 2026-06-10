# Plugins & Prompts

## Plugin class

Mark a class with `[KernelPlugin("name")]` and expose tools with `[KernelFunction]`.

| Target | Attribute | Notes |
|--------|-----------|-------|
| Method | `[KernelFunction("id")]` | Async supported |
| Property | `[KernelPrompt]` | Injected as system context |

## Prompt properties

```csharp
[KernelPlugin("assistant")]
public class AssistantPlugin
{
    [KernelPrompt]
    public string SystemInstructions =>
        "Reply in Markdown. Be concise.";
}
```

## Streaming

```csharp
await foreach (var update in kernel.InvokeStreamingAsync(history, cancellationToken))
{
    switch (update)
    {
        case KernelStreamingUpdate.TextDelta d:
            Console.Write(d.Text);
            break;
        case KernelStreamingUpdate.ToolCall t:
            // Tool calls are handled by the kernel; log separately if needed
            break;
        case KernelStreamingUpdate.Completed:
            break;
    }
}
```

## Source generator (AOT)

Add `DotKernel.Generators` and use `[KernelPlugin]` on partial classes — registration is emitted at compile time for trimming-friendly builds.
