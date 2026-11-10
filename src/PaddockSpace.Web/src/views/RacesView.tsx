import React, { useEffect, useState } from 'react';
import { formatLapMs, getCountryFlagUrl, paddockApi } from '../api';
import { useGlobalFilter } from '../context/GlobalFilterContext';
import type { GrandPrix, GrandPrixDetail } from '../types';

export const RacesView: React.FC = () => {
  const { category, seasonScope, scopeLabel } = useGlobalFilter();
  const [races, setRaces] = useState<GrandPrix[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [detail, setDetail] = useState<GrandPrixDetail | null>(null);
  const [subTab, setSubTab] = useState<'race' | 'qualifying' | 'sprint' | 'telemetry'>('race');

  useEffect(() => {
    setLoading(true);
    setDetail(null);
    paddockApi
      .getRaces(category, seasonScope)
      .then((res) => setRaces(res))
      .catch(() => setRaces([]))
      .finally(() => setLoading(false));
  }, [category, seasonScope]);

  const openRace = (season: number, round: number) => {
    paddockApi.getRaceDetail(category, season, round).then((d) => {
      setDetail(d);
      setSubTab('race');
    });
  };

  return (
    <div>
      <div className="panel">
        <div className="panel-header">
          <h2 className="panel-title">Provas / Grandes Prêmios — {scopeLabel} ({races.length})</h2>
        </div>

        {loading ? (
          <p>Carregando calendário e resultados das provas...</p>
        ) : (
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Ano / Etapa</th>
                  <th>Grande Prêmio</th>
                  <th>Circuito / País</th>
                  <th>Data</th>
                  <th>Vencedor</th>
                  <th>Equipe Vencedora</th>
                  <th>Pole Position</th>
                  <th>Volta Mais Rápida</th>
                </tr>
              </thead>
              <tbody>
                {races.map((r) => (
                  <tr
                    key={`${r.season}-${r.round}`}
                    className="clickable-row"
                    onClick={() => openRace(r.season, r.round)}
                  >
                    <td>
                      <strong>
                        {r.season} #{r.round}
                      </strong>
                    </td>
                    <td>
                      <div className="gp-flag-cell">
                        {getCountryFlagUrl(r.country, r.name) && (
                          <img
                            src={getCountryFlagUrl(r.country, r.name)!}
                            alt={r.country ?? r.name}
                            className="gp-flag-thumb"
                            loading="lazy"
                          />
                        )}
                        <div>
                          <strong>{r.name}</strong>{' '}
                          {r.hasSprint ? <span className="pill pill-gold">Sprint</span> : null}
                        </div>
                      </div>
                    </td>
                    <td>
                      {r.circuitName ?? r.circuitId} ({r.country})
                    </td>
                    <td>{r.raceDateUtc ? new Date(r.raceDateUtc).toLocaleDateString('pt-BR') : '-'}</td>
                    <td>
                      <strong>{r.winnerName ?? 'Agendado'}</strong>
                    </td>
                    <td>{r.winnerTeam ?? '-'}</td>
                    <td>{r.poleName ?? '-'}</td>
                    <td>{r.fastestLapName ?? '-'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {detail && (
        <div className="modal-backdrop" onClick={() => setDetail(null)}>
          <div className="modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="panel-header">
              <div className="gp-flag-cell">
                {getCountryFlagUrl(detail.race.country, detail.race.name) && (
                  <img
                    src={getCountryFlagUrl(detail.race.country, detail.race.name)!}
                    alt={detail.race.country ?? detail.race.name}
                    className="gp-flag-modal-thumb"
                  />
                )}
                <div>
                  <h2 className="panel-title">
                    {detail.race.season} — Etapa #{detail.race.round}: {detail.race.name}
                  </h2>
                  <div className="brand-sub">
                    Circuito: {detail.race.circuitName} ({detail.race.country}) • Vencedor:{' '}
                    {detail.race.winnerName ?? 'A definir'}
                  </div>
                </div>
              </div>
              <button type="button" className="close-btn" onClick={() => setDetail(null)}>
                Fechar ✕
              </button>
            </div>

            <div className="badge-row" style={{ marginBottom: '1rem' }}>
              <button
                type="button"
                className={`quick-scope-btn ${subTab === 'race' ? 'active' : ''}`}
                onClick={() => setSubTab('race')}
              >
                Resultado da Corrida ({detail.raceResults.length})
              </button>
              <button
                type="button"
                className={`quick-scope-btn ${subTab === 'qualifying' ? 'active' : ''}`}
                onClick={() => setSubTab('qualifying')}
              >
                Classificatório ({detail.qualifyingResults.length})
              </button>
              {detail.sprintResults.length > 0 && (
                <button
                  type="button"
                  className={`quick-scope-btn ${subTab === 'sprint' ? 'active' : ''}`}
                  onClick={() => setSubTab('sprint')}
                >
                  Sprint ({detail.sprintResults.length})
                </button>
              )}
              {detail.telemetry && (
                <button
                  type="button"
                  className={`quick-scope-btn ${subTab === 'telemetry' ? 'active' : ''}`}
                  onClick={() => setSubTab('telemetry')}
                >
                  Telemetria (Pneus, Pit Stops, Clima & Controle)
                </button>
              )}
            </div>

            {subTab === 'race' && (
              <div className="table-wrap">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Pos</th>
                      <th>Piloto</th>
                      <th>Equipe</th>
                      <th>Grid</th>
                      <th>Voltas</th>
                      <th>Tempo / Status</th>
                      <th>Volta Mais Rápida</th>
                      <th>Pontos</th>
                    </tr>
                  </thead>
                  <tbody>
                    {detail.raceResults.map((res, idx) => (
                      <tr key={`${res.driverId}-${idx}`}>
                        <td>{res.position ?? res.positionText}</td>
                        <td>
                          <strong>{res.driverName}</strong>
                        </td>
                        <td>{res.teamName ?? '-'}</td>
                        <td>{res.gridPosition ?? '-'}</td>
                        <td>{res.laps ?? '-'}</td>
                        <td>{res.timeText ?? res.status ?? '-'}</td>
                        <td>{formatLapMs(res.fastestLapMs)}</td>
                        <td>
                          <strong>{res.points}</strong>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}

            {subTab === 'qualifying' && (
              <div className="table-wrap">
                {detail.qualifyingResults.length === 0 ? (
                  <p>Dados de Q1/Q2/Q3 separados disponíveis a partir de 1994.</p>
                ) : (
                  <table className="data-table">
                    <thead>
                      <tr>
                        <th>Pos</th>
                        <th>Piloto</th>
                        <th>Equipe</th>
                        <th>Q1</th>
                        <th>Q2</th>
                        <th>Q3</th>
                      </tr>
                    </thead>
                    <tbody>
                      {detail.qualifyingResults.map((q) => (
                        <tr key={q.driverId}>
                          <td>#{q.position}</td>
                          <td>
                            <strong>{q.driverName}</strong>
                          </td>
                          <td>{q.teamName ?? '-'}</td>
                          <td>{formatLapMs(q.q1Ms)}</td>
                          <td>{formatLapMs(q.q2Ms)}</td>
                          <td>{formatLapMs(q.q3Ms)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </div>
            )}

            {subTab === 'sprint' && (
              <div className="table-wrap">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Pos</th>
                      <th>Piloto</th>
                      <th>Equipe</th>
                      <th>Grid</th>
                      <th>Voltas</th>
                      <th>Status</th>
                      <th>Pontos</th>
                    </tr>
                  </thead>
                  <tbody>
                    {detail.sprintResults.map((s) => (
                      <tr key={s.driverId}>
                        <td>{s.position ?? s.positionText}</td>
                        <td>
                          <strong>{s.driverName}</strong>
                        </td>
                        <td>{s.teamName ?? '-'}</td>
                        <td>{s.gridPosition ?? '-'}</td>
                        <td>{s.laps ?? '-'}</td>
                        <td>{s.timeText ?? s.status}</td>
                        <td>
                          <strong>{s.points}</strong>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}

            {subTab === 'telemetry' && detail.telemetry && (
              <div>
                <h4>Estratégia de Pneus (Stints)</h4>
                <div className="table-wrap" style={{ marginBottom: '1rem' }}>
                  <table className="data-table">
                    <thead>
                      <tr>
                        <th>Piloto</th>
                        <th>Equipe</th>
                        <th>Stint</th>
                        <th>Composto</th>
                        <th>Volta Início–Fim</th>
                        <th>Total Voltas</th>
                      </tr>
                    </thead>
                    <tbody>
                      {detail.telemetry.tyreStrategies.slice(0, 30).map((st, idx) => (
                        <tr key={idx}>
                          <td>
                            <strong>{st.driverCode}</strong>
                          </td>
                          <td>{st.teamName}</td>
                          <td>#{st.stintNumber}</td>
                          <td>
                            <span className={`tyre-badge tyre-${st.compound ?? 'HARD'}`}>
                              {st.compound ?? 'N/A'}
                            </span>
                          </td>
                          <td>
                            {st.startLap} → {st.endLap}
                          </td>
                          <td>{st.lapsInStint}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                <h4>Controle de Prova (Bandeiras / Safety Car / DRS)</h4>
                <div className="table-wrap">
                  <table className="data-table">
                    <thead>
                      <tr>
                        <th>Volta</th>
                        <th>Categoria</th>
                        <th>Bandeira</th>
                        <th>Mensagem</th>
                      </tr>
                    </thead>
                    <tbody>
                      {detail.telemetry.raceControlEvents.slice(0, 20).map((rc) => (
                        <tr key={rc.id}>
                          <td>{rc.lap ?? '-'}</td>
                          <td>{rc.category ?? '-'}</td>
                          <td>{rc.flag ?? '-'}</td>
                          <td>{rc.message}</td>
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

