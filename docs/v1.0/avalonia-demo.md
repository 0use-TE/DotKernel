# Avalonia demo

Cross-platform sample under `examples/` (docs version **v1.0**):

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

## Chat provider settings

Use the **API** button in the header to open settings:

| Field | Purpose |
|-------|---------|
| Endpoint | API base URL (default `https://api.deepseek.com`) |
| Model | Model id (default `deepseek-chat`) |
| API key | Secret; leave empty for **Echo** scripted demo |

Click **Apply** to swap the `IChatClient` at runtime (`Kernel.SetChatClient`). Values can also come from `appsettings.json` / user-secrets / `DOTKERNEL_` env vars on startup.

## UI layout

- **Left**: streaming chat with Markdown rendering
- **Right**: digital twin (lighting, conveyor, AGV, robot, inventory, alarms)
- **Bottom of twin panel**: call history (not in chat); **Auto** / **Manual** tool approval (default Auto)
- Twin plugin exposes `[KernelProperty]` values (`agv_zone`, `total_actions`) as live context each turn

## Live demo

Published to `https://0use.net/DotKernel/demo/` via GitHub Actions. CI can inject `DEEPSEEK_API_KEY`; you can still override settings in the UI.
