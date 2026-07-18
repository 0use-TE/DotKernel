# Native AOT

DotKernel 尽量减少运行时反射：

- 特性发现插件，可选 **源生成器** 静态注册
- 聊天客户端基于 `Microsoft.Extensions.AI`
- 桌面示例启用 `PublishAot=true`

```bash
powershell -File scripts/publish-aot.ps1
```

裁剪：必要时为插件类型添加 `[DynamicallyAccessedMembers]`；AOT 发布优先用生成器注册，而非 `AddFromType`。
