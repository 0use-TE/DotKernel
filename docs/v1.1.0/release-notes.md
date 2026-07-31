# Release notes

## 1.1.0

**NuGet:** `dotnet add package DotKernel --version 1.1.0`

- **`UseServiceProvider`:** inject `IServiceProvider` into `[KernelFunction]` parameters
- **`KernelInvokeOptions`:** pass `ChatOptions` (temperature, max tokens, model id, …), configure `MaxToolCallRounds`, and toggle system-prompt / property-context injection
- **`InvokeFunctionAsync`:** call a registered tool by `Plugin.function` or tool name without going through the model
- **System prompts:** `[KernelPrompt(..., Role = System)]` templates are injected automatically on each model call (can be disabled)
- **Filters:** `OnBeforeModelCallAsync` / `OnAfterModelCallAsync` hooks on `IKernelFilter` (default no-op)
- **Introspection:** `kernel.Functions` / `Prompts` / `Properties`
- **Binding:** stronger argument conversion (int/enum/object JSON) and JSON serialization for complex tool return values
- **Analyzers:** DK001–DK003 report at the class location; **DK004** warns when `[KernelFunction]` is used without `[KernelPlugin]`

## 1.0.1

**NuGet:** `dotnet add package DotKernel --version 1.0.1`

- **Fix:** source generator no longer passes an unwanted `CancellationToken` into sync `[KernelFunction]` methods (CS1501).
- Injected parameters (`CancellationToken`, `Kernel`, `IChatClient`) are only passed when present on the method signature.
- Added `Kernel.ChatClient` for optional injection.

## 1.0.0

Initial release:

- Attribute plugins, prompts, filters, and `[KernelProperty]` live context
- `KernelBuilder` / `AddChatClient` / `InvokeAsync` / streaming
- Source generator shipped inside the NuGet package
- Native AOT–oriented design
