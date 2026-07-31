# Avalonia 示例

跨平台示例位于 `examples/`（文档版本 **v1.0**）：

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

## 聊天服务设置

顶部 **API** 按钮展开设置：

| 字段 | 作用 |
|------|------|
| Endpoint | API 地址（默认 `https://api.deepseek.com`） |
| Model | 模型名（默认 `deepseek-chat`） |
| API key | 密钥；留空则使用 **Echo** 脚本演示 |

点 **Apply** 会在运行时替换 `IChatClient`（`Kernel.SetChatClient`）。启动默认值可用 user-secrets 或 `DOTKERNEL_` 环境变量（仓库内不再提交 `appsettings.json`）。

## 界面

- **左侧**：流式聊天 + Markdown
- **右侧**：数字孪生（照明、传送带、AGV、机器人、库存、告警）
- **孪生面板底部**：调用历史（不在聊天里）；**Auto** / **Manual** 工具确认（默认 Auto）
- 孪生插件通过 `[KernelProperty]` 暴露 `agv_zone`、`total_actions`，每轮注入实时上下文

## 在线演示

通过 GitHub Actions 发布到 `https://0use.net/DotKernel/demo/`。CI 可注入 `DEEPSEEK_API_KEY`；页面上仍可改设置。
