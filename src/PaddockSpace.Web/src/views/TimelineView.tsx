import React, { useEffect, useState } from 'react';
import { paddockApi } from '../api';
import { SeasonSummaryModal } from '../components/EntityModals';
import { useGlobalFilter } from '../context/GlobalFilterContext';
import type { SeasonSummary } from '../types';

export const TimelineView: React.FC = () => {
  const { category, setSeasonScope, setActiveTab } = useGlobalFilter();
  const [seasons, setSeasons] = useState<SeasonSummary[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [selectedSeason, setSelectedSeason] = useState<SeasonSummary | null>(null);

  useEffect(() => {
    setLoading(true);
    paddockApi
      .getTimeline(category)
      .then((res) => setSeasons(res))
      .catch(() => setSeasons([]))
      .finally(() => setLoading(false));
  }, [category]);

  const jumpToSeason = (year: number) => {
    setSelectedSeason(null);
    setSeasonScope(String(year));
    setActiveTab('dashboard');
  };

  return (
    <div>
      <div className="panel">
        <div className="panel-header">
          <div>
            <h2 className="panel-title">
              Linha do Tempo da Fórmula 1 (1950–2025) — {seasons.length} Temporadas
            </h2>
            <div className="brand-sub">
              Clique em qualquer temporada para abrir o modal de resumo da edição
            </div>
          </div>
        </div>

        {loading ? (
          <p>Carregando linha do tempo...</p>
        ) : (
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Temporada</th>
                  <th>GPs</th>
                  <th>Campeão de Pilotos</th>
                  <th>Nacionalidade</th>
                  <th>Equipe do Campeão</th>
                  <th>Vitórias / Pontos</th>
                  <th>Campeã de Construtores</th>
                </tr>
              </thead>
              <tbody>
                {seasons.map((s) => (
                  <tr
                    key={s.season}
                    className="clickable-row"
                    onClick={() => setSelectedSeason(s)}
                  >
                    <td>
                      <span className="pill pill-gold">{s.season}</span>
                    </td>
                    <td>{s.racesCount ?? '-'}</td>
                    <td>
                      <strong>{s.driverChampion ?? '-'}</strong>
                    </td>
                    <td>{s.driverNationality ?? '-'}</td>
                    <td>{s.driverTeam ?? '-'}</td>
                    <td>
                      {s.driverWins ?? 0} vit. ({s.driverPoints ?? 0} pts)
                    </td>
                    <td>
                      <strong>{s.constructorChampion ?? 'Pré-1958'}</strong>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {selectedSeason && (
        <SeasonSummaryModal
          season={selectedSeason}
          onClose={() => setSelectedSeason(null)}
          onExploreSeason={jumpToSeason}
        />
      )}
    </div>
  );
};
