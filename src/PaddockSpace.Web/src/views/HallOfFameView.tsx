import React, { useEffect, useState } from 'react';
import { paddockApi } from '../api';
import { useGlobalFilter } from '../context/GlobalFilterContext';
import type { ChampionBio } from '../types';

export const HallOfFameView: React.FC = () => {
  const { category } = useGlobalFilter();
  const [champions, setChampions] = useState<ChampionBio[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [selectedBio, setSelectedBio] = useState<ChampionBio | null>(null);

  useEffect(() => {
    setLoading(true);
    paddockApi
      .getHallOfFame(category)
      .then((res) => setChampions(res))
      .catch(() => setChampions([]))
      .finally(() => setLoading(false));
  }, [category]);

  return (
    <div>
      <div className="panel">
        <div className="panel-header">
          <h2 className="panel-title">Hall da Fama — Campeões Mundiais de Fórmula 1 ({champions.length})</h2>
        </div>

        {loading ? (
          <p>Carregando Hall da Fama...</p>
        ) : (
          <div className="cards-grid">
            {champions.map((c) => (
              <div
                key={c.championId}
                className="entity-card"
                onClick={() => setSelectedBio(c)}
              >
                <div className="badge-row" style={{ marginBottom: '0.5rem' }}>
                  <span className="pill pill-gold">{c.titlesCount}x Campeão Mundial</span>
                  <span className="pill pill-muted">{c.nationality}</span>
                </div>
                <h3 style={{ margin: '0.25rem 0' }}>{c.driverFullName ?? c.originalName}</h3>
                <div className="kpi-sub" style={{ marginBottom: '0.5rem' }}>
                  Títulos: {c.titleYears ?? '-'}
                </div>
                <div className="brand-sub">
                  {c.careerWins} vitórias • {c.careerPodiums} pódios • {c.careerPoles} poles •{' '}
                  {c.historicalPhotos.length} registros fotográficos
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {selectedBio && (
        <div className="modal-backdrop" onClick={() => setSelectedBio(null)}>
          <div className="modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="panel-header">
              <div>
                <h2 className="panel-title">
                  {selectedBio.driverFullName ?? selectedBio.originalName} ({selectedBio.titlesCount}x Campeão)
                </h2>
                <div className="brand-sub">
                  Anos de Título: {selectedBio.titleYears} • Nacionalidade: {selectedBio.nationality}
                </div>
              </div>
              <button type="button" className="close-btn" onClick={() => setSelectedBio(null)}>
                Fechar ✕
              </button>
            </div>

            <div className="kpi-grid">
              <div className="kpi-card">
                <div className="kpi-label">Títulos Mundiais</div>
                <div className="kpi-value">{selectedBio.titlesCount}</div>
              </div>
              <div className="kpi-card">
                <div className="kpi-label">Vitórias</div>
                <div className="kpi-value">{selectedBio.careerWins}</div>
              </div>
              <div className="kpi-card">
                <div className="kpi-label">Pódios</div>
                <div className="kpi-value">{selectedBio.careerPodiums}</div>
              </div>
              <div className="kpi-card">
                <div className="kpi-label">Pole Positions</div>
                <div className="kpi-value">{selectedBio.careerPoles}</div>
              </div>
            </div>

            {selectedBio.biography && (
              <div className="panel">
                <h4 className="panel-title">Biografia Completa</h4>
                <p className="bio-text">{selectedBio.biography}</p>
              </div>
            )}

            {selectedBio.historicalPhotos.length > 0 && (
              <div className="panel">
                <h4 className="panel-title">
                  Acervo Histórico ({selectedBio.historicalPhotos.length} momentos)
                </h4>
                <div className="table-wrap">
                  <table className="data-table">
                    <thead>
                      <tr>
                        <th>#</th>
                        <th>Local & Data</th>
                        <th>Descrição Histórica</th>
                      </tr>
                    </thead>
                    <tbody>
                      {selectedBio.historicalPhotos.map((ph) => (
                        <tr key={ph.order}>
                          <td>#{ph.order}</td>
                          <td>
                            <strong>{ph.location}</strong>
                          </td>
                          <td>{ph.caption}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
};

