import React from 'react';
import type { DriverProfile, SeasonSummary, TeamProfile } from '../types';

export const DriverDetailModal: React.FC<{
  profile: DriverProfile;
  seasonLabel: string;
  onClose: () => void;
}> = ({ profile, seasonLabel, onClose }) => {
  const stats = profile.currentSeasonStats;

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal-card" onClick={(e) => e.stopPropagation()}>
        <div className="panel-header">
          <div>
            <h2 className="panel-title">
              {profile.driver.fullName} {profile.driver.code ? `(${profile.driver.code})` : ''}
              {profile.driver.permanentNumber ? ` #${profile.driver.permanentNumber}` : ''}
            </h2>
            <div className="brand-sub">
              Nacionalidade: {profile.driver.nationality ?? '-'} • Nascimento:{' '}
              {profile.driver.dateOfBirth
                ? new Date(profile.driver.dateOfBirth).toLocaleDateString('pt-BR')
                : '-'}{' '}
              • Carreira: {profile.driver.firstSeason ?? '-'}–{profile.driver.lastSeason ?? '-'}
            </div>
          </div>
          <button type="button" className="close-btn" onClick={onClose}>
            Fechar ✕
          </button>
        </div>

        {stats && (
          <div className="panel" style={{ marginBottom: '1rem' }}>
            <h4 className="panel-title">Desempenho na Temporada ({seasonLabel})</h4>
            <div className="kpi-grid" style={{ marginTop: '0.75rem' }}>
              <div className="kpi-card">
                <div className="kpi-label">Classificação & Pontos</div>
                <div className="kpi-value">
                  {stats.championshipPosition ? `#${stats.championshipPosition}` : '-'} (
                  {stats.points ?? 0} pts)
                </div>
                <div className="kpi-sub">GPs disputados: {stats.races ?? 0}</div>
              </div>
              <div className="kpi-card">
                <div className="kpi-label">Vitórias / Pódios</div>
                <div className="kpi-value">
                  {stats.wins ?? 0} / {stats.podiums ?? 0}
                </div>
                <div className="kpi-sub">Chegadas nos pontos: {stats.pointsFinishes ?? 0}</div>
              </div>
              <div className="kpi-card">
                <div className="kpi-label">Poles / Voltas Rápidas</div>
                <div className="kpi-value">
                  {stats.poles ?? 0} / {stats.fastestLaps ?? 0}
                </div>
                <div className="kpi-sub">
                  Sprint: {stats.sprintWins ?? 0} vit. ({stats.sprintPoints ?? 0} pts)
                </div>
              </div>
              <div className="kpi-card">
                <div className="kpi-label">Grid Médio → Chegada Média</div>
                <div className="kpi-value">
                  {stats.avgGrid ?? '-'} → {stats.avgFinish ?? '-'}
                </div>
                <div className="kpi-sub">
                  Abandonos: {stats.retirements ?? 0} • Voltas: {stats.lapsCompleted ?? 0}
                </div>
              </div>
            </div>
          </div>
        )}

        <div className="panel" style={{ marginBottom: '1rem' }}>
          <h4 className="panel-title">Números Consolidados na Carreira (1950–2026)</h4>
          <div className="kpi-grid" style={{ marginTop: '0.75rem' }}>
            <div className="kpi-card">
              <div className="kpi-label">Títulos Mundiais</div>
              <div className="kpi-value">{profile.driver.championships}</div>
              <div className="kpi-sub">Pontos na carreira: {profile.driver.careerPoints}</div>
            </div>
            <div className="kpi-card">
              <div className="kpi-label">Vitórias / Pódios Totais</div>
              <div className="kpi-value">
                {profile.driver.wins} / {profile.driver.podiums}
              </div>
              <div className="kpi-sub">Taxa de vitória: {profile.driver.winPercentage}%</div>
            </div>
            <div className="kpi-card">
              <div className="kpi-label">Poles / Voltas Rápidas Totais</div>
              <div className="kpi-value">
                {profile.driver.poles} / {profile.driver.fastestLaps}
              </div>
            </div>
            <div className="kpi-card">
              <div className="kpi-label">Total de GPs Disputados</div>
              <div className="kpi-value">{profile.driver.races}</div>
              <div className="kpi-sub">
                Período: {profile.driver.firstSeason ?? '-'}–{profile.driver.lastSeason ?? '-'}
              </div>
            </div>
          </div>
        </div>

        {profile.biography?.biography && (
          <div className="panel" style={{ marginBottom: '1rem' }}>
            <h4 className="panel-title">
              Biografia de Campeão Mundial ({profile.biography.titleYears})
            </h4>
            <p className="bio-text">{profile.biography.biography}</p>
          </div>
        )}

        <h4 className="panel-title">Histórico Temporada a Temporada</h4>
        <div className="table-wrap">
          <table className="data-table">
            <thead>
              <tr>
                <th>Ano</th>
                <th>Equipe</th>
                <th>Pos. Campeonato</th>
                <th>GPs</th>
                <th>Vitórias</th>
                <th>Pódios</th>
                <th>Poles</th>
                <th>Pontos</th>
              </tr>
            </thead>
            <tbody>
              {profile.seasonHistory.map((s) => (
                <tr key={s.season}>
                  <td>
                    <strong>{s.season}</strong>
                  </td>
                  <td>{s.teamName ?? '-'}</td>
                  <td>
                    {s.championshipPosition ? (
                      <span className={s.championshipPosition === 1 ? 'pill pill-gold' : ''}>
                        #{s.championshipPosition}
                      </span>
                    ) : (
                      '-'
                    )}
                  </td>
                  <td>{s.races}</td>
                  <td>{s.wins}</td>
                  <td>{s.podiums}</td>
                  <td>{s.poles}</td>
                  <td>
                    <strong>{s.points}</strong>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};

export const TeamDetailModal: React.FC<{
  profile: TeamProfile;
  seasonLabel: string;
  onClose: () => void;
  onSelectDriver?: (driverId: string) => void;
}> = ({ profile, seasonLabel, onClose, onSelectDriver }) => {
  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal-card" onClick={(e) => e.stopPropagation()}>
        <div className="panel-header">
          <div>
            <h2 className="panel-title">{profile.team.name}</h2>
            <div className="brand-sub">
              Nacionalidade: {profile.team.nationality ?? '-'} • Período Ativo:{' '}
              {profile.team.firstSeason ?? '-'}–{profile.team.lastSeason ?? '-'}
            </div>
          </div>
          <button type="button" className="close-btn" onClick={onClose}>
            Fechar ✕
          </button>
        </div>

        <div className="kpi-grid">
          <div className="kpi-card">
            <div className="kpi-label">Títulos de Construtores</div>
            <div className="kpi-value">{profile.team.constructorTitles}</div>
          </div>
          <div className="kpi-card">
            <div className="kpi-label">Vitórias / Pódios na História</div>
            <div className="kpi-value">
              {profile.team.wins} / {profile.team.podiums}
            </div>
          </div>
          <div className="kpi-card">
            <div className="kpi-label">Pole Positions</div>
            <div className="kpi-value">{profile.team.poles}</div>
          </div>
          <div className="kpi-card">
            <div className="kpi-label">GPs Disputados</div>
            <div className="kpi-value">{profile.team.races}</div>
          </div>
        </div>

        {profile.drivers.length > 0 && (
          <div className="panel" style={{ marginBottom: '1rem' }}>
            <h4 className="panel-title">
              Pilotos da Equipe ({seasonLabel}) — {profile.drivers.length}
            </h4>
            <div className="table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Piloto</th>
                    <th>Código</th>
                    <th>Nacionalidade</th>
                    <th>Títulos</th>
                    <th>Vitórias</th>
                    <th>Pódios</th>
                    <th>Poles</th>
                    <th>GPs</th>
                  </tr>
                </thead>
                <tbody>
                  {profile.drivers.map((d) => (
                    <tr
                      key={d.driverId}
                      className={onSelectDriver ? 'clickable-row' : ''}
                      onClick={() => onSelectDriver && onSelectDriver(d.driverId)}
                    >
                      <td>
                        <strong>{d.fullName}</strong>
                      </td>
                      <td>{d.code ?? '-'}</td>
                      <td>{d.nationality ?? '-'}</td>
                      <td>{d.championships}</td>
                      <td>{d.wins}</td>
                      <td>{d.podiums}</td>
                      <td>{d.poles}</td>
                      <td>{d.races}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}

        <h4 className="panel-title">Histórico da Equipe por Temporada</h4>
        <div className="table-wrap">
          <table className="data-table">
            <thead>
              <tr>
                <th>Temporada</th>
                <th>Pos. Construtores</th>
                <th>GPs</th>
                <th>Vitórias</th>
                <th>Pódios</th>
                <th>Poles</th>
                <th>Pontos</th>
              </tr>
            </thead>
            <tbody>
              {profile.seasonHistory.map((s) => (
                <tr key={s.season}>
                  <td>
                    <strong>{s.season}</strong>
                  </td>
                  <td>
                    {s.championshipPosition ? (
                      <span className={s.championshipPosition === 1 ? 'pill pill-gold' : ''}>
                        #{s.championshipPosition}
                      </span>
                    ) : (
                      '-'
                    )}
                  </td>
                  <td>{s.races}</td>
                  <td>{s.wins}</td>
                  <td>{s.podiums}</td>
                  <td>{s.poles}</td>
                  <td>
                    <strong>{s.points}</strong>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};

export const SeasonSummaryModal: React.FC<{
  season: SeasonSummary;
  onClose: () => void;
  onExploreSeason: (year: number) => void;
}> = ({ season, onClose, onExploreSeason }) => {
  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal-card" onClick={(e) => e.stopPropagation()}>
        <div className="panel-header">
          <div>
            <h2 className="panel-title">Temporada {season.season} — Resumo do Campeonato</h2>
            <div className="brand-sub">Total de Grandes Prêmios: {season.racesCount ?? '-'}</div>
          </div>
          <button type="button" className="close-btn" onClick={onClose}>
            Fechar ✕
          </button>
        </div>

        <div className="kpi-grid">
          <div className="kpi-card">
            <div className="kpi-label">Campeão Mundial de Pilotos</div>
            <div className="kpi-value">{season.driverChampion ?? '-'}</div>
            <div className="kpi-sub">Nacionalidade: {season.driverNationality ?? '-'}</div>
          </div>
          <div className="kpi-card">
            <div className="kpi-label">Equipe do Campeão</div>
            <div className="kpi-value">{season.driverTeam ?? '-'}</div>
            <div className="kpi-sub">
              {season.driverWins ?? 0} vitórias • {season.driverPoints ?? 0} pts
            </div>
          </div>
          <div className="kpi-card">
            <div className="kpi-label">Campeã Mundial de Construtores</div>
            <div className="kpi-value">{season.constructorChampion ?? 'Pré-1958'}</div>
            <div className="kpi-sub">
              {season.constructorPoints != null ? `${season.constructorPoints} pts` : ''}
            </div>
          </div>
          <div className="kpi-card">
            <div className="kpi-label">Etapas Disputadas</div>
            <div className="kpi-value">{season.racesCount ?? '-'}</div>
            <div className="kpi-sub">Temporada oficial FIA F1</div>
          </div>
        </div>

        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1rem' }}>
          <button
            type="button"
            className="quick-scope-btn active"
            onClick={() => onExploreSeason(season.season)}
          >
            Carregar Temporada {season.season} no Painel Global →
          </button>
        </div>
      </div>
    </div>
  );
};
