# Native AOT

DotKernel avoids heavy reflection at runtime:

- Plugins discovered via attributes + optional **source generator**
- `Microsoft.Extensions.AI` abstractions for chat clients
- Example desktop app sets `PublishAot=true`

```bash
powershell -File scripts/publish-aot.ps1
```

Trimming: mark plugin types with `[DynamicallyAccessedMembers]` where needed; prefer generated registration over `AddFromType` in AOT builds.
