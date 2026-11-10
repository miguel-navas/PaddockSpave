import React, { useEffect, useState } from 'react';
import { paddockApi } from '../api';
import { DriverDetailModal } from '../components/EntityModals';
import { useGlobalFilter } from '../context/GlobalFilterContext';
import type { Driver, DriverProfile, DriverStanding } from '../types';

interface DriverBannerRow {
  position: number;
  driverId: string;
  driverName: string;
  code?: string | null;
  carNumber?: number | null;
  nationality?: string | null;
  teamName?: string | null;
  colorHex: string;
  textColor: string;
  points: number;
  gpCount: number;
  winsCount: number;
  polesCount: number;
  championships: number;
}

const KNOWN_CAR_NUMBERS: Record<string, number> = {
  antonelli: 12,
  russell: 63,
  hamilton: 44,
  verstappen: 1,
  max_verstappen: 1,
  leclerc: 16,
  norris: 4,
  piastri: 81,
  alonso: 14,
  stroll: 18,
  gasly: 10,
  albon: 23,
  sainz: 55,
  hulkenberg: 27,
  bortoleto: 5,
  bearman: 87,
  ocon: 31,
  tsunoda: 22,
  lawson: 30,
  hadjar: 6,
  doohan: 7,
  colapinto: 43,
  bottas: 77,
  perez: 11,
  ricciardo: 3,
  vettel: 5,
  raikkonen: 7,
  senna: 12,
  prost: 2,
  mansell: 5,
  schumacher: 1,
  michael_schumacher: 1,
  fangio: 1,
  lauda: 12,
  piquet: 6,
  fittipaldi: 8
};

function resolveCarNumber(driverId: string, dbNum?: number | null): number | null {
  if (dbNum && dbNum > 0) return dbNum;
  const k = driverId.toLowerCase();
  if (KNOWN_CAR_NUMBERS[k]) return KNOWN_CAR_NUMBERS[k];
  for (const [slug, num] of Object.entries(KNOWN_CAR_NUMBERS)) {
    if (k.includes(slug)) return num;
  }
  return null;
}

function resolveDriverTeamColor(
  teamId?: string | null,
  teamName?: string | null,
  dbColor?: string | null
): {
  bg: string;
  text: string;
} {
  if (dbColor && dbColor.trim().length >= 4) {
    const hex = dbColor.startsWith('#') ? dbColor.trim() : `#${dbColor.trim()}`;
    return { bg: hex, text: isLightHex(hex) ? '#0b0d13' : '#ffffff' };
  }

  const key = `${teamId ?? ''} ${teamName ?? ''}`.toLowerCase();
  if (key.includes('mercedes')) return { bg: '#00D2BE', text: '#061818' };
  if (key.includes('mclaren')) return { bg: '#FF8000', text: '#0b0d13' };
  if (key.includes('ferrari')) return { bg: '#E8002D', text: '#ffffff' };
  if (key.includes('red_bull') || key.includes('red bull')) return { bg: '#1E41FF', text: '#ffffff' };
  if (key.includes('aston_martin') || key.includes('aston martin')) return { bg: '#006F62', text: '#ffffff' };
  if (key.includes('alpine')) return { bg: '#0093CC', text: '#ffffff' };
  if (key.includes('williams')) return { bg: '#005AFF', text: '#ffffff' };
  if (key.includes('rb') || key.includes('alphatauri') || key.includes('toro_rosso') || key.includes('racing bulls'))
    return { bg: '#4E7C9B', text: '#ffffff' };
  if (key.includes('sauber') || key.includes('kick') || key.includes('alfa'))
    return { bg: '#52E252', text: '#061a06' };
  if (key.includes('haas')) return { bg: '#B6BABD', text: '#0b0d13' };
  if (key.includes('audi')) return { bg: '#F50537', text: '#ffffff' };
  if (key.includes('cadillac')) return { bg: '#909090', text: '#0b0d13' };
  if (key.includes('renault')) return { bg: '#FFF500', text: '#0b0d13' };
  if (key.includes('lotus')) return { bg: '#D4AF37', text: '#0b0d13' };
  if (key.includes('brabham')) return { bg: '#1B4F9C', text: '#ffffff' };
  if (key.includes('benetton')) return { bg: '#00A550', text: '#ffffff' };
  if (key.includes('tyrrell')) return { bg: '#0044AA', text: '#ffffff' };
  if (key.includes('brawn')) return { bg: '#B8FD6E', text: '#0b0d13' };
  if (key.includes('jordan')) return { bg: '#F7D117', text: '#0b0d13' };
  if (key.includes('cooper') || key.includes('brm') || key.includes('vanwall'))
    return { bg: '#00594F', text: '#ffffff' };
  if (key.includes('maserati')) return { bg: '#9B0000', text: '#ffffff' };
  if (key.includes('matra') || key.includes('ligier') || key.includes('prost'))
    return { bg: '#0055A4', text: '#ffffff' };

  return { bg: '#e10600', text: '#ffffff' };
}

