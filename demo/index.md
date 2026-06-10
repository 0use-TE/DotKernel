---
_layout: landing
---

# Live demo

数字孪生 Avalonia Web 演示由 CI 构建并发布到本路径。

本地预览：

```bash
dotnet run --project examples/DotKernel.AvaExample.Browser
```

推送至 `main` 后，GitHub Actions 会将 WASM 输出合并到 `_site/demo/`。
