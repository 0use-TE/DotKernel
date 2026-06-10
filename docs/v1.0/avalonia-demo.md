# Avalonia demo

Cross-platform sample under `examples/`:

| Project | Role |
|---------|------|
| `DotKernel.AvaExample` | Shared UI + `BuildingTwinPlugin` |
| `DotKernel.AvaExample.Desktop` | Desktop, Native AOT publish |
| `DotKernel.AvaExample.Browser` | WebAssembly for GitHub Pages |

## Run locally

```bash
dotnet run --project examples/DotKernel.AvaExample.Desktop
dotnet run --project examples/DotKernel.AvaExample.Browser
```

Set `DeepSeek:ApiKey` in `examples/DotKernel.AvaExample/appsettings.json`. Without a key, **Echo** mode runs a scripted multi-step tool chain.

## UI layout

- **Left**: streaming chat with Markdown rendering
- **Right**: digital twin (lighting, conveyor, AGV, robot, inventory, alarms)
- **Bottom of twin panel**: call history (not in chat); **Auto** / **Manual** tool approval toggle

## Live demo

Published to `https://0use.net/DotKernel/demo/` via GitHub Actions. CI injects `DEEPSEEK_API_KEY` from repository secrets at publish time.
