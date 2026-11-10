// Coleta os dados que o SITE entrega (via API do BFF + o que é exibido na tela)
// e grava em output/snapshot.json para comparar com o banco.
//
// Uso:  BASE_URL=http://localhost:5173 node scripts/collect.mjs
//       SEASONS=2026,2025,2000 node scripts/collect.mjs      (padrão abaixo)
//       SEASONS=all-years node scripts/collect.mjs           (1950–2026, mais lento)
import { chromium, request } from '@playwright/test';
import { mkdirSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const BASE_URL = (process.env.BASE_URL ?? 'http://localhost:5173').replace(/\/$/, '');
const API = `${BASE_URL}/api/web/v1`;
const OUT = resolve(dirname(fileURLToPath(import.meta.url)), '..', 'output', 'snapshot.json');

const seasonsEnv = process.env.SEASONS ?? '2026,2025,2024,2020,2010,2000,1990,1980,1970,1960,1950';
const SEASONS = seasonsEnv === 'all-years'
  ? Array.from({ length: 2026 - 1950 + 1 }, (_, i) => String(2026 - i))
  : seasonsEnv.split(',').map((s) => s.trim());

const TABS = ['Dashboard', 'Equipes', 'Pilotos', 'Classificação', 'Provas (GPs & Telemetria)',
  'Circuitos', 'Linha do Tempo (1950–Hoje)', 'Hall da Fama'];

const snapshot = { collectedAt: new Date().toISOString(), baseUrl: BASE_URL, api: {}, ui: {}, errors: [] };

async function get(ctx, path, key) {
  const t0 = Date.now();
  const res = await ctx.get(`${API}${path}`);
  const ms = Date.now() - t0;
  if (!res.ok()) { snapshot.errors.push({ path, status: res.status(), ms }); return null; }
  const json = await res.json();
  snapshot.api[key ?? path] = json;
  return json;
}

// ---------- 1) API: coleta direta por temporada ----------
const ctx = await request.newContext({ ignoreHTTPSErrors: true });
await get(ctx, '/context', 'context');
await get(ctx, '/f1/timeline', 'timeline');
await get(ctx, '/f1/hall-of-fame', 'hallOfFame');
await get(ctx, '/f1/circuits?season=all', 'circuits:all');
await get(ctx, '/f1/drivers?season=all', 'drivers:all');
await get(ctx, '/f1/teams?season=all', 'teams:all');

for (const s of SEASONS) {
  process.stdout.write(`temporada ${s}... `);
  await get(ctx, `/f1/standings/drivers?season=${s}`, `standings/drivers:${s}`);
  await get(ctx, `/f1/standings/teams?season=${s}`, `standings/teams:${s}`);
  await get(ctx, `/f1/races?season=${s}`, `races:${s}`);
  await get(ctx, `/f1/dashboard?season=${s}`, `dashboard:${s}`);
  console.log('ok');
}
await ctx.dispose();

// ---------- 2) UI: o que o usuário realmente vê ----------
const browser = await chromium.launch({ channel: 'chromium' }); // Chromium completo (evita o headless-shell)
const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
const uiErrors = [];
page.on('pageerror', (e) => uiErrors.push(`pageerror: ${e.message}`));
page.on('response', (r) => { if (r.url().includes('/api/') && r.status() >= 400) uiErrors.push(`HTTP ${r.status()} ${r.url()}`); });

await page.goto(BASE_URL);
await page.locator('.paddock-strip-col.is-live').first().click();
await page.waitForLoadState('load'); await page.waitForTimeout(1500);

for (const label of TABS) {
  await page.getByRole('button', { name: label, exact: true }).click();
  await page.waitForLoadState('load'); await page.waitForTimeout(1500);
  const tables = await page.$$eval('table.data-table', (ts) =>
    ts.map((t) => ({
      headers: [...t.querySelectorAll('thead th')].map((h) => h.textContent.trim()),
      rows: [...t.querySelectorAll('tbody tr')].map((tr) => [...tr.querySelectorAll('td')].map((td) => td.textContent.trim()))
    })));
  snapshot.ui[label] = {
    title: await page.locator('.panel-title').first().textContent().catch(() => null),
    tables,
    bodyTextLength: (await page.locator('.main-container').innerText()).length
  };
  await page.screenshot({ path: resolve(dirname(OUT), `tela-${label.replace(/\W+/g, '_')}.png`), fullPage: true });
}
snapshot.uiErrors = uiErrors;
await browser.close();

mkdirSync(dirname(OUT), { recursive: true });
writeFileSync(OUT, JSON.stringify(snapshot, null, 1));
console.log(`\nSnapshot salvo em ${OUT}`);
console.log(`Endpoints coletados: ${Object.keys(snapshot.api).length} | falhas de API: ${snapshot.errors.length} | erros de UI: ${uiErrors.length}`);
if (snapshot.errors.length || uiErrors.length) { console.error(snapshot.errors, uiErrors); process.exitCode = 1; }