function isLightHex(hex: string): boolean {
  const clean = hex.replace('#', '');
  if (clean.length !== 6) return false;
  const r = parseInt(clean.slice(0, 2), 16);
  const g = parseInt(clean.slice(2, 4), 16);
  const b = parseInt(clean.slice(4, 6), 16);
  const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
  return luminance > 0.64;
}

export const DriversView: React.FC = () => {
  const { category, seasonScope, scopeLabel } = useGlobalFilter();
  const [rows, setRows] = useState<DriverBannerRow[]>([]);
  const [search, setSearch] = useState<string>('');
  const [sortBy, setSortBy] = useState<'points' | 'gp'>(
    seasonScope === 'all' ? 'gp' : 'points'
  );
  const [loading, setLoading] = useState<boolean>(true);
  const [selectedProfile, setSelectedProfile] = useState<DriverProfile | null>(null);

  // Sempre que mudar o escopo global: temporada específica -> Pontos; todas -> Nº GP
  useEffect(() => {
    setSortBy(seasonScope === 'all' ? 'gp' : 'points');
  }, [seasonScope]);

  useEffect(() => {
    let active = true;
    setLoading(true);

    Promise.all([
      paddockApi.getDriverStandings(category, seasonScope).catch(() => [] as DriverStanding[]),
      paddockApi.getDrivers(category, seasonScope).catch(() => [] as Driver[])
    ])
      .then(([standings, drivers]) => {
        if (!active) return;

        const driverMap = new Map<string, Driver>();
        drivers.forEach((d) => {
          if (d.driverId) {
            driverMap.set(d.driverId.toLowerCase(), d);
          }
        });

        const sourceList: DriverStanding[] =
          standings.length > 0
            ? standings
            : drivers.map((d, idx) => ({
                season: Number(seasonScope) || 0,
                position: idx + 1,
                driverId: d.driverId,
                driverName: d.fullName,
                code: d.code,
                nationality: d.nationality,
                teamId: null,
                teamName: null,
                teamColor: null,
                points: d.careerPoints,
                wins: d.wins,
                afterRound: d.races,
                photoUrl: d.photoUrl
              }));

        const mapped: DriverBannerRow[] = sourceList.map((st, idx) => {
          const safeId = st.driverId ?? `driver-${idx}`;
          const safeName = st.driverName ?? safeId;
          const full = driverMap.get(safeId.toLowerCase());
          const palette = resolveDriverTeamColor(st.teamId, st.teamName, st.teamColor);

          const gpCount = full?.races ?? st.afterRound ?? 0;
          const winsCount = seasonScope === 'all' ? full?.wins ?? st.wins : st.wins;
          const polesCount = seasonScope === 'all'
            ? full?.poles ?? 0
            : st.poles ?? full?.poles ?? 0;
          const pointsValue =
            seasonScope === 'all'
              ? full?.careerPoints ?? st.points ?? 0
              : st.points ?? 0;
          const carNumber = resolveCarNumber(safeId, full?.permanentNumber);

          return {
            position: st.position || idx + 1,
            driverId: safeId,
            driverName: safeName,
            code: st.code ?? full?.code,
            carNumber,
            nationality: st.nationality ?? full?.nationality,
            teamName: st.teamName,
            colorHex: palette.bg,
            textColor: palette.text,
            points: pointsValue,
            gpCount,
            winsCount,
            polesCount,
            championships: full?.championships ?? 0
          };
        });

        setRows(mapped);
      })
      .catch(() => {
        if (active) setRows([]);
      })
      .finally(() => {
        if (active) setLoading(false);
      });

    return () => {
      active = false;
    };
  }, [category, seasonScope]);

  const openProfile = (driverId: string) => {
    paddockApi.getDriverProfile(category, driverId, seasonScope).then((p) => setSelectedProfile(p));
  };

  const filteredAndSorted = rows
    .filter((d) => {
      if (!search.trim()) return true;
      const q = search.toLowerCase();
      return (
        d.driverName.toLowerCase().includes(q) ||
        (d.code ?? '').toLowerCase().includes(q) ||
        String(d.carNumber ?? '').includes(q) ||
        (d.teamName ?? '').toLowerCase().includes(q) ||
        (d.nationality ?? '').toLowerCase().includes(q)
      );
    })
    .slice()
    .sort((a, b) => {
      if (sortBy === 'gp') {
        if (b.gpCount !== a.gpCount) return b.gpCount - a.gpCount;
        if (b.points !== a.points) return b.points - a.points;
        return b.winsCount - a.winsCount;
      }
      if (b.points !== a.points) return b.points - a.points;
      if (b.winsCount !== a.winsCount) return b.winsCount - a.winsCount;
      return b.gpCount - a.gpCount;
    });

  return (
    <div>
      <div className="panel">
        <div className="panel-header">
          <div>
            <h2 className="panel-title">
              Pilotos — {scopeLabel} ({filteredAndSorted.length})
            </h2>
            <div className="brand-sub">
              Clique sobre qualquer piloto para abrir o modal com desempenho detalhado, carreira e histórico
            </div>
          </div>

          <div className="filter-sort-controls">
            <input
              type="search"
              className="search-input"
              placeholder="Buscar por nome do piloto ou por equipe..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
            <div className="selector-group">
              <label>Classificar por:</label>
              <button
                type="button"
                className={`quick-scope-btn ${sortBy === 'points' ? 'active' : ''}`}
                onClick={() => setSortBy('points')}
              >
                Pontos
              </button>
              <button
                type="button"
                className={`quick-scope-btn ${sortBy === 'gp' ? 'active' : ''}`}
                onClick={() => setSortBy('gp')}
              >
                Nº GP
              </button>
            </div>
          </div>
        </div>

        {loading ? (
          <p>Carregando classificação de pilotos...</p>
        ) : filteredAndSorted.length === 0 ? (
          <p>Nenhum piloto encontrado para o filtro selecionado.</p>
        ) : (
          <div className="team-strips-list">
            {filteredAndSorted.slice(0, 120).map((d, idx) => (
              <div
                key={d.driverId}
                className="driver-strip-entry"
                style={{ '--team-color': d.colorHex, '--team-text': d.textColor } as React.CSSProperties}
                onClick={() => openProfile(d.driverId)}
              >
                {/* Classificação ANTES DE TUDO, sem cor de fundo, em um box com cantos arredondados */}
                <div className="driver-rank-box" title="Posição na listagem atual">
                  {idx + 1}º
                </div>

                {/* Faixa do piloto com a COR DA EQUIPE e o NÚMERO DO PILOTO junto com o nome */}
                <div className="team-strip-row driver-strip-row">
                  <div className="team-strip-color-block">
                    <div className="team-strip-name-wrap">
                      <span className="team-strip-name">
                        {d.carNumber != null ? (
                          <span className="driver-car-number">{d.carNumber}</span>
                        ) : null}
                        {d.driverName.toUpperCase()}
                        {d.code ? ` (${d.code})` : ''}
                      </span>
                      <span className="team-strip-nat">
                        {d.teamName ?? d.nationality ?? 'F1 Driver'}
                        {d.championships > 0 ? ` • ${d.championships}x Campeão Mundial` : ''}
                      </span>
                    </div>
                  </div>

                  {/* Bloco direito: Nº GP, Nº Vitórias, Nº Pole e Pontuação (Temporada ou Somatória Total) */}
                  <div className="team-strip-metrics">
                    <div className="team-strip-stat">
                      <span className="team-strip-stat-label">Nº GP</span>
                      <span className="team-strip-stat-value">{d.gpCount}</span>
                    </div>
                    <div className="team-strip-stat">
                      <span className="team-strip-stat-label">Nº VITÓRIAS</span>
                      <span className="team-strip-stat-value">{d.winsCount}</span>
                    </div>
                    <div className="team-strip-stat">
                      <span className="team-strip-stat-label">Nº POLE</span>
                      <span className="team-strip-stat-value">{d.polesCount}</span>
                    </div>
                    <div className="team-strip-stat">
                      <span className="team-strip-stat-label">
                        {seasonScope === 'all' ? 'TOTAL DE PONTOS' : 'PONTOS NA TEMPORADA'}
                      </span>
                      <span className="team-strip-stat-value">{d.points}</span>
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {selectedProfile && (
        <DriverDetailModal
          profile={selectedProfile}
          seasonLabel={scopeLabel}
          onClose={() => setSelectedProfile(null)}
        />
      )}
    </div>
  );
};
