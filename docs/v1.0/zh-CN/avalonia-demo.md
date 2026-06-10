# Avalonia 示例

跨平台示例位于 `examples/`：

| 项目 | 作用 |
|------|------|
| `DotKernel.AvaExample` | 共享 UI + `BuildingTwinPlugin` |
| `DotKernel.AvaExample.Desktop` | 桌面端，可 Native AOT 发布 |
| `DotKernel.AvaExample.Browser` | WebAssembly，用于 GitHub Pages |

## 本地运行

```bash
dotnet run --project examples/DotKernel.AvaExample.Desktop
dotnet run --project examples/DotKernel.AvaExample.Browser
```

在 `examples/DotKernel.AvaExample/appsettings.json` 配置 `DeepSeek:ApiKey`。无密钥时使用 **Echo** 脚本演示多步工具链。

## 界面

- **左侧**：流式聊天 + Markdown
- **右侧**：数字孪生（照明、传送带、AGV、机器人、库存、告警）
- **孪生面板底部**：工具调用历史（不在聊天里显示）

## 在线演示

通过 GitHub Actions 发布到 `https://0use.net/DotKernel/demo/`。CI 从仓库 Secret `DEEPSEEK_API_KEY` 注入密钥。
