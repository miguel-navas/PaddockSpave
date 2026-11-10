import { test, expect } from '@playwright/test';

const API = '/api/web/v1';
const SEASONS = ['2026', '2025', '2010', '1990', '1950', 'all'];

test.describe('API (BFF) - contrato e dados básicos', () => {
  test('GET /context lista F1 disponível e temporadas', async ({ request }) => {
    const res = await request.get(`${API}/context`);
    expect(res.status()).toBe(200);
    const ctx = await res.json();
    expect(ctx.categories.find((c: any) => c.code === 'f1')?.isAvailable).toBe(true);
    expect(ctx.availableSeasons).toEqual(expect.arrayContaining([2026, 2025, 1950]));
    expect(ctx.currentSeason).toBeGreaterThanOrEqual(2026);
  });

  for (const season of SEASONS) {
    test.describe(`temporada ${season}`, () => {
      for (const path of ['drivers', 'teams', 'standings/drivers', 'standings/teams', 'races', 'circuits']) {
        test(`GET /f1/${path} retorna lista não vazia`, async ({ request }) => {
          const res = await request.get(`${API}/f1/${path}?season=${season}`);
          expect(res.status(), `${path} ${season}`).toBe(200);
          const body = await res.json();
          expect(Array.isArray(body)).toBe(true);
          // Campeonato de construtores só existe a partir de 1958 (SeasonScope.HasConstructorChampionship)
          const semConstrutores = path === 'standings/teams' && /^\d+$/.test(season) && Number(season) < 1958;
          if (semConstrutores) expect(body.length).toBe(0);
          else expect(body.length).toBeGreaterThan(0);
        });
      }
      test('GET /f1/dashboard responde com KPIs', async ({ request }) => {
        const res = await request.get(`${API}/f1/dashboard?season=${season}`);
        expect(res.status()).toBe(200);
        const d = await res.json();
        expect(d.kpis.totalRaces).toBeGreaterThan(0);
      });
    });
  }

  test('classificação de pilotos 2025 está ordenada e começa em 1', async ({ request }) => {
    const rows = await (await request.get(`${API}/f1/standings/drivers?season=2025`)).json();
    expect(rows[0].position).toBe(1);
    for (let i = 1; i < rows.length; i++) {
      expect(rows[i].position).toBeGreaterThanOrEqual(rows[i - 1].position);
      expect(rows[i].points).toBeLessThanOrEqual(rows[i - 1].points + 0.001);
    }
  });

  test('timeline cobre 1950–2025 sem buracos', async ({ request }) => {
    const rows = await (await request.get(`${API}/f1/timeline`)).json();
    const years = rows.map((r: any) => r.season).sort((a: number, b: number) => a - b);
    expect(years[0]).toBe(1950);
    expect(years.at(-1)).toBeGreaterThanOrEqual(2025);
    for (let y = 1950; y <= 2025; y++) expect(years, `falta ${y}`).toContain(y);
  });

  test('hall da fama retorna campeões com biografia', async ({ request }) => {
    const rows = await (await request.get(`${API}/f1/hall-of-fame`)).json();
    expect(rows.length).toBeGreaterThan(0);
    for (const r of rows) {
      expect(r.originalName || r.driverFullName).toBeTruthy();
      expect(r.titlesCount).toBeGreaterThan(0);
    }
  });

  test('detalhe de um GP e de um circuito', async ({ request }) => {
    const races = await (await request.get(`${API}/f1/races?season=2025`)).json();
    const r = races[0];
    const detail = await request.get(`${API}/f1/races/${r.season}/${r.round}`);
    expect(detail.status()).toBe(200);
    const circuit = await request.get(`${API}/f1/circuits/${encodeURIComponent(r.circuitId)}`);
    expect(circuit.status()).toBe(200);
  });

  test('perfil de piloto e de equipe existem a partir da classificação', async ({ request }) => {
    const st = await (await request.get(`${API}/f1/standings/drivers?season=2025`)).json();
    const d = await request.get(`${API}/f1/drivers/${encodeURIComponent(st[0].driverId)}?season=2025`);
    expect(d.status()).toBe(200);
    const t = await request.get(`${API}/f1/teams/${encodeURIComponent(st[0].teamId)}?season=2025`);
    expect(t.status()).toBe(200);
  });

  test('perfis cuja temporada mais recente não tem posição no campeonato respondem 200', async ({ request }) => {
    // Casos que davam 500 (primeira linha do histórico com ChampionshipPosition NULL)
    for (const id of ['ascari', 'alan_brown', 'alliot']) {
      expect((await request.get(`${API}/f1/drivers/${id}?season=all`)).status(), `piloto ${id}`).toBe(200);
    }
    for (const id of ['brabham', 'cooper', 'brm']) {
      expect((await request.get(`${API}/f1/teams/${id}?season=all`)).status(), `equipe ${id}`).toBe(200);
    }
  });

  test('IDs inexistentes retornam 404', async ({ request }) => {
    expect((await request.get(`${API}/f1/drivers/nao-existe-xyz`)).status()).toBe(404);
    expect((await request.get(`${API}/f1/teams/nao-existe-xyz`)).status()).toBe(404);
    expect((await request.get(`${API}/f1/circuits/nao-existe-xyz`)).status()).toBe(404);
    expect((await request.get(`${API}/f1/races/2025/999`)).status()).toBe(404);
  });

  test('temporada fora de 1950–2026 retorna 400/404, nunca 500', async ({ request }) => {
    // BUG conhecido: SeasonScope.ForYear lança ArgumentOutOfRangeException e o endpoint
    // /races/{season}/{round} não trata (o /dashboard trata). Resultado atual: 500.
    for (const p of ['races/1900/1', 'races/2099/1', 'races?season=1900', 'drivers?season=2099', 'standings/drivers?season=abc', 'dashboard?season=1800']) {
      expect([400, 404], p).toContain((await request.get(`${API}/f1/${p}`)).status());
    }
  });

  test('categoria não suportada no dashboard retorna 400', async ({ request }) => {
    const res = await request.get(`${API}/categoria-invalida/dashboard?season=2026`);
    expect([400, 404]).toContain(res.status());
  });

  test('busca de pilotos filtra resultados', async ({ request }) => {
    const all = await (await request.get(`${API}/f1/drivers?season=all`)).json();
    const some = await (await request.get(`${API}/f1/drivers?season=all&search=senna`)).json();
    expect(some.length).toBeGreaterThan(0);
    expect(some.length).toBeLessThan(all.length);
  });

  test('tempo de resposta razoável nos endpoints principais', async ({ request }) => {
    for (const p of ['context', 'f1/dashboard?season=2026', 'f1/standings/drivers?season=2026', 'f1/timeline']) {
      const t0 = Date.now();
      const res = await request.get(`${API}/${p}`);
      expect(res.ok()).toBe(true);
      expect(Date.now() - t0, p).toBeLessThan(5_000);
    }
  });
});
