---
_layout: landing
---

# Live demo

Avalonia WebAssembly digital twin demo. CI publishes the build to this path.

Local preview:

```bash
dotnet run --project examples/DotKernel.AvaExample.Browser
```

After pushing to `main`, GitHub Actions merges WASM output into `_site/demo/`.
