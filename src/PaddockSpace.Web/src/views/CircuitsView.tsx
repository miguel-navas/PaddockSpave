import React, { useEffect, useState } from 'react';
import { getCountryFlagUrl, paddockApi } from '../api';
import { useGlobalFilter } from '../context/GlobalFilterContext';
import type { Circuit, CircuitDetail } from '../types';

export const CircuitsView: React.FC = () => {
  const { category, seasonScope, scopeLabel } = useGlobalFilter();
  const [circuits, setCircuits] = useState<Circuit[]>([]);
  const [search, setSearch] = useState<string>('');
  const [loading, setLoading] = useState<boolean>(true);
  const [selectedCircuit, setSelectedCircuit] = useState<CircuitDetail | null>(null);

  useEffect(() => {
    setLoading(true);
    Promise.all([
      paddockApi.getCircuits(category, seasonScope),
      seasonScope === 'all' ? Promise.resolve(null) : paddockApi.getRaces(category, seasonScope)
    ])
      .then(([allCircuits, seasonRaces]) => {
        if (!seasonRaces) {
          setCircuits(allCircuits);
          return;
        }
        const roundByCircuitId = new Map<string, number>();
        seasonRaces.forEach((r) => {
          if (r.circuitId && !roundByCircuitId.has(r.circuitId.toLowerCase())) {
            roundByCircuitId.set(r.circuitId.toLowerCase(), r.round);
          }
        });
        const filteredBySeason = allCircuits
          .filter((c) => roundByCircuitId.has(c.circuitId.toLowerCase()))
          .sort(
            (a, b) =>
              (roundByCircuitId.get(a.circuitId.toLowerCase()) ?? 999) -
              (roundByCircuitId.get(b.circuitId.toLowerCase()) ?? 999)
          );
        setCircuits(filteredBySeason);
      })
      .catch(() => setCircuits([]))
      .finally(() => setLoading(false));
  }, [category, seasonScope]);

  const filtered = circuits.filter(
    (c) =>
      !search ||
      c.name.toLowerCase().includes(search.toLowerCase()) ||
      (c.country ?? '').toLowerCase().includes(search.toLowerCase()) ||
      (c.city ?? '').toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div>
      <div className="panel">
        <div className="panel-header">
          <h2 className="panel-title">
            Circuitos da Fórmula 1 ({scopeLabel}) — {filtered.length} pistas
          </h2>
          <input
            type="search"
            className="search-input"
            placeholder="Filtrar por nome, cidade ou país..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>

        {loading ? (
          <p>Carregando catálogo de circuitos...</p>
        ) : (
          <div className="cards-grid">
            {filtered.map((c) => {
              const flagUrl = getCountryFlagUrl(c.country);
              return (
                <div
                  key={c.circuitId}
                  className="entity-card circuit-flag-card"
                  onClick={() =>
                    paddockApi.getCircuitDetail(category, c.circuitId).then((d) => setSelectedCircuit(d))
                  }
                >
                  {flagUrl && (
                    <div className="circuit-flag-banner">
                      <img src={flagUrl} alt={c.country ?? c.name} className="circuit-flag-img" loading="lazy" />
                    </div>
                  )}
                  <div className="kpi-label">{c.country} • {c.city}</div>
                  <h3 style={{ margin: '0.25rem 0 0.5rem' }}>{c.name}</h3>
                  <div className="badge-row">
                    <span className="pill pill-gold">{c.totalRacesHosted} GPs sediados</span>
                    <span className="pill pill-muted">
                      {c.firstSeasonHosted ?? '-'}–{c.lastSeasonHosted ?? '-'}
                    </span>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>

      {selectedCircuit && (
        <div className="modal-backdrop" onClick={() => setSelectedCircuit(null)}>
          <div className="modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="panel-header">
              <div className="gp-flag-cell">
                {getCountryFlagUrl(selectedCircuit.circuit.country) && (
                  <img
                    src={getCountryFlagUrl(selectedCircuit.circuit.country)!}
                    alt={selectedCircuit.circuit.country ?? selectedCircuit.circuit.name}
                    className="gp-flag-modal-thumb"
                  />
                )}
                <div>
                  <h2 className="panel-title">{selectedCircuit.circuit.name}</h2>
                  <div className="brand-sub">
                    {selectedCircuit.circuit.city}, {selectedCircuit.circuit.country} • Coordenadas:{' '}
                    {selectedCircuit.circuit.latitude}, {selectedCircuit.circuit.longitude}
                  </div>
                </div>
              </div>
              <button type="button" className="close-btn" onClick={() => setSelectedCircuit(null)}>
                Fechar ✕
              </button>
            </div>

            <div className="table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Temporada</th>
                    <th>Grande Prêmio</th>
                    <th>Vencedor</th>
                    <th>Equipe</th>
                    <th>Pole Position</th>
                  </tr>
                </thead>
                <tbody>
                  {selectedCircuit.hostedRaces.map((r) => (
                    <tr key={`${r.season}-${r.round}`}>
                      <td>
                        <strong>{r.season}</strong>
                      </td>
                      <td>{r.grandPrixName}</td>
                      <td>
                        <strong>{r.winnerName ?? '-'}</strong>
                      </td>
                      <td>{r.winnerTeam ?? '-'}</td>
                      <td>{r.poleName ?? '-'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

