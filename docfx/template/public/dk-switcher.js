(function () {
  const versionSelect = document.getElementById('dk-version');
  const langSelect = document.getElementById('dk-lang');
  if (!versionSelect || !langSelect) return;

  const rel = document.querySelector('meta[name="docfx:rel"]')?.content ?? '';
  const path = window.location.pathname;

  /** Newest first — must match <option> values and docs/<version>/ folders. */
  const versions = ['v1.0.1', 'v1.0'];
  const defaultVersion = versions[0];

  const docPages = new Set([
    'getting-started',
    'plugins-and-prompts',
    'filters',
    'avalonia-demo',
    'aot-compatibility',
    'release-notes',
    'introduction',
    'index',
  ]);

  function currentLang() {
    return path.includes('/zh-CN/') ? 'zh-CN' : 'en';
  }

  function currentVersion() {
    const match = path.match(/\/docs\/(v[\d.]+)\//i);
    if (match && versions.includes(match[1])) return match[1];
    return defaultVersion;
  }

  function currentPage() {
    const match = path.match(/\/docs\/v[\d.]+\/(?:zh-CN\/)?([^/]+)\.html/i);
    if (match && docPages.has(match[1])) return match[1];
    return 'getting-started';
  }

  function buildDocUrl(version, lang, page) {
    if (!versions.includes(version)) version = defaultVersion;
    const prefix = lang === 'zh-CN'
      ? `docs/${version}/zh-CN/`
      : `docs/${version}/`;
    return rel + prefix + page + '.html';
  }

  versionSelect.value = currentVersion();
  langSelect.value = currentLang();

  versionSelect.addEventListener('change', () => {
    window.location.href = buildDocUrl(versionSelect.value, langSelect.value, currentPage());
  });

  langSelect.addEventListener('change', () => {
    window.location.href = buildDocUrl(versionSelect.value, langSelect.value, currentPage());
  });
})();
