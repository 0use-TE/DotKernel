# Filters

Filters run around tool invocations. Register on `KernelBuilder`:

```csharp
builder.Filters.Add<ToolCallHistoryFilter>();
```

Implement `IKernelFilter` or use the built-in pipeline hooks to:

- Log or audit tool calls (see Avalonia demo **Call history** panel)
- Auto-approve tools in trusted demos
- Add rate limits or policy checks

Tool results do not appear in the chat bubble UI in the demo — only assistant Markdown is shown; history lives in the twin panel sidebar.
