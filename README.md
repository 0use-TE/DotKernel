# DotKernel

Lightweight, **Native AOT–friendly** AI kernel for .NET. Register plugins, prompts, and filters with attributes — a simpler alternative to Microsoft Semantic Kernel.

- **Docs:** [https://0use.net/DotKernel/](https://0use.net/DotKernel/)
- **Live demo:** [https://0use.net/DotKernel/demo/](https://0use.net/DotKernel/demo/)
- **Repository:** [https://github.com/0use-TE/DotKernel](https://github.com/0use-TE/DotKernel)

## Features

- Attribute-driven plugins (`[KernelPlugin]`, `[KernelFunction]`, `[KernelPrompt]`)
- Tool-call filter pipeline with auto / manual approval
- Streaming via `InvokeStreamingAsync`
- Source generator for trim-safe static registration
- Avalonia cross-platform demo (Desktop + WebAssembly) with digital twin UI

## Quick start

```bash
git clone https://github.com/0use-TE/DotKernel.git
cd DotKernel
dotnet test
dotnet run --project examples/DotKernel.AvaExample.Desktop
```

### Minimal kernel

```csharp
using DotKernel;
using Microsoft.Extensions.AI;

var kernel = KernelBuilder.Create()
    .AddChatClient(chatClient)
    .AddPlugin(new WeatherPlugin())
    .Build();

var answer = await kernel.InvokeAsync("What's the weather in Seattle?");
```

### Configure DeepSeek (optional)

Edit `examples/DotKernel.AvaExample/appsettings.json`:

```json
{
  "DeepSeek": {
    "ApiKey": "your-key",
    "Endpoint": "https://api.deepseek.com",
    "ModelId": "deepseek-chat"
  }
}
```

Without an API key, the demo uses a built-in **Echo** client that runs a scripted multi-step tool chain.

## Solution layout

```
src/DotKernel/              Core kernel
src/DotKernel.Generators/   AOT source generator
tests/DotKernel.Tests/      Unit tests
examples/
  DotKernel.Example/        Console demo
  DotKernel.AvaExample/     Shared Avalonia UI + twin plugin
  DotKernel.AvaExample.Desktop/
  DotKernel.AvaExample.Browser/
docs/                       DocFX documentation (v1.0 + zh-CN)
```

## Avalonia demo

| Project | Purpose |
|---------|---------|
| `DotKernel.AvaExample` | Shared UI, `BuildingTwinPlugin`, twin state |
| `DotKernel.AvaExample.Desktop` | Desktop app (`PublishAot` in Release) |
| `DotKernel.AvaExample.Browser` | WebAssembly for GitHub Pages |

```bash
# Desktop
dotnet run --project examples/DotKernel.AvaExample.Desktop

# Browser (local)
dotnet run --project examples/DotKernel.AvaExample.Browser
```

**UI**

- Left: streaming chat with Markdown replies
- Right: digital twin (lighting, conveyor, AGV, robot, inventory, alerts)
- Tool calls are logged in the **Call history** panel (not in chat bubbles)
- Toggle **Auto** / **Manual** for tool approval (default: Auto)

## Native AOT publish

```powershell
.\scripts\publish-aot.ps1
```

## Documentation

Build locally:

```bash
dotnet tool update -g docfx
docfx docfx.json
docfx serve _site --port 8080
```

GitHub Actions publishes docs + WASM demo to GitHub Pages on push to `main`. Set repository secret `DEEPSEEK_API_KEY` for the live web demo.

## License

MIT (see repository for details).
