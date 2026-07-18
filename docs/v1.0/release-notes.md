# Release notes

## 1.0.0

Initial release:

- Attribute-driven plugins, prompts, filters, and `[KernelProperty]` live context
- `KernelBuilder` / `AddChatClient` / `InvokeAsync` / streaming
- Roslyn source generator packed as NuGet analyzers
- Native AOT–oriented design

> Known issue: sync `[KernelFunction]` methods without `CancellationToken` may fail to compile (CS1501). Fixed in **[1.0.1 / v1.0.1 docs](../v1.0.1/release-notes.md)**.
