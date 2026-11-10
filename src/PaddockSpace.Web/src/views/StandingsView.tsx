import React, { useEffect, useState } from 'react';
import { paddockApi } from '../api';
import { DriverDetailModal, TeamDetailModal } from '../components/EntityModals';
import { useGlobalFilter } from '../context/GlobalFilterContext';
import type { DriverProfile, DriverStanding, TeamProfile, TeamStanding } from '../types';

export const StandingsView: React.FC = () => {
  const { category, seasonScope, scopeLabel } = useGlobalFilter();
  const [drivers, setDrivers] = useState<DriverStanding[]>([]);
  const [teams, setTeams] = useState<TeamStanding[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [selectedDriver, setSelectedDriver] = useState<DriverProfile | null>(null);
  const [selectedTeam, setSelectedTeam] = useState<TeamProfile | null>(null);

  useEffect(() => {
    setLoading(true);
    Promise.all([
      paddockApi.getDriverStandings(category, seasonScope),
      paddockApi.getTeamStandings(category, seasonScope)
    ])
      .then(([d, t]) => {
        setDrivers(d);
        setTeams(t);
      })
      .catch(() => {
        setDrivers([]);
        setTeams([]);
      })
      .finally(() => setLoading(false));
  }, [category, seasonScope]);

  const openDriver = (driverId: string) => {
    paddockApi.getDriverProfile(category, driverId, seasonScope).then((p) => setSelectedDriver(p));
  };

  const openTeam = (teamId: string) => {
    paddockApi.getTeamProfile(category, teamId, seasonScope).then((p) => setSelectedTeam(p));
  };

  return (
    <div>
      <div className="grid-2">
        <div className="panel">
          <div className="panel-header">
            <div>
              <h2 className="panel-title">Classificação de Pilotos — {scopeLabel}</h2>
              <div className="brand-sub">Clique em um piloto para ver detalhes em modal</div>
            </div>
          </div>
          {loading ? (
            <p>Carregando classificação de pilotos...</p>
          ) : (
            <div className="table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Pos</th>
                    <th>Piloto</th>
                    <th>Equipe</th>
                    <th>Vitórias</th>
                    <th>Pontos</th>
                  </tr>
                </thead>
                <tbody>
                  {drivers.slice(0, 50).map((d) => (
                    <tr
                      key={d.driverId}
                      className="clickable-row"
                      onClick={() => openDriver(d.driverId)}
                    >
                      <td>#{d.position}</td>
                      <td>
                        <strong>{d.driverName}</strong>
                      </td>
                      <td>{d.teamName ?? '-'}</td>
                      <td>{d.wins}</td>
                      <td>
                        <strong>{d.points}</strong>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        <div className="panel">
          <div className="panel-header">
            <div>
              <h2 className="panel-title">Classificação de Equipes — {scopeLabel}</h2>
              <div className="brand-sub">Clique em uma equipe para ver detalhes em modal</div>
            </div>
          </div>
          {loading ? (
            <p>Carregando classificação de equipes...</p>
          ) : teams.length === 0 ? (
            <p>Campeonato de Construtores não disputado nesta temporada (instituído em 1958).</p>
          ) : (
            <div className="table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Pos</th>
                    <th>Equipe</th>
                    <th>Nacionalidade</th>
                    <th>Vitórias</th>
                    <th>Pontos</th>
                  </tr>
                </thead>
                <tbody>
                  {teams.slice(0, 50).map((t) => (
                    <tr
                      key={t.teamId}
                      className="clickable-row"
                      onClick={() => openTeam(t.teamId)}
                    >
                      <td>#{t.position}</td>
                      <td>
                        <strong>{t.teamName}</strong>
                      </td>
                      <td>{t.nationality ?? '-'}</td>
                      <td>{t.wins}</td>
                      <td>
                        <strong>{t.points}</strong>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>

      {selectedDriver && (
        <DriverDetailModal
          profile={selectedDriver}
          seasonLabel={scopeLabel}
          onClose={() => setSelectedDriver(null)}
        />
      )}

      {selectedTeam && (
        <TeamDetailModal
          profile={selectedTeam}
          seasonLabel={scopeLabel}
          onClose={() => setSelectedTeam(null)}
          onSelectDriver={(driverId) => openDriver(driverId)}
        />
      )}
    </div>
  );
};
