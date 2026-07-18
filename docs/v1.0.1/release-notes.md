# Release notes

## 1.0.1

**NuGet:** `dotnet add package DotKernel --version 1.0.1`

- **Fix:** Source generator no longer passes `CancellationToken` into `[KernelFunction]` methods that do not declare it (fixes CS1501 for sync tools like the Quick Start weather sample).
- Injected parameters (`CancellationToken`, `Kernel`, `IChatClient`) are only supplied when present on the method signature.
- Added `Kernel.ChatClient` getter for optional injection.

## 1.0.0

Initial release:

- Attribute-driven plugins, prompts, filters, and `[KernelProperty]` live context
- `KernelBuilder` / `AddChatClient` / `InvokeAsync` / streaming
- Roslyn source generator packed as NuGet analyzers
- Native AOT–oriented design
