# Filters

Filters wrap tool calls and can also hook model requests. Register on `KernelBuilder`:

```csharp
builder.AddFilter<ToolCallHistoryFilter>();
```

Implement `IKernelFilter`:

| Hook | When |
|------|------|
| `OnToolCallAsync` | Around each tool invocation (`Continue` / `Skip` / `Cancel`) |
| `OnBeforeModelCallAsync` | Before each model request (mutate `Messages` / `ChatOptions`) |
| `OnAfterModelCallAsync` | After each model response |

```csharp
public sealed class AuditFilter : IKernelFilter
{
    public async ValueTask<ToolCallFilterResult> OnToolCallAsync(
        ToolCallContext context,
        ToolCallFilterDelegate next,
        CancellationToken cancellationToken)
    {
        // audit context.FullName / Arguments
        return await next(context, cancellationToken);
    }

    public ValueTask OnBeforeModelCallAsync(ModelCallContext context, CancellationToken cancellationToken)
    {
        // e.g. context.ChatOptions!.Temperature = 0.2f;
        return ValueTask.CompletedTask;
    }
}
```

Typical uses:

- Log or audit tool calls (Avalonia demo **Call history**)
- Auto / manual tool approval
- Rate limits or policy checks
- Adjust temperature / strip messages before the model call

Tool results do not appear in the chat bubble UI in the demo — only assistant Markdown is shown; history lives in the twin panel sidebar.
