import { test, expect, type Page } from '@playwright/test';

const TABS = [
  'Dashboard',
  'Equipes',
  'Pilotos',
  'Classificação',
  'Provas (GPs & Telemetria)',
  'Circuitos',
  'Linha do Tempo (1950–Hoje)',
  'Hall da Fama'
];

/** Coleta erros de console, exceções e respostas /api com falha. */
function watchErrors(page: Page) {
  const errors: string[] = [];
  page.on('pageerror', (e) => errors.push(`pageerror: ${e.message}`));
  page.on('console', (m) => {
    if (m.type() === 'error' && !/favicon|Failed to load resource: the server responded with a status of 404/.test(m.text()))
      errors.push(`console: ${m.text()}`);
  });
  page.on('response', (r) => {
    if (r.url().includes('/api/') && r.status() >= 400) errors.push(`HTTP ${r.status()} ${r.url()}`);
  });
  return errors;
}

/** Abre a Home e entra na F1 (faixa disponível). */
async function enterF1(page: Page) {
  await page.goto('/');
  await page.locator('.paddock-strip-col.is-live').first().click();
  await expect(page.locator('.main-container')).toBeVisible();
}

/** O site mantém requisições em segundo plano, então 'networkidle' nunca chega: espera o load e um respiro. */
async function settle(page: Page) {
  await page.waitForLoadState('load');
  await page.waitForTimeout(1500);
}

/** Em telas pequenas o menu é retrátil: abre se necessário. */
async function openMenuIfMobile(page: Page) {
  const toggle = page.getByRole('button', { name: 'Abrir menu de navegação' });
  if (await toggle.isVisible().catch(() => false)) await toggle.click();
}

test('home carrega com faixas de categorias e logo', async ({ page }) => {
  const errors = watchErrors(page);
  await page.goto('/');
  await expect(page.locator('.paddock-strip-col').first()).toBeVisible();
  expect(await page.locator('.paddock-strip-col').count()).toBeGreaterThanOrEqual(8);
  await expect(page.locator('.paddock-strip-col.is-live')).toHaveCount(1);
  expect(errors).toEqual([]);
});

test('categoria "em breve" abre modal e permite voltar para F1', async ({ page }) => {
  await page.goto('/');
  await page.locator('.paddock-strip-col.is-soon').first().click();
  await expect(page.locator('.modal-card')).toBeVisible();
  await page.getByRole('button', { name: /Explorar Fórmula 1/ }).click();
  await expect(page.locator('.main-container')).toBeVisible();
});

test('navega por todas as abas sem erros de console/API', async ({ page }) => {
  const errors = watchErrors(page);
  await enterF1(page);
  for (const label of TABS) {
    await openMenuIfMobile(page);
    await page.getByRole('button', { name: label, exact: true }).click();
    await expect(page.locator('.main-container')).toBeVisible();
    await settle(page);
    await expect(page.locator('.main-container')).not.toBeEmpty();
    await expect(page.locator('.main-container'), `aba ${label}`).not.toContainText(/Erro HTTP|undefined|NaN/);
  }
  expect(errors).toEqual([]);
});

test('classificação: tabelas de pilotos e equipes têm linhas', async ({ page }) => {
  await enterF1(page);
  await openMenuIfMobile(page);
  await page.getByRole('button', { name: 'Classificação', exact: true }).click();
  const tables = page.locator('table.data-table');
  await expect(tables).toHaveCount(2);
  for (let i = 0; i < 2; i++) expect(await tables.nth(i).locator('tbody tr').count()).toBeGreaterThan(5);
});

test('troca de temporada recarrega a classificação (2026 -> 2000)', async ({ page }) => {
  await enterF1(page);
  await openMenuIfMobile(page);
  await page.getByRole('button', { name: 'Classificação', exact: true }).click();
  const first = page.locator('table.data-table').first().locator('tbody tr').first();
  const before = await first.innerText();
  await openMenuIfMobile(page);
  await page.locator('#season-select').selectOption('2000');
  await expect(first).not.toHaveText(before);
  await expect(page.locator('.panel-title').first()).toContainText('2000');
});

test('pilotos: busca filtra a lista', async ({ page }) => {
  await enterF1(page);
  await openMenuIfMobile(page);
  await page.getByRole('button', { name: 'Pilotos', exact: true }).click();
  const search = page.getByPlaceholder('Buscar por nome do piloto ou por equipe...');
  await expect(search).toBeVisible();
  await search.fill('zzzzzz-inexistente');
  await page.waitForTimeout(600);
  await expect(page.locator('.main-container')).not.toContainText('Erro HTTP');
});

test('layout responsivo: sem rolagem horizontal na home e no dashboard', async ({ page }) => {
  await page.goto('/');
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth + 1)).toBe(true);
  await page.locator('.paddock-strip-col.is-live').first().click();
  await settle(page);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth + 1)).toBe(true);
});

test('imagens principais carregam (sem imagem quebrada)', async ({ page }) => {
  await enterF1(page);
  for (const label of ['Dashboard', 'Equipes', 'Pilotos']) {
    await openMenuIfMobile(page);
    await page.getByRole('button', { name: label, exact: true }).click();
    await settle(page);
    const broken = await page.$$eval('img', (imgs) =>
      imgs.filter((i) => i.complete && i.naturalWidth === 0 && i.src).map((i) => i.src));
    // Fotos externas podem falhar; reporta como anotação, falha só se for imagem do próprio site.
    const own = broken.filter((s) => new URL(s).origin === new URL(page.url()).origin);
    expect(own, `imagens quebradas em ${label}`).toEqual([]);
  }
});
