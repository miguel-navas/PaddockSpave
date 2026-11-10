import React, { useEffect, useState } from 'react';
import { paddockApi } from '../api';
import { DriverDetailModal, TeamDetailModal } from '../components/EntityModals';
import { useGlobalFilter } from '../context/GlobalFilterContext';
import type { ConstructorTeam, Driver, DriverProfile, TeamProfile, TeamStanding } from '../types';

interface TeamBannerRow {
  teamId: string;
  teamName: string;
  nationality?: string | null;
  driverNamesSearch: string;
  colorHex: string;
  textColor: string;
  points: number;
  gpCount: number;
  totalWins: number;
  totalPoles: number;
  constructorTitles: number;
}

function resolveTeamColor(teamId: string, teamName: string, dbColor?: string | null): {
  bg: string;
  text: string;
} {
  if (dbColor && dbColor.trim().length >= 4) {
    const hex = dbColor.startsWith('#') ? dbColor.trim() : `#${dbColor.trim()}`;
    return { bg: hex, text: isLightHex(hex) ? '#0b0d13' : '#ffffff' };
  }

  const key = `${teamId} ${teamName}`.toLowerCase();
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

  return { bg: '#334155', text: '#ffffff' };
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

export const TeamsView: React.FC = () => {
  const { category, seasonScope, scopeLabel } = useGlobalFilter();
  const [rows, setRows] = useState<TeamBannerRow[]>([]);
  const [search, setSearch] = useState<string>('');
  const [sortBy, setSortBy] = useState<'points' | 'gp'>(
    seasonScope === 'all' ? 'gp' : 'points'
  );
  const [loading, setLoading] = useState<boolean>(true);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [selectedTeam, setSelectedTeam] = useState<TeamProfile | null>(null);
  const [selectedDriver, setSelectedDriver] = useState<DriverProfile | null>(null);

  // Sempre que mudar o escopo global: temporada específica -> Pontos; todas -> Nº GP
  useEffect(() => {
    setSortBy(seasonScope === 'all' ? 'gp' : 'points');
  }, [seasonScope]);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setErrorMsg(null);

    Promise.all([
      paddockApi.getTeamStandings(category, seasonScope).catch(() => [] as TeamStanding[]),
      paddockApi.getTeams(category, seasonScope).catch(() => [] as ConstructorTeam[]),
      paddockApi.getDriverStandings(category, seasonScope).catch(() => [])
    ])
      .then(([standings, teams, driverStandings]) => {
        if (!active) return;

        const teamMap = new Map<string, ConstructorTeam>();
        teams.forEach((t) => {
          if (t.teamId) {
            teamMap.set(t.teamId.toLowerCase(), t);
          }
        });

        // Mapeia nomes de pilotos por equipe para permitir busca por nome de piloto ou equipe
        const teamDriversSearchMap = new Map<string, string>();
        driverStandings.forEach((ds) => {
          const tid = (ds.teamId ?? '').toLowerCase();
          if (tid) {
            const prev = teamDriversSearchMap.get(tid) ?? '';
            teamDriversSearchMap.set(tid, `${prev} ${ds.driverName} ${ds.code ?? ''}`);
          }
        });

        const sourceList: TeamStanding[] =
          standings.length > 0
            ? standings
            : teams.map((t, idx) => ({
                season: Number(seasonScope) || 0,
                position: idx + 1,
                teamId: t.teamId,
                teamName: t.name,
                nationality: t.nationality,
                colorHex: t.colorHex,
                points: t.totalPoints ?? 0,
                wins: t.wins
              }));

        const mapped: TeamBannerRow[] = sourceList.map((st, idx) => {
          const safeId = st.teamId ?? `team-${idx}`;
          const safeName = st.teamName ?? safeId;
          const full = teamMap.get(safeId.toLowerCase());
          const palette = resolveTeamColor(
            safeId,
            safeName,
            st.colorHex ?? full?.colorHex
          );

          const gpCount = full?.races ?? st.afterRound ?? 0;
          const winsCount = seasonScope === 'all' ? full?.wins ?? st.wins : st.wins;
          const polesCount = full?.poles ?? 0;
          const pointsValue =
            seasonScope === 'all'
              ? full?.totalPoints ?? st.points ?? 0
              : st.points ?? 0;

          return {
            teamId: safeId,
            teamName: safeName,
            nationality: st.nationality ?? full?.nationality,
            driverNamesSearch: teamDriversSearchMap.get(safeId.toLowerCase()) ?? '',
            colorHex: palette.bg,
            textColor: palette.text,
            points: pointsValue,
            gpCount,
            totalWins: winsCount,
            totalPoles: polesCount,
            constructorTitles: full?.constructorTitles ?? 0
          };
        });

        setRows(mapped);
      })
      .catch((err: Error) => {
        if (!active) return;
        setErrorMsg(err.message);
        setRows([]);
      })
      .finally(() => {
        if (active) setLoading(false);
      });

    return () => {
      active = false;
    };
  }, [category, seasonScope]);

  const openTeam = (teamId: string) => {
    paddockApi.getTeamProfile(category, teamId, seasonScope).then((p) => setSelectedTeam(p));
  };

  const openDriver = (driverId: string) => {
    paddockApi.getDriverProfile(category, driverId, seasonScope).then((p) => setSelectedDriver(p));
  };

  const filteredAndSorted = rows
    .filter((t) => {
      if (!search.trim()) return true;
      const q = search.toLowerCase();
      return (
        t.teamName.toLowerCase().includes(q) ||
        (t.nationality ?? '').toLowerCase().includes(q) ||
        t.driverNamesSearch.toLowerCase().includes(q)
      );
    })
    .slice()
    .sort((a, b) => {
      if (sortBy === 'gp') {
        if (b.gpCount !== a.gpCount) return b.gpCount - a.gpCount;
        if (b.points !== a.points) return b.points - a.points;
        return b.totalWins - a.totalWins;
      }
      if (b.points !== a.points) return b.points - a.points;
      if (b.totalWins !== a.totalWins) return b.totalWins - a.totalWins;
      return b.gpCount - a.gpCount;
    });

  return (
    <div>
      <div className="panel">
        <div className="panel-header">
          <div>
            <h2 className="panel-title">
              Equipes — {scopeLabel} ({filteredAndSorted.length})
            </h2>
            <div className="brand-sub">
              Clique sobre qualquer equipe para abrir o modal com pilotos, títulos e histórico completo
            </div>
          </div>

          <div className="filter-sort-controls">
            <input
              type="search"
              className="search-input"
              placeholder="Buscar por equipe ou piloto..."
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
          <p>Carregando equipes e cores oficiais...</p>
        ) : errorMsg ? (
          <p>{errorMsg}</p>
        ) : filteredAndSorted.length === 0 ? (
          <p>Nenhuma equipe encontrada para o filtro selecionado.</p>
        ) : (
          <div className="team-strips-list">
            {filteredAndSorted.map((t) => (
              <div
                key={t.teamId}
                className="team-strip-row"
                style={{ '--team-color': t.colorHex, '--team-text': t.textColor } as React.CSSProperties}
                onClick={() => openTeam(t.teamId)}
              >
                {/* Bloco esquerdo diagonal na COR DA EQUIPE sem # e sem número */}
                <div className="team-strip-color-block">
                  <div className="team-strip-name-wrap">
                    <span className="team-strip-name">{t.teamName.toUpperCase()}</span>
                    <span className="team-strip-nat">
                      {t.nationality ?? 'F1 Constructor'}
                      {t.constructorTitles > 0 ? ` • ${t.constructorTitles}x Campeã Mundial` : ''}
                    </span>
                  </div>
                </div>

                {/* Bloco direito: Nº GP, Nº Vitórias, Nº Pole e Pontuação (Temporada ou Total) */}
                <div className="team-strip-metrics">
                  <div className="team-strip-stat">
                    <span className="team-strip-stat-label">Nº GP</span>
                    <span className="team-strip-stat-value">{t.gpCount}</span>
                  </div>
                  <div className="team-strip-stat">
                    <span className="team-strip-stat-label">Nº VITÓRIAS</span>
                    <span className="team-strip-stat-value">{t.totalWins}</span>
                  </div>
                  <div className="team-strip-stat">
                    <span className="team-strip-stat-label">Nº POLE</span>
                    <span className="team-strip-stat-value">{t.totalPoles}</span>
                  </div>
                  <div className="team-strip-stat">
                    <span className="team-strip-stat-label">
                      {seasonScope === 'all' ? 'TOTAL DE PONTOS' : 'PONTOS NA TEMPORADA'}
                    </span>
                    <span className="team-strip-stat-value">{t.points}</span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {selectedTeam && (
        <TeamDetailModal
          profile={selectedTeam}
          seasonLabel={scopeLabel}
          onClose={() => setSelectedTeam(null)}
          onSelectDriver={(driverId) => openDriver(driverId)}
        />
      )}

      {selectedDriver && (
        <DriverDetailModal
          profile={selectedDriver}
          seasonLabel={scopeLabel}
          onClose={() => setSelectedDriver(null)}
        />
      )}
    </div>
  );
};
