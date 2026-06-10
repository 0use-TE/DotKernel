# DocFX 文档系统 — AI 工作提示词（DotKernel）

> 复制给其他 AI，用于维护本仓库 DocFX 文档。  
> 官方文档：https://dotnet.github.io/docfx/

---

## 一、给 AI 的总提示词（可直接复制）

```
你在维护 DotKernel 的 DocFX 文档时，必须遵循：

1. 版本切换：只用顶栏 navbar 的 Version 下拉（docfx/template + dk-switcher.js）。根 toc.yml：Home | v1.0 | API Reference | Live demo。
2. 语言切换：只用顶栏 navbar 的 Lang 下拉（English / 简体中文）。正文里不要写行内双语链接。
3. 双语结构：docs/v1.0/（英文）与 docs/v1.0/zh-CN/（中文）镜像；文件名一一对应。
4. GitHub Pages 项目站：globalMetadata._appBasePath: "/DotKernel/"（线上：https://0use.net/DotKernel/）。
5. docfx.json template：["default", "modern", "docfx/template"]；顶栏在 docfx/template/layout/_master.tmpl，脚本 docfx/template/public/dk-switcher.js。
6. introduction.md 用 redirect_url: getting-started.html。
7. Live demo：CI 将 Browser WASM 发布到 _site/demo/，base href="/DotKernel/demo/"；顶栏 Live demo 链到 demo/。
8. 侧边栏 toc：每语言各一份 toc.yml，只放文档章节。
9. api/ 与 _site/ 不提交；改完 docfx docfx.json，0 error。
10. 跨语言同页切换由 dk-switcher.js 按 html 文件名映射（getting-started、plugins-and-prompts、filters、avalonia-demo、aot-compatibility、introduction、index）。
11. Web 演示密钥：GitHub Secret DEEPSEEK_API_KEY，CI 注入 appsettings.json，勿提交明文 Key。
```

---

## 二、本仓库目录结构

```
仓库根/
├── docfx.json
├── toc.yml
├── index.md
├── docfx/template/
│   ├── layout/_master.tmpl
│   └── public/dk-switcher.{js,css}
├── docs/v1.0/
│   ├── toc.yml
│   ├── getting-started.md
│   ├── plugins-and-prompts.md
│   ├── filters.md
│   ├── avalonia-demo.md
│   ├── aot-compatibility.md
│   ├── introduction.md
│   └── zh-CN/          # 中文镜像 + toc.yml
├── .github/workflows/docs.yml
├── api/                # .gitignore
└── _site/              # .gitignore
```

---

## 三、常用命令

```bash
docfx docfx.json
docfx serve _site --port 8080
dotnet publish examples/DotKernel.AvaExample.Browser/DotKernel.AvaExample.Browser.csproj -c Release
```

---

## 四、新增文档页 checklist

1. 在 `docs/v1.0/` 写英文 `.md`
2. 在 `docs/v1.0/zh-CN/` 写同名中文 `.md`
3. 两边 `toc.yml` 各加一项
4. 在 `dk-switcher.js` 的 `docPages` Set 里加页面 stem（无扩展名）
5. `docfx docfx.json` 验证 0 error
