using Dapper;
using Microsoft.EntityFrameworkCore;
using PaddockSpace.Application.Abstractions;
using PaddockSpace.Application.DTOs;
using PaddockSpace.Domain.Entities;
using PaddockSpace.Domain.ValueObjects;
using PaddockSpace.Infrastructure.Persistence;

namespace PaddockSpace.Infrastructure.Repositories;

public sealed class Formula1ReadRepository : IFormula1ReadRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly PaddockSpaceDbContext _dbContext;

    public Formula1ReadRepository(ISqlConnectionFactory connectionFactory, PaddockSpaceDbContext dbContext)
    {
        _connectionFactory = connectionFactory;
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<int>> GetAvailableSeasonsAsync(CancellationToken ct = default)
    {
        await using var conn = _connectionFactory.CreateConnection();
        var histYears = await conn.QueryAsync<int>(
            "SELECT Temporada FROM dbo.HistTemporadas ORDER BY Temporada DESC");

        var list = new List<int> { SeasonScope.CurrentFormula1Season };
        foreach (var y in histYears)
        {
            if (y != SeasonScope.CurrentFormula1Season)
            {
                list.Add(y);
            }
        }

        return list;
    }

    public async Task<SyncExecutionInfoDto?> GetLastSyncExecutionAsync(CancellationToken ct = default)
    {
        await using var conn = _connectionFactory.CreateConnection();
        var last = await conn.QueryFirstOrDefaultAsync<ExecucaoAgenteRow>(
            "SELECT TOP 1 * FROM dbo.ExecucoesAgente ORDER BY ExecucaoId DESC;");

        return last is null
            ? null
            : new SyncExecutionInfoDto(
                last.ExecucaoId,
                last.Inicio,
                last.Fim,
                last.Modo,
                last.SessoesCarregadas,
                last.Sucesso,
                last.Mensagem);
    }

    public async Task<DashboardDataDto> GetCurrentSeasonDashboardAsync(CancellationToken ct = default)
    {
        var scope = SeasonScope.Current();
        var driverStandings = await GetDriverStandingsAsync(scope, ct);
        var teamStandings = await GetTeamStandingsAsync(scope, ct);
        var races = await GetRacesAsync(scope, ct);

        await using var conn = _connectionFactory.CreateConnection();

        var driverStatsRows = (await conn.QueryAsync<VwEstatisticasPilotosRow>(@"
            WITH cor AS (
                SELECT PilotoId,
                       COUNT(*) AS Corridas,
                       SUM(CASE WHEN Posicao = 1 THEN 1 ELSE 0 END) AS Vitorias,
                       SUM(CASE WHEN Posicao <= 3 THEN 1 ELSE 0 END) AS Podios,
                       SUM(CASE WHEN Pontos > 0 THEN 1 ELSE 0 END) AS NosPontos,
                       SUM(CASE WHEN PosicaoTexto IN ('R','D','E','W','N') THEN 1 ELSE 0 END) AS Abandonos,
                       SUM(CASE WHEN Largada = 1 THEN 1 ELSE 0 END) AS Poles,
                       SUM(CASE WHEN VoltaRapidaRank = 1 THEN 1 ELSE 0 END) AS VoltasMaisRapidas,
                       AVG(CAST(NULLIF(Largada,0) AS DECIMAL(5,2))) AS LargadaMedia,
                       AVG(CAST(NULLIF(Posicao,0) AS DECIMAL(5,2))) AS ChegadaMedia,
                       SUM(CASE WHEN Largada > 0 AND Posicao > 0 THEN Largada - Posicao ELSE 0 END) AS PosicoesGanhas,
                       SUM(ISNULL(Voltas,0)) AS VoltasCompletadas
                FROM dbo.ResultadosCorrida
                GROUP BY PilotoId
            )
            SELECT p.PilotoId, RTRIM(ISNULL(p.Nome,'') + ' ' + ISNULL(p.Sobrenome,'')) AS Piloto,
                   p.Codigo, cp.Posicao AS PosicaoCampeonato, cp.Pontos, cor.Corridas,
                   cor.Vitorias, cor.Podios, cor.NosPontos, cor.Abandonos, cor.Poles,
                   cor.VoltasMaisRapidas, 0 AS VitoriasSprint,
                   CAST(0 AS DECIMAL(5,1)) AS PontosSprint,
                   cor.LargadaMedia AS ClassifMedia, cor.LargadaMedia, cor.ChegadaMedia,
                   cor.PosicoesGanhas, cor.VoltasCompletadas
            FROM cor
            JOIN dbo.Pilotos p ON p.PilotoId = cor.PilotoId
            LEFT JOIN dbo.ClassificacaoPilotos cp ON cp.PilotoId = cor.PilotoId
            ORDER BY ISNULL(cp.Posicao, 999), cor.Vitorias DESC, cor.Podios DESC;")).ToList();

        var driverStats = driverStatsRows.Select(r => new DriverSeasonStatsDto(
            r.PilotoId,
            r.Piloto ?? r.PilotoId,
            r.Codigo,
            r.PosicaoCampeonato,
            r.Pontos,
            r.Corridas,
            r.Vitorias,
            r.Podios,
            r.NosPontos,
            r.Abandonos,
            r.Poles,
            r.VoltasMaisRapidas,
            r.VitoriasSprint,
            r.PontosSprint,
            r.ClassifMedia,
            r.LargadaMedia,
            r.ChegadaMedia,
            r.PosicoesGanhas,
            r.VoltasCompletadas)).ToList();

        var fastestPitStops = (await conn.QueryAsync<VwPitStopsMaisRapidosRow>(
            "SELECT TOP 15 GrandePremio, Piloto, Equipe, Volta, DuracaoSeg FROM dbo.vw_PitStopsMaisRapidos ORDER BY DuracaoSeg ASC"))
            .Select(p => new PitStopHighlightDto(p.GrandePremio, p.Piloto ?? "-", p.Equipe, p.Volta, p.DuracaoSeg))
            .ToList();

        var tyreStrategies = (await conn.QueryAsync<VwEstrategiaPneusRow>(
            "SELECT TOP 40 * FROM dbo.vw_EstrategiaPneus WHERE Sessao = 'Race' ORDER BY Rodada DESC, Piloto, Stint"))
            .Select(s => new TyreStrategyDto(s.Rodada, s.GrandePremio, s.Sessao, s.Piloto, s.Equipe, s.Stint, s.Composto, s.VoltaInicio, s.VoltaFim, s.VoltasNoStint, s.IdadePneuInicio))
            .ToList();

        var weatherSummaries = (await conn.QueryAsync<VwClimaSessaoRow>(
            "SELECT * FROM dbo.vw_ClimaSessao WHERE Sessao IN ('Race', 'Qualifying', 'Sprint') ORDER BY Rodada DESC"))
            .Select(w => new SessionWeatherSummaryDto(w.Rodada, w.GrandePremio, w.Sessao, w.TempArMedia, w.TempPistaMedia, w.TempPistaMax, w.UmidadeMedia, (w.TeveChuva ?? 0) > 0, w.VentoMedio))
            .ToList();

        var sessions = (await conn.QueryAsync<VwCalendarioRow>(
            "SELECT TOP 20 * FROM dbo.vw_Calendario ORDER BY InicioBrasilia DESC"))
            .Select(c => new CalendarSessionDto(c.Rodada, c.GrandePremio, c.Circuito, c.Pais, c.Sessao, c.InicioBrasilia, c.Cancelada, c.DadosCarregados, c.TemSprint ?? false, c.SessaoKey))
            .ToList();

        var completedRaces = races.Count(r => !string.IsNullOrWhiteSpace(r.WinnerName));
        var nextOrLastGp = races.FirstOrDefault(r => string.IsNullOrWhiteSpace(r.WinnerName))
                           ?? races.LastOrDefault(r => !string.IsNullOrWhiteSpace(r.WinnerName));

        var leaderDriver = driverStandings.FirstOrDefault();
        var leaderTeam = teamStandings.FirstOrDefault();

        var kpis = new DashboardKpisDto(
            races.Count,
            completedRaces,
            driverStandings.Count,
            teamStandings.Count,
            races.Select(r => r.CircuitId).Where(x => x is not null).Distinct().Count(),
            leaderDriver?.DriverName,
            leaderDriver?.Points,
            leaderTeam?.TeamName,
            leaderTeam?.Points,
            nextOrLastGp?.Name,
            nextOrLastGp?.RaceDateUtc?.AddHours(-3));

        return new DashboardDataDto(
            "f1",
            scope.ToKey(),
            scope.Kind.ToString(),
            scope.Year,
            ToCapabilities(scope),
            kpis,
            driverStandings,
            teamStandings,
            races,
            driverStats,
            fastestPitStops,
            tyreStrategies,
            weatherSummaries,
            sessions,
            [],
            [],
            []);
    }

    public async Task<DashboardDataDto> GetHistoricalSeasonDashboardAsync(int year, CancellationToken ct = default)
    {
        var scope = SeasonScope.ForYear(year);
        var driverStandings = await GetDriverStandingsAsync(scope, ct);
        var teamStandings = await GetTeamStandingsAsync(scope, ct);
        var races = await GetRacesAsync(scope, ct);

        await using var conn = _connectionFactory.CreateConnection();
        var driverStatsSql = @"
            WITH cor AS (
                SELECT PilotoId,
                       COUNT(*) AS Corridas,
                       SUM(CASE WHEN Posicao = 1 THEN 1 ELSE 0 END) AS Vitorias,
                       SUM(CASE WHEN Posicao <= 3 THEN 1 ELSE 0 END) AS Podios,
                       SUM(CASE WHEN Pontos > 0 THEN 1 ELSE 0 END) AS NosPontos,
                       SUM(CASE WHEN PosicaoTexto IN ('R','D','E','W','N') THEN 1 ELSE 0 END) AS Abandonos,
                       SUM(CASE WHEN Largada = 1 THEN 1 ELSE 0 END) AS Poles,
                       SUM(CASE WHEN VoltaRapidaRank = 1 THEN 1 ELSE 0 END) AS VoltasMaisRapidas,
                       AVG(CAST(NULLIF(Largada,0) AS DECIMAL(5,2))) AS LargadaMedia,
                       AVG(CAST(NULLIF(Posicao,0) AS DECIMAL(5,2))) AS ChegadaMedia,
                       SUM(CASE WHEN Largada > 0 AND Posicao > 0 THEN Largada - Posicao ELSE 0 END) AS PosicoesGanhas,
                       SUM(ISNULL(Voltas,0)) AS VoltasCompletadas
                FROM dbo.HistResultadosCorrida
                WHERE Temporada = @Year
                GROUP BY PilotoId
            )
            SELECT p.PilotoId,
                   RTRIM(ISNULL(p.Nome,'') + ' ' + ISNULL(p.Sobrenome,'')) AS Piloto,
                   p.Codigo,
                   cp.Posicao AS PosicaoCampeonato,
                   cp.Pontos,
                   cor.Corridas,
                   cor.Vitorias,
                   cor.Podios,
                   cor.NosPontos,
                   cor.Abandonos,
                   cor.Poles,
                   cor.VoltasMaisRapidas,
                   0 AS VitoriasSprint,
                   CAST(0 AS DECIMAL(5,1)) AS PontosSprint,
                   cor.LargadaMedia AS ClassifMedia,
                   cor.LargadaMedia,
                   cor.ChegadaMedia,
                   cor.PosicoesGanhas,
                   cor.VoltasCompletadas
            FROM cor
            JOIN dbo.Pilotos p ON p.PilotoId = cor.PilotoId
            LEFT JOIN dbo.HistClassificacaoPilotos cp ON cp.Temporada = @Year AND cp.PilotoId = cor.PilotoId
            ORDER BY ISNULL(cp.Posicao, 999), cor.Vitorias DESC, cor.Podios DESC;";

        var driverStatsRows = (await conn.QueryAsync<VwEstatisticasPilotosRow>(driverStatsSql, new { Year = year })).ToList();
        var driverStats = driverStatsRows.Select(r => new DriverSeasonStatsDto(
            r.PilotoId,
            r.Piloto ?? r.PilotoId,
            r.Codigo,
            r.PosicaoCampeonato,
            r.Pontos,
            r.Corridas,
            r.Vitorias,
            r.Podios,
            r.NosPontos,
            r.Abandonos,
            r.Poles,
            r.VoltasMaisRapidas,
            r.VitoriasSprint,
            r.PontosSprint,
            r.ClassifMedia,
            r.LargadaMedia,
            r.ChegadaMedia,
            r.PosicoesGanhas,
            r.VoltasCompletadas)).ToList();

        var leaderDriver = driverStandings.FirstOrDefault();
        var leaderTeam = teamStandings.FirstOrDefault();
        var lastGp = races.LastOrDefault();

        var kpis = new DashboardKpisDto(
            races.Count,
            races.Count,
            driverStats.Count,
            teamStandings.Count,
            races.Select(r => r.CircuitId).Where(x => x is not null).Distinct().Count(),
            leaderDriver?.DriverName,
            leaderDriver?.Points,
            leaderTeam?.TeamName,
            leaderTeam?.Points,
            lastGp?.Name,
            lastGp?.RaceDateUtc);

        return new DashboardDataDto(
            "f1",
            scope.ToKey(),
            scope.Kind.ToString(),
            scope.Year,
            ToCapabilities(scope),
            kpis,
            driverStandings,
            teamStandings,
            races,
            driverStats,
            [],
            [],
            [],
            [],
            [],
            [],
            []);
    }

    public async Task<DashboardDataDto> GetAllTimeDashboardAsync(CancellationToken ct = default)
    {
        var scope = SeasonScope.AllTime();
        var topDrivers = (await GetDriversAsync(scope, null, ct)).Take(25).ToList();
        var topTeams = (await GetTeamsAsync(scope, null, ct)).Take(20).ToList();
        var timeline = await GetTimelineAsync(ct);
        var circuits = await GetCircuitsAsync(scope, ct);

        await using var conn = _connectionFactory.CreateConnection();
        var totalRaces = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.vw_GrandesPremiosTodos");
        var totalDrivers = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Pilotos");
        var totalTeams = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Equipes");

        var topDriver = topDrivers.FirstOrDefault();
        var topTeam = topTeams.FirstOrDefault();
        var latestSeason = timeline.FirstOrDefault();

        var kpis = new DashboardKpisDto(
            totalRaces,
            totalRaces,
            totalDrivers,
            totalTeams,
            circuits.Count,
            topDriver is not null ? $"{topDriver.FullName} ({topDriver.Championships}x Campeão / {topDriver.Wins} vitórias)" : null,
            topDriver?.Wins,
            topTeam is not null ? $"{topTeam.Name} ({topTeam.ConstructorTitles} títulos / {topTeam.Wins} vitórias)" : null,
            topTeam?.Wins,
            latestSeason is not null ? $"Temporada {latestSeason.Season} ({latestSeason.DriverChampion})" : null,
            null);

        return new DashboardDataDto(
            "f1",
            scope.ToKey(),
            scope.Kind.ToString(),
            null,
            ToCapabilities(scope),
            kpis,
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            topDrivers,
            topTeams,
            timeline.Take(20).ToList());
    }

    public async Task<IReadOnlyList<Driver>> GetDriversAsync(SeasonScope scope, string? search = null, CancellationToken ct = default)
    {
        await using var conn = _connectionFactory.CreateConnection();

        HashSet<string>? allowedIds = null;
        if (scope.IsCurrent)
        {
            var ids = await conn.QueryAsync<string>(@"
                SELECT DISTINCT PilotoId FROM dbo.ClassificacaoPilotos WHERE PilotoId IS NOT NULL
                UNION
                SELECT DISTINCT PilotoId FROM dbo.ResultadosCorrida WHERE PilotoId IS NOT NULL");
            allowedIds = new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);
        }
        else if (scope.IsHistorical && scope.Year.HasValue)
        {
            var ids = await conn.QueryAsync<string>(
                "SELECT DISTINCT PilotoId FROM dbo.HistResultadosCorrida WHERE Temporada = @Year AND PilotoId IS NOT NULL",
                new { Year = scope.Year.Value });
            allowedIds = new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);
        }

        var rows = (await conn.QueryAsync<DriverRowProjection>(@"
            SELECT p.PilotoId AS DriverId,
                   p.Codigo AS Code,
                   p.NumeroPermanente AS PermanentNumber,
                   p.Nome AS FirstName,
                   p.Sobrenome AS LastName,
                   p.DataNascimento AS DateOfBirthUtc,
                   p.Nacionalidade AS Nationality,
                   p.FotoUrl AS PhotoUrl,
                   ISNULL(v.Titulos, 0) AS Championships,
                   ISNULL(v.Corridas, 0) AS Races,
                   ISNULL(v.Vitorias, 0) AS Wins,
                   ISNULL(v.Podios, 0) AS Podiums,
                   ISNULL(v.Poles, 0) AS Poles,
                   ISNULL(v.VoltasMaisRapidas, 0) AS FastestLaps,
                   ISNULL(v.Pontos, 0) AS CareerPoints,
                   v.PrimeiraTemporada AS FirstSeason,
                   v.UltimaTemporada AS LastSeason,
                   ISNULL(v.PercVitorias, 0) AS WinPercentage
            FROM dbo.Pilotos p
            LEFT JOIN dbo.vw_CarreiraPilotos v ON v.PilotoId = p.PilotoId
            ORDER BY ISNULL(v.Titulos, 0) DESC, ISNULL(v.Vitorias, 0) DESC, ISNULL(v.Podios, 0) DESC, ISNULL(v.Pontos, 0) DESC;")).ToList();

        if (scope.IsCurrent || (scope.IsHistorical && scope.Year.HasValue))
        {
            var poleRows = scope.IsCurrent
                ? await conn.QueryAsync<(string PilotoId, int Poles)>(@"
                    SELECT PilotoId, COUNT(*) AS Poles
                    FROM dbo.ResultadosCorrida
                    WHERE Largada = 1
                    GROUP BY PilotoId;")
                : await conn.QueryAsync<(string PilotoId, int Poles)>(@"
                    SELECT PilotoId, COUNT(*) AS Poles
                    FROM dbo.HistResultadosCorrida
                    WHERE Temporada = @Year AND Largada = 1
                    GROUP BY PilotoId;", new { Year = scope.Year!.Value });
            var polesByDriver = poleRows.ToDictionary(x => x.PilotoId, x => x.Poles, StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                row.Poles = polesByDriver.TryGetValue(row.DriverId, out var poles) ? poles : 0;
            }
        }

        var q = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        return rows
            .Where(r => allowedIds is null || allowedIds.Contains(r.DriverId))
            .Select(MapDriver)
            .Where(d => q is null
                || d.FullName.Contains(q, StringComparison.OrdinalIgnoreCase)
                || (d.Nationality?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                || (d.Code?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();
    }

    public async Task<DriverProfileDto?> GetDriverProfileAsync(string driverId, SeasonScope scope, CancellationToken ct = default)
    {
        await using var conn = _connectionFactory.CreateConnection();
        var driverRow = await conn.QueryFirstOrDefaultAsync<DriverRowProjection>(@"
            SELECT p.PilotoId AS DriverId,
                   p.Codigo AS Code,
                   p.NumeroPermanente AS PermanentNumber,
                   p.Nome AS FirstName,
                   p.Sobrenome AS LastName,
                   p.DataNascimento AS DateOfBirthUtc,
                   p.Nacionalidade AS Nationality,
                   p.FotoUrl AS PhotoUrl,
                   ISNULL(v.Titulos, 0) AS Championships,
                   ISNULL(v.Corridas, 0) AS Races,
                   ISNULL(v.Vitorias, 0) AS Wins,
                   ISNULL(v.Podios, 0) AS Podiums,
                   ISNULL(v.Poles, 0) AS Poles,
                   ISNULL(v.VoltasMaisRapidas, 0) AS FastestLaps,
                   ISNULL(v.Pontos, 0) AS CareerPoints,
                   v.PrimeiraTemporada AS FirstSeason,
                   v.UltimaTemporada AS LastSeason,
                   ISNULL(v.PercVitorias, 0) AS WinPercentage
            FROM dbo.Pilotos p
            LEFT JOIN dbo.vw_CarreiraPilotos v ON v.PilotoId = p.PilotoId
            WHERE p.PilotoId = @DriverId", new { DriverId = driverId });

        if (driverRow is null)
        {
            return null;
        }

        var driver = MapDriver(driverRow);

        var statsRow = scope.IsCurrent
            ? await conn.QueryFirstOrDefaultAsync<VwEstatisticasPilotosRow>(@"
                SELECT @DriverId AS PilotoId,
                       RTRIM(ISNULL(p.Nome,'') + ' ' + ISNULL(p.Sobrenome,'')) AS Piloto,
                       p.Codigo,
                       cp.Posicao AS PosicaoCampeonato,
                       cp.Pontos,
                       COUNT(*) AS Corridas,
                       SUM(CASE WHEN r.Posicao = 1 THEN 1 ELSE 0 END) AS Vitorias,
                       SUM(CASE WHEN r.Posicao <= 3 THEN 1 ELSE 0 END) AS Podios,
                       SUM(CASE WHEN r.Pontos > 0 THEN 1 ELSE 0 END) AS NosPontos,
                       SUM(CASE WHEN r.PosicaoTexto IN ('R','D','E','W','N') THEN 1 ELSE 0 END) AS Abandonos,
                       SUM(CASE WHEN r.Largada = 1 THEN 1 ELSE 0 END) AS Poles,
                       SUM(CASE WHEN r.VoltaRapidaRank = 1 THEN 1 ELSE 0 END) AS VoltasMaisRapidas,
                       0 AS VitoriasSprint,
                       CAST(0 AS DECIMAL(5,1)) AS PontosSprint,
                       AVG(CAST(NULLIF(r.Largada,0) AS DECIMAL(5,2))) AS ClassifMedia,
                       AVG(CAST(NULLIF(r.Largada,0) AS DECIMAL(5,2))) AS LargadaMedia,
                       AVG(CAST(NULLIF(r.Posicao,0) AS DECIMAL(5,2))) AS ChegadaMedia,
                       SUM(CASE WHEN r.Largada > 0 AND r.Posicao > 0 THEN r.Largada - r.Posicao ELSE 0 END) AS PosicoesGanhas,
                       SUM(ISNULL(r.Voltas,0)) AS VoltasCompletadas
                FROM dbo.ResultadosCorrida r
                JOIN dbo.Pilotos p ON p.PilotoId = r.PilotoId
                LEFT JOIN dbo.ClassificacaoPilotos cp ON cp.PilotoId = r.PilotoId
                WHERE r.PilotoId = @DriverId
                GROUP BY r.PilotoId, p.Nome, p.Sobrenome, p.Codigo, cp.Posicao, cp.Pontos;",
                new { DriverId = driverId })
            : scope.IsHistorical
                ? await conn.QueryFirstOrDefaultAsync<VwEstatisticasPilotosRow>(@"
                    SELECT @DriverId AS PilotoId,
                           RTRIM(ISNULL(p.Nome,'') + ' ' + ISNULL(p.Sobrenome,'')) AS Piloto,
                           p.Codigo, cp.Posicao AS PosicaoCampeonato, cp.Pontos,
                           COUNT(*) AS Corridas,
                           SUM(CASE WHEN r.Posicao = 1 THEN 1 ELSE 0 END) AS Vitorias,
                           SUM(CASE WHEN r.Posicao <= 3 THEN 1 ELSE 0 END) AS Podios,
                           SUM(CASE WHEN r.Pontos > 0 THEN 1 ELSE 0 END) AS NosPontos,
                           SUM(CASE WHEN r.PosicaoTexto IN ('R','D','E','W','N') THEN 1 ELSE 0 END) AS Abandonos,
                           SUM(CASE WHEN r.Largada = 1 THEN 1 ELSE 0 END) AS Poles,
                           SUM(CASE WHEN r.VoltaRapidaRank = 1 THEN 1 ELSE 0 END) AS VoltasMaisRapidas,
                           0 AS VitoriasSprint, CAST(0 AS DECIMAL(5,1)) AS PontosSprint,
                           AVG(CAST(NULLIF(r.Largada,0) AS DECIMAL(5,2))) AS ClassifMedia,
                           AVG(CAST(NULLIF(r.Largada,0) AS DECIMAL(5,2))) AS LargadaMedia,
                           AVG(CAST(NULLIF(r.Posicao,0) AS DECIMAL(5,2))) AS ChegadaMedia,
                           SUM(CASE WHEN r.Largada > 0 AND r.Posicao > 0 THEN r.Largada - r.Posicao ELSE 0 END) AS PosicoesGanhas,
                           SUM(ISNULL(r.Voltas,0)) AS VoltasCompletadas
                    FROM dbo.HistResultadosCorrida r
                    JOIN dbo.Pilotos p ON p.PilotoId = r.PilotoId
                    LEFT JOIN dbo.HistClassificacaoPilotos cp ON cp.Temporada = @Year AND cp.PilotoId = r.PilotoId
                    WHERE r.PilotoId = @DriverId AND r.Temporada = @Year
                    GROUP BY r.PilotoId, p.Nome, p.Sobrenome, p.Codigo, cp.Posicao, cp.Pontos;",
                    new { DriverId = driverId, Year = scope.Year!.Value })
                : await conn.QueryFirstOrDefaultAsync<VwEstatisticasPilotosRow>(
                    "SELECT * FROM dbo.vw_EstatisticasPilotos WHERE PilotoId = @DriverId",
                    new { DriverId = driverId });

        DriverSeasonStatsDto? currentStats = statsRow?.Corridas is not null
            ? new DriverSeasonStatsDto(
                statsRow.PilotoId,
                statsRow.Piloto ?? driver.FullName,
                statsRow.Codigo,
                statsRow.PosicaoCampeonato,
                statsRow.Pontos,
                statsRow.Corridas,
                statsRow.Vitorias,
                statsRow.Podios,
                statsRow.NosPontos,
                statsRow.Abandonos,
                statsRow.Poles,
                statsRow.VoltasMaisRapidas,
                statsRow.VitoriasSprint,
                statsRow.PontosSprint,
                statsRow.ClassifMedia,
                statsRow.LargadaMedia,
                statsRow.ChegadaMedia,
                statsRow.PosicoesGanhas,
                statsRow.VoltasCompletadas)
            : null;

        var seasonHistory = (await conn.QueryAsync<DriverSeasonHistoryItemDto>(@"
            WITH res AS (
                SELECT Temporada,
                       MAX(EquipeId) AS EquipeId,
                       COUNT(DISTINCT Rodada) AS Races,
                       SUM(CASE WHEN Posicao = 1 THEN 1 ELSE 0 END) AS Wins,
                       SUM(CASE WHEN Posicao <= 3 THEN 1 ELSE 0 END) AS Podiums,
                       SUM(CASE WHEN Largada = 1 THEN 1 ELSE 0 END) AS Poles,
                       SUM(ISNULL(Pontos, 0)) AS Points
                FROM dbo.vw_ResultadosTodos
                WHERE PilotoId = @DriverId
                GROUP BY Temporada
            )
            SELECT res.Temporada AS Season,
                   e.Nome AS TeamName,
                   ISNULL(hcp.Posicao, CASE WHEN res.Temporada = 2026 THEN ISNULL(cp.Posicao, 0) ELSE 0 END) AS ChampionshipPosition, -- 0 = sem posição (convertido para null abaixo)
                   ISNULL(hcp.Pontos, res.Points) AS Points,
                   res.Wins,
                   res.Podiums,
                   res.Poles,
                   res.Races
            FROM res
            LEFT JOIN dbo.Equipes e ON e.EquipeId = res.EquipeId
            LEFT JOIN dbo.HistClassificacaoPilotos hcp ON hcp.Temporada = res.Temporada AND hcp.PilotoId = @DriverId
            LEFT JOIN dbo.ClassificacaoPilotos cp ON res.Temporada = 2026 AND cp.PilotoId = @DriverId
            ORDER BY res.Temporada DESC;", new { DriverId = driverId }))
            .Select(h => h.ChampionshipPosition == 0 ? h with { ChampionshipPosition = null } : h)
            .ToList();

        var hallOfFame = await GetHallOfFameAsync(ct);
        var bio = hallOfFame.FirstOrDefault(b => string.Equals(b.DriverId, driverId, StringComparison.OrdinalIgnoreCase));

        return new DriverProfileDto(driver, currentStats, seasonHistory, bio);
    }

    public async Task<IReadOnlyList<ConstructorTeam>> GetTeamsAsync(SeasonScope scope, string? search = null, CancellationToken ct = default)
    {
        await using var conn = _connectionFactory.CreateConnection();

        HashSet<string>? allowedIds = null;
        if (scope.IsCurrent)
        {
            var ids = await conn.QueryAsync<string>(@"
                SELECT DISTINCT EquipeId FROM dbo.ClassificacaoEquipes WHERE EquipeId IS NOT NULL
                UNION
                SELECT DISTINCT EquipeId FROM dbo.ResultadosCorrida WHERE EquipeId IS NOT NULL");
            allowedIds = new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);
        }
        else if (scope.IsHistorical && scope.Year.HasValue)
        {
            var ids = await conn.QueryAsync<string>(@"
                SELECT DISTINCT EquipeId FROM dbo.HistClassificacaoEquipes WHERE Temporada = @Year AND EquipeId IS NOT NULL
                UNION
                SELECT DISTINCT EquipeId FROM dbo.HistResultadosCorrida WHERE Temporada = @Year AND EquipeId IS NOT NULL",
                new { Year = scope.Year.Value });
            allowedIds = new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);
        }

        var ptsMap = (await conn.QueryAsync<TeamPtsRow>(@"
            SELECT EquipeId, SUM(ISNULL(Pontos, 0)) AS TotalPts
            FROM dbo.vw_ResultadosTodos
            WHERE EquipeId IS NOT NULL
            GROUP BY EquipeId;"))
            .ToDictionary(x => x.EquipeId, x => x.TotalPts, StringComparer.OrdinalIgnoreCase);

        var rows = (await conn.QueryAsync<ConstructorTeam>(@"
            SELECT e.EquipeId AS TeamId,
                   e.Nome AS Name,
                   e.Nacionalidade AS Nationality,
                   e.Cor AS ColorHex,
                   ISNULL(v.TitulosConstrutores, 0) AS ConstructorTitles,
                   ISNULL(v.Corridas, 0) AS Races,
                   ISNULL(v.Vitorias, 0) AS Wins,
                   ISNULL(v.Podios, 0) AS Podiums,
                   ISNULL(v.Poles, 0) AS Poles,
                   CAST(0 AS DECIMAL(18,2)) AS TotalPoints,
                   v.PrimeiraTemporada AS FirstSeason,
                   v.UltimaTemporada AS LastSeason
            FROM dbo.Equipes e
            LEFT JOIN dbo.vw_CarreiraEquipes v ON v.EquipeId = e.EquipeId
            ORDER BY ISNULL(v.TitulosConstrutores, 0) DESC, ISNULL(v.Vitorias, 0) DESC, ISNULL(v.Podios, 0) DESC;"))
            .Select(t => new ConstructorTeam
            {
                TeamId = t.TeamId,
                Name = t.Name,
                Nationality = t.Nationality,
                ColorHex = t.ColorHex,
                ConstructorTitles = t.ConstructorTitles,
                Races = t.Races,
                Wins = t.Wins,
                Podiums = t.Podiums,
                Poles = t.Poles,
                TotalPoints = ptsMap.TryGetValue(t.TeamId, out var tp) ? tp : 0m,
                FirstSeason = t.FirstSeason,
                LastSeason = t.LastSeason
            })
            .ToList();

        var q = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        return rows
            .Where(t => allowedIds is null || allowedIds.Contains(t.TeamId))
            .Where(t => q is null
                || t.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                || (t.Nationality?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();
    }

    public async Task<TeamProfileDto?> GetTeamProfileAsync(string teamId, SeasonScope scope, CancellationToken ct = default)
    {
        await using var conn = _connectionFactory.CreateConnection();
        var totalPts = await conn.ExecuteScalarAsync<decimal?>(@"
            SELECT SUM(ISNULL(Pontos, 0))
            FROM dbo.vw_ResultadosTodos
            WHERE EquipeId = @TeamId;", new { TeamId = teamId }) ?? 0m;

        var teamRow = await conn.QueryFirstOrDefaultAsync<ConstructorTeam>(@"
            SELECT e.EquipeId AS TeamId,
                   e.Nome AS Name,
                   e.Nacionalidade AS Nationality,
                   e.Cor AS ColorHex,
                   ISNULL(v.TitulosConstrutores, 0) AS ConstructorTitles,
                   ISNULL(v.Corridas, 0) AS Races,
                   ISNULL(v.Vitorias, 0) AS Wins,
                   ISNULL(v.Podios, 0) AS Podiums,
                   ISNULL(v.Poles, 0) AS Poles,
                   CAST(0 AS DECIMAL(18,2)) AS TotalPoints,
                   v.PrimeiraTemporada AS FirstSeason,
                   v.UltimaTemporada AS LastSeason
            FROM dbo.Equipes e
            LEFT JOIN dbo.vw_CarreiraEquipes v ON v.EquipeId = e.EquipeId
            WHERE e.EquipeId = @TeamId", new { TeamId = teamId });

        if (teamRow is null)
        {
            return null;
        }

        var team = new ConstructorTeam
        {
            TeamId = teamRow.TeamId,
            Name = teamRow.Name,
            Nationality = teamRow.Nationality,
            ColorHex = teamRow.ColorHex,
            ConstructorTitles = teamRow.ConstructorTitles,
            Races = teamRow.Races,
            Wins = teamRow.Wins,
            Podiums = teamRow.Podiums,
            Poles = teamRow.Poles,
            TotalPoints = totalPts,
            FirstSeason = teamRow.FirstSeason,
            LastSeason = teamRow.LastSeason
        };

        var seasonHistory = (await conn.QueryAsync<TeamSeasonHistoryItemDto>(@"
            WITH res AS (
                SELECT Temporada,
                       COUNT(DISTINCT Rodada) AS Races,
                       SUM(CASE WHEN Posicao = 1 THEN 1 ELSE 0 END) AS Wins,
                       SUM(CASE WHEN Posicao <= 3 THEN 1 ELSE 0 END) AS Podiums,
                       SUM(CASE WHEN Largada = 1 THEN 1 ELSE 0 END) AS Poles,
                       SUM(ISNULL(Pontos, 0)) AS Points
                FROM dbo.vw_ResultadosTodos
                WHERE EquipeId = @TeamId
                GROUP BY Temporada
            )
            SELECT res.Temporada AS Season,
                   ISNULL(hce.Posicao, CASE WHEN res.Temporada = 2026 THEN ISNULL(ce.Posicao, 0) ELSE 0 END) AS ChampionshipPosition, -- 0 = sem posição (convertido para null abaixo)
                   ISNULL(hce.Pontos, res.Points) AS Points,
                   res.Wins,
                   res.Podiums,
                   res.Poles,
                   res.Races
            FROM res
            LEFT JOIN dbo.HistClassificacaoEquipes hce ON hce.Temporada = res.Temporada AND hce.EquipeId = @TeamId
            LEFT JOIN dbo.ClassificacaoEquipes ce ON res.Temporada = 2026 AND ce.EquipeId = @TeamId
            ORDER BY res.Temporada DESC;", new { TeamId = teamId }))
            .Select(h => h.ChampionshipPosition == 0 ? h with { ChampionshipPosition = null } : h)
            .ToList();

        var teamDriverIds = (await conn.QueryAsync<string>(@"
            SELECT DISTINCT PilotoId
            FROM dbo.vw_ResultadosTodos
            WHERE EquipeId = @TeamId AND PilotoId IS NOT NULL;", new { TeamId = teamId }))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var allDrivers = await GetDriversAsync(SeasonScope.AllTime(), null, ct);
        var teamDrivers = allDrivers
            .Where(d => teamDriverIds.Contains(d.DriverId))
            .OrderByDescending(d => d.Wins)
            .ThenByDescending(d => d.Podiums)
            .ToList();

        return new TeamProfileDto(team, seasonHistory, teamDrivers);
    }

    public async Task<IReadOnlyList<DriverStandingDto>> GetDriverStandingsAsync(SeasonScope scope, CancellationToken ct = default)
    {
        await using var conn = _connectionFactory.CreateConnection();
        if (scope.IsCurrent)
        {
            var rows = await conn.QueryAsync<DriverStandingDto>(@"
                SELECT 2026 AS Season,
                       ISNULL(c.Posicao, 999) AS Position,
                       c.PilotoId AS DriverId,
                       RTRIM(ISNULL(p.Nome,'') + ' ' + ISNULL(p.Sobrenome,'')) AS DriverName,
                       p.Codigo AS Code,
                       p.Nacionalidade AS Nationality,
                       c.EquipeId AS TeamId,
                       e.Nome AS TeamName,
                       e.Cor AS TeamColor,
                       ISNULL(c.Pontos, 0) AS Points,
                       ISNULL(c.Vitorias, 0) AS Wins,
                       c.AposRodada AS AfterRound,
                       p.FotoUrl AS PhotoUrl,
                       (SELECT COUNT(*)
                        FROM dbo.ResultadosCorrida pole
                        WHERE pole.PilotoId = c.PilotoId AND pole.Largada = 1) AS Poles
                FROM dbo.ClassificacaoPilotos c
                JOIN dbo.Pilotos p ON p.PilotoId = c.PilotoId
                LEFT JOIN dbo.Equipes e ON e.EquipeId = c.EquipeId
                ORDER BY ISNULL(c.Posicao, 999), c.Pontos DESC;");
            return rows.ToList();
        }

        if (scope.IsHistorical)
        {
            var rows = await conn.QueryAsync<DriverStandingDto>(@"
                SELECT c.Temporada AS Season,
                       ISNULL(c.Posicao, 999) AS Position,
                       c.PilotoId AS DriverId,
                       RTRIM(ISNULL(p.Nome,'') + ' ' + ISNULL(p.Sobrenome,'')) AS DriverName,
                       p.Codigo AS Code,
                       p.Nacionalidade AS Nationality,
                       c.EquipeId AS TeamId,
                       e.Nome AS TeamName,
                       e.Cor AS TeamColor,
                       ISNULL(c.Pontos, 0) AS Points,
                       ISNULL(c.Vitorias, 0) AS Wins,
                       CAST(NULL AS INT) AS AfterRound,
                       p.FotoUrl AS PhotoUrl,
                       (SELECT COUNT(*)
                        FROM dbo.HistResultadosCorrida pole
                        WHERE pole.Temporada = @Year
                          AND pole.PilotoId = c.PilotoId
                          AND pole.Largada = 1) AS Poles
                FROM dbo.HistClassificacaoPilotos c
                JOIN dbo.Pilotos p ON p.PilotoId = c.PilotoId
                LEFT JOIN dbo.Equipes e ON e.EquipeId = c.EquipeId
                WHERE c.Temporada = @Year
                ORDER BY ISNULL(c.Posicao, 999), c.Pontos DESC;", new { Year = scope.Year!.Value });
            return rows.ToList();
        }

        // AllTime ranking based on vw_CarreiraPilotos + latest team
        var driverTeams = (await conn.QueryAsync<(string PilotoId, string EquipeId, string EquipeNome, string? EquipeCor)>(@"
            WITH ranked AS (
                SELECT r.PilotoId,
                       r.EquipeId,
                       ROW_NUMBER() OVER (PARTITION BY r.PilotoId ORDER BY r.Temporada DESC, r.Rodada DESC) AS rn
                FROM dbo.vw_ResultadosTodos r
                WHERE r.PilotoId IS NOT NULL AND r.EquipeId IS NOT NULL
            )
            SELECT rk.PilotoId, rk.EquipeId, e.Nome AS EquipeNome, e.Cor AS EquipeCor
            FROM ranked rk
            JOIN dbo.Equipes e ON e.EquipeId = rk.EquipeId
            WHERE rk.rn = 1;"))
            .ToDictionary(x => x.PilotoId, x => x, StringComparer.OrdinalIgnoreCase);

        var allTimeDrivers = await GetDriversAsync(scope, null, ct);
        return allTimeDrivers
            .Select((d, idx) =>
            {
                driverTeams.TryGetValue(d.DriverId, out var dt);
                return new DriverStandingDto(
                    0,
                    idx + 1,
                    d.DriverId,
                    d.FullName,
                    d.Code,
                    d.Nationality,
                    dt.EquipeId,
                    dt.EquipeNome ?? $"{d.Championships}x Campeão ({d.FirstSeason}-{d.LastSeason})",
                    dt.EquipeCor,
                    d.CareerPoints,
                    d.Wins,
                    d.Races,
                    d.PhotoUrl,
                    d.Poles);
            })
            .ToList();
    }

    public async Task<IReadOnlyList<TeamStandingDto>> GetTeamStandingsAsync(SeasonScope scope, CancellationToken ct = default)
    {
        await using var conn = _connectionFactory.CreateConnection();
        if (scope.IsCurrent)
        {
            var rows = await conn.QueryAsync<TeamStandingDto>(@"
                SELECT 2026 AS Season,
                       ISNULL(c.Posicao, 999) AS Position,
                       c.EquipeId AS TeamId,
                       e.Nome AS TeamName,
                       e.Nacionalidade AS Nationality,
                       e.Cor AS ColorHex,
                       ISNULL(c.Pontos, 0) AS Points,
                       ISNULL(c.Vitorias, 0) AS Wins,
                       c.AposRodada AS AfterRound
                FROM dbo.ClassificacaoEquipes c
                JOIN dbo.Equipes e ON e.EquipeId = c.EquipeId
                ORDER BY ISNULL(c.Posicao, 999), c.Pontos DESC;");
            return rows.ToList();
        }

        if (scope.IsHistorical)
        {
            var rows = await conn.QueryAsync<TeamStandingDto>(@"
                SELECT c.Temporada AS Season,
                       ISNULL(c.Posicao, 999) AS Position,
                       c.EquipeId AS TeamId,
                       e.Nome AS TeamName,
                       e.Nacionalidade AS Nationality,
                       e.Cor AS ColorHex,
                       ISNULL(c.Pontos, 0) AS Points,
                       ISNULL(c.Vitorias, 0) AS Wins,
                       CAST(NULL AS INT) AS AfterRound
                FROM dbo.HistClassificacaoEquipes c
                JOIN dbo.Equipes e ON e.EquipeId = c.EquipeId
                WHERE c.Temporada = @Year
                ORDER BY ISNULL(c.Posicao, 999), c.Pontos DESC;", new { Year = scope.Year!.Value });
            return rows.ToList();
        }

        var allTimeTeams = await GetTeamsAsync(scope, null, ct);
        return allTimeTeams
            .Select((t, idx) => new TeamStandingDto(
                0,
                idx + 1,
                t.TeamId,
                t.Name,
                t.Nationality,
                t.ColorHex,
                t.TotalPoints,
                t.Wins,
                t.Races))
            .ToList();
    }

    public async Task<IReadOnlyList<GrandPrix>> GetRacesAsync(SeasonScope scope, CancellationToken ct = default)
    {
        await using var conn = _connectionFactory.CreateConnection();
        if (scope.IsCurrent)
        {
            var gpRows = (await conn.QueryAsync<GrandePremioWithCircuitRow>(@"
                SELECT gp.Rodada,
                       gp.Nome,
                       gp.CircuitoId,
                       c.Nome AS CircuitName,
                       c.Pais AS Country,
                       gp.DataCorridaUTC,
                       gp.TemSprint,
                       gp.Status,
                       (SELECT MAX(r.Voltas)
                        FROM dbo.ResultadosCorrida r
                        WHERE r.Rodada = gp.Rodada) AS Laps
                FROM dbo.GrandesPremios gp
                LEFT JOIN dbo.Circuitos c ON c.CircuitoId = gp.CircuitoId
                ORDER BY gp.Rodada ASC;")).ToList();

            var vencedores = (await conn.QueryAsync<VwVencedoresRow>("SELECT * FROM dbo.vw_Vencedores"))
                .ToDictionary(v => v.Rodada);

            return gpRows.Select(gp =>
            {
                vencedores.TryGetValue(gp.Rodada, out var v);
                return new GrandPrix
                {
                    Season = 2026,
                    Round = gp.Rodada,
                    Name = gp.Nome,
                    CircuitId = gp.CircuitoId,
                    CircuitName = gp.CircuitName,
                    Country = gp.Country,
                    RaceDateUtc = gp.DataCorridaUTC,
                    HasSprint = gp.TemSprint,
                    Status = gp.Status,
                    WinnerName = v?.Vencedor,
                    WinnerTeam = v?.Equipe,
                    PoleName = v?.Pole,
                    FastestLapName = v?.VoltaMaisRapida,
                    Laps = gp.Laps
                };
            }).ToList();
        }

        if (scope.IsHistorical)
        {
            var rows = await conn.QueryAsync<GrandPrix>(@"
                WITH win AS (
                    SELECT Rodada, MIN(PilotoId) AS WinnerId, MIN(EquipeId) AS TeamId
                    FROM dbo.HistResultadosCorrida
                    WHERE Temporada = @Year AND Posicao = 1
                    GROUP BY Rodada
                ),
                pole AS (
                    SELECT Rodada, MIN(PilotoId) AS PoleId
                    FROM dbo.HistResultadosCorrida
                    WHERE Temporada = @Year AND Largada = 1
                    GROUP BY Rodada
                ),
                fl AS (
                    SELECT Rodada, MIN(PilotoId) AS FastId
                    FROM dbo.HistResultadosCorrida
                    WHERE Temporada = @Year AND VoltaRapidaRank = 1
                    GROUP BY Rodada
                )
                SELECT gp.Temporada AS Season,
                       gp.Rodada AS Round,
                       gp.Nome AS Name,
                       gp.CircuitoId AS CircuitId,
                       c.Nome AS CircuitName,
                       c.Pais AS Country,
                       gp.DataCorridaUTC AS RaceDateUtc,
                       gp.TemSprint AS HasSprint,
                       'CONCLUIDO' AS Status,
                       RTRIM(pw.Nome + ' ' + pw.Sobrenome) AS WinnerName,
                       ew.Nome AS WinnerTeam,
                       RTRIM(pp.Nome + ' ' + pp.Sobrenome) AS PoleName,
                       RTRIM(pf.Nome + ' ' + pf.Sobrenome) AS FastestLapName,
                       (SELECT MAX(r.Voltas)
                        FROM dbo.HistResultadosCorrida r
                        WHERE r.Temporada = gp.Temporada AND r.Rodada = gp.Rodada) AS Laps
                FROM dbo.HistGrandesPremios gp
                LEFT JOIN dbo.Circuitos c ON c.CircuitoId = gp.CircuitoId
                LEFT JOIN win w ON w.Rodada = gp.Rodada
                LEFT JOIN dbo.Pilotos pw ON pw.PilotoId = w.WinnerId
                LEFT JOIN dbo.Equipes ew ON ew.EquipeId = w.TeamId
                LEFT JOIN pole pl ON pl.Rodada = gp.Rodada
                LEFT JOIN dbo.Pilotos pp ON pp.PilotoId = pl.PoleId
                LEFT JOIN fl f ON f.Rodada = gp.Rodada
                LEFT JOIN dbo.Pilotos pf ON pf.PilotoId = f.FastId
                WHERE gp.Temporada = @Year
                ORDER BY gp.Rodada ASC;", new { Year = scope.Year!.Value });
            return rows.ToList();
        }

        var allRaces = await conn.QueryAsync<GrandPrix>(@"
            SELECT TOP 150 gp.Temporada AS Season,
                   gp.Rodada AS Round,
                   gp.Nome AS Name,
                   gp.CircuitoId AS CircuitId,
                   c.Nome AS CircuitName,
                   c.Pais AS Country,
                   gp.DataCorridaUTC AS RaceDateUtc,
                   CAST(0 AS BIT) AS HasSprint,
                   'HISTORICO' AS Status,
                   RTRIM(p.Nome + ' ' + p.Sobrenome) AS WinnerName,
                   e.Nome AS WinnerTeam,
                   CAST(NULL AS NVARCHAR(100)) AS PoleName,
                   CAST(NULL AS NVARCHAR(100)) AS FastestLapName,
                   (SELECT MAX(x.Voltas)
                    FROM dbo.vw_ResultadosTodos x
                    WHERE x.Temporada = gp.Temporada AND x.Rodada = gp.Rodada) AS Laps
            FROM dbo.vw_GrandesPremiosTodos gp
            LEFT JOIN dbo.Circuitos c ON c.CircuitoId = gp.CircuitoId
            LEFT JOIN dbo.vw_ResultadosTodos r ON r.Temporada = gp.Temporada AND r.Rodada = gp.Rodada AND r.Posicao = 1
            LEFT JOIN dbo.Pilotos p ON p.PilotoId = r.PilotoId
            LEFT JOIN dbo.Equipes e ON e.EquipeId = r.EquipeId
            WHERE r.PilotoId IS NOT NULL -- só provas já disputadas: evita Laps NULL na 1ª linha (Dapper + SQLite inferem o tipo da coluna pela 1ª linha)
            ORDER BY gp.Temporada DESC, gp.Rodada DESC;");
        return allRaces.ToList();
    }

    public async Task<GrandPrixDetailDto?> GetGrandPrixDetailAsync(int season, int round, bool includeTelemetry, CancellationToken ct = default)
    {
        var scope = SeasonScope.ForYear(season);
        var races = await GetRacesAsync(scope, ct);
        var race = races.FirstOrDefault(r => r.Round == round);
        if (race is null)
        {
            return null;
        }

        await using var conn = _connectionFactory.CreateConnection();
        IReadOnlyList<RaceResultItemDto> raceResults;
        IReadOnlyList<RaceResultItemDto> sprintResults;
        IReadOnlyList<QualifyingResultItemDto> qualifyingResults;

        if (scope.IsCurrent)
        {
            raceResults = (await conn.QueryAsync<RaceResultItemDto>(@"
                SELECT 2026 AS Season, r.Rodada AS Round, r.PilotoId AS DriverId,
                       RTRIM(p.Nome + ' ' + p.Sobrenome) AS DriverName, p.Codigo AS DriverCode,
                       r.EquipeId AS TeamId, e.Nome AS TeamName, e.Cor AS TeamColor,
                       r.Numero AS CarNumber, r.Largada AS GridPosition, r.Posicao AS Position,
                       r.PosicaoTexto AS PositionText, ISNULL(r.Pontos, 0) AS Points, r.Voltas AS Laps,
                       r.Status AS Status, r.TempoTexto AS TimeText, r.VoltaRapidaRank AS FastestLapRank,
                       r.VoltaRapidaNum AS FastestLapNumber, r.VoltaRapidaMs AS FastestLapMs
                FROM dbo.ResultadosCorrida r
                JOIN dbo.Pilotos p ON p.PilotoId = r.PilotoId
                LEFT JOIN dbo.Equipes e ON e.EquipeId = r.EquipeId
                WHERE r.Rodada = @Round
                ORDER BY ISNULL(r.Posicao, 999), r.Largada;", new { Round = round })).ToList();

            sprintResults = (await conn.QueryAsync<RaceResultItemDto>(@"
                SELECT 2026 AS Season, r.Rodada AS Round, r.PilotoId AS DriverId,
                       RTRIM(p.Nome + ' ' + p.Sobrenome) AS DriverName, p.Codigo AS DriverCode,
                       r.EquipeId AS TeamId, e.Nome AS TeamName, e.Cor AS TeamColor,
                       r.Numero AS CarNumber, r.Largada AS GridPosition, r.Posicao AS Position,
                       r.PosicaoTexto AS PositionText, ISNULL(r.Pontos, 0) AS Points, r.Voltas AS Laps,
                       r.Status AS Status, r.TempoTexto AS TimeText, r.VoltaRapidaRank AS FastestLapRank,
                       r.VoltaRapidaNum AS FastestLapNumber, r.VoltaRapidaMs AS FastestLapMs
                FROM dbo.ResultadosSprint r
                JOIN dbo.Pilotos p ON p.PilotoId = r.PilotoId
                LEFT JOIN dbo.Equipes e ON e.EquipeId = r.EquipeId
                WHERE r.Rodada = @Round
                ORDER BY ISNULL(r.Posicao, 999), r.Largada;", new { Round = round })).ToList();

            qualifyingResults = (await conn.QueryAsync<QualifyingResultItemDto>(@"
                SELECT 2026 AS Season, q.Rodada AS Round, q.PilotoId AS DriverId,
                       RTRIM(p.Nome + ' ' + p.Sobrenome) AS DriverName, p.Codigo AS DriverCode,
                       q.EquipeId AS TeamId, e.Nome AS TeamName, q.Numero AS CarNumber,
                       q.Posicao AS Position, q.Q1Ms, q.Q2Ms, q.Q3Ms
                FROM dbo.Classificatorio q
                JOIN dbo.Pilotos p ON p.PilotoId = q.PilotoId
                LEFT JOIN dbo.Equipes e ON e.EquipeId = q.EquipeId
                WHERE q.Rodada = @Round
                ORDER BY ISNULL(q.Posicao, 999);", new { Round = round })).ToList();
        }
        else
        {
            raceResults = (await conn.QueryAsync<RaceResultItemDto>(@"
                SELECT r.Temporada AS Season, r.Rodada AS Round, r.PilotoId AS DriverId,
                       RTRIM(p.Nome + ' ' + p.Sobrenome) AS DriverName, p.Codigo AS DriverCode,
                       r.EquipeId AS TeamId, e.Nome AS TeamName, e.Cor AS TeamColor,
                       r.Numero AS CarNumber, r.Largada AS GridPosition, r.Posicao AS Position,
                       r.PosicaoTexto AS PositionText, ISNULL(r.Pontos, 0) AS Points, r.Voltas AS Laps,
                       r.Status AS Status, r.TempoTexto AS TimeText, r.VoltaRapidaRank AS FastestLapRank,
                       r.VoltaRapidaNum AS FastestLapNumber, r.VoltaRapidaMs AS FastestLapMs
                FROM dbo.HistResultadosCorrida r
                JOIN dbo.Pilotos p ON p.PilotoId = r.PilotoId
                LEFT JOIN dbo.Equipes e ON e.EquipeId = r.EquipeId
                WHERE r.Temporada = @Season AND r.Rodada = @Round
                ORDER BY ISNULL(r.Posicao, 999), r.Largada;", new { Season = season, Round = round })).ToList();

            sprintResults = (await conn.QueryAsync<RaceResultItemDto>(@"
                SELECT r.Temporada AS Season, r.Rodada AS Round, r.PilotoId AS DriverId,
                       RTRIM(p.Nome + ' ' + p.Sobrenome) AS DriverName, p.Codigo AS DriverCode,
                       r.EquipeId AS TeamId, e.Nome AS TeamName, e.Cor AS TeamColor,
                       r.Numero AS CarNumber, r.Largada AS GridPosition, r.Posicao AS Position,
                       r.PosicaoTexto AS PositionText, ISNULL(r.Pontos, 0) AS Points, r.Voltas AS Laps,
                       r.Status AS Status, r.TempoTexto AS TimeText, r.VoltaRapidaRank AS FastestLapRank,
                       r.VoltaRapidaNum AS FastestLapNumber, r.VoltaRapidaMs AS FastestLapMs
                FROM dbo.HistResultadosSprint r
                JOIN dbo.Pilotos p ON p.PilotoId = r.PilotoId
                LEFT JOIN dbo.Equipes e ON e.EquipeId = r.EquipeId
                WHERE r.Temporada = @Season AND r.Rodada = @Round
                ORDER BY ISNULL(r.Posicao, 999), r.Largada;", new { Season = season, Round = round })).ToList();

            qualifyingResults = (await conn.QueryAsync<QualifyingResultItemDto>(@"
                SELECT q.Temporada AS Season, q.Rodada AS Round, q.PilotoId AS DriverId,
                       RTRIM(p.Nome + ' ' + p.Sobrenome) AS DriverName, p.Codigo AS DriverCode,
                       q.EquipeId AS TeamId, e.Nome AS TeamName, q.Numero AS CarNumber,
                       q.Posicao AS Position, q.Q1Ms, q.Q2Ms, q.Q3Ms
                FROM dbo.HistClassificatorio q
                JOIN dbo.Pilotos p ON p.PilotoId = q.PilotoId
                LEFT JOIN dbo.Equipes e ON e.EquipeId = q.EquipeId
                WHERE q.Temporada = @Season AND q.Rodada = @Round
                ORDER BY ISNULL(q.Posicao, 999);", new { Season = season, Round = round })).ToList();
        }

        RaceTelemetryDto? telemetry = null;
        if (includeTelemetry && scope.HasRichTelemetry)
        {
            telemetry = await GetRaceTelemetryAsync(season, round, ct);
        }

        return new GrandPrixDetailDto(
            race,
            ToCapabilities(scope),
            raceResults,
            sprintResults,
            qualifyingResults,
            telemetry);
    }

    public async Task<RaceTelemetryDto?> GetRaceTelemetryAsync(int season, int round, CancellationToken ct = default)
    {
        var scope = SeasonScope.ForYear(season);
        if (!scope.HasRichTelemetry)
        {
            return null;
        }

        await using var conn = _connectionFactory.CreateConnection();
        var pitStops = (await conn.QueryAsync<PitStopHighlightDto>(@"
            SELECT gp.Nome AS GrandPrix,
                   RTRIM(p.Nome + ' ' + p.Sobrenome) AS DriverName,
                   e.Nome AS TeamName,
                   ps.Volta AS Lap,
                   ps.DuracaoMs / 1000.0 AS DurationSeconds
            FROM dbo.PitStops ps
            JOIN dbo.GrandesPremios gp ON gp.Rodada = ps.Rodada
            JOIN dbo.Pilotos p ON p.PilotoId = ps.PilotoId
            LEFT JOIN dbo.ResultadosCorrida r ON r.Rodada = ps.Rodada AND r.PilotoId = ps.PilotoId
            LEFT JOIN dbo.Equipes e ON e.EquipeId = r.EquipeId
            WHERE ps.Rodada = @Round AND ps.DuracaoMs IS NOT NULL
            ORDER BY ps.DuracaoMs ASC;", new { Round = round })).ToList();

        var stints = (await conn.QueryAsync<VwEstrategiaPneusRow>(
            "SELECT * FROM dbo.vw_EstrategiaPneus WHERE Rodada = @Round ORDER BY Sessao, Piloto, Stint",
            new { Round = round }))
            .Select(s => new TyreStrategyDto(s.Rodada, s.GrandePremio, s.Sessao, s.Piloto, s.Equipe, s.Stint, s.Composto, s.VoltaInicio, s.VoltaFim, s.VoltasNoStint, s.IdadePneuInicio))
            .ToList();

        var weather = (await conn.QueryAsync<VwClimaSessaoRow>(
            "SELECT * FROM dbo.vw_ClimaSessao WHERE Rodada = @Round",
            new { Round = round }))
            .Select(w => new SessionWeatherSummaryDto(w.Rodada, w.GrandePremio, w.Sessao, w.TempArMedia, w.TempPistaMedia, w.TempPistaMax, w.UmidadeMedia, (w.TeveChuva ?? 0) > 0, w.VentoMedio))
            .ToList();

        var raceControl = (await conn.QueryAsync<RaceControlEventDto>(@"
            SELECT TOP 60 cp.Id, cp.DataHoraUTC AS TimestampUtc, cp.Volta AS Lap,
                   cp.Categoria AS Category, cp.Bandeira AS Flag,
                   cp.NumeroCarro AS CarNumber, cp.Mensagem AS Message
            FROM dbo.ControleProva cp
            JOIN dbo.Sessoes s ON s.SessaoKey = cp.SessaoKey
            WHERE s.Rodada = @Round
            ORDER BY cp.DataHoraUTC DESC;", new { Round = round })).ToList();

        var lapSummaries = (await conn.QueryAsync<LapSummaryDto>(@"
            SELECT v.PilotoId AS DriverId,
                   RTRIM(p.Nome + ' ' + p.Sobrenome) AS DriverName,
                   COUNT(*) AS LapsCount,
                   MIN(v.TempoMs) AS BestLapMs,
                   AVG(v.TempoMs) AS AvgLapMs
            FROM dbo.Voltas v
            JOIN dbo.Pilotos p ON p.PilotoId = v.PilotoId
            WHERE v.Rodada = @Round
            GROUP BY v.PilotoId, p.Nome, p.Sobrenome
            ORDER BY MIN(v.TempoMs) ASC;", new { Round = round })).ToList();

        return new RaceTelemetryDto(season, round, pitStops, stints, weather, raceControl, lapSummaries);
    }

    public async Task<IReadOnlyList<Circuit>> GetCircuitsAsync(SeasonScope? scope = null, CancellationToken ct = default)
    {
        await using var conn = _connectionFactory.CreateConnection();
        var rows = (await conn.QueryAsync<Circuit>(@"
            WITH gp_stats AS (
                SELECT CircuitoId,
                       COUNT(*) AS TotalRacesHosted,
                       MIN(Temporada) AS FirstSeasonHosted,
                       MAX(Temporada) AS LastSeasonHosted
                FROM dbo.vw_GrandesPremiosTodos
                WHERE CircuitoId IS NOT NULL
                GROUP BY CircuitoId
            )
            SELECT c.CircuitoId AS CircuitId,
                   c.Nome AS Name,
                   c.Cidade AS City,
                   c.Pais AS Country,
                   c.Latitude,
                   c.Longitude,
                   ISNULL(s.TotalRacesHosted, 0) AS TotalRacesHosted,
                   s.FirstSeasonHosted,
                   s.LastSeasonHosted,
                   CAST(NULL AS NVARCHAR(100)) AS LastWinner
            FROM dbo.Circuitos c
            LEFT JOIN gp_stats s ON s.CircuitoId = c.CircuitoId
            ORDER BY ISNULL(s.TotalRacesHosted, 0) DESC, c.Nome ASC;")).ToList();

        if (scope is not null && !scope.IsAllTime)
        {
            var seasonYear = scope.Year ?? SeasonScope.CurrentFormula1Season;
            var seasonCircuits = (await conn.QueryAsync<CircuitRoundRow>(@"
                SELECT CircuitoId AS CircuitId, MIN(Rodada) AS MinRound
                FROM dbo.vw_GrandesPremiosTodos
                WHERE Temporada = @Year AND CircuitoId IS NOT NULL
                GROUP BY CircuitoId;", new { Year = seasonYear }))
                .ToDictionary(x => x.CircuitId, x => x.MinRound, StringComparer.OrdinalIgnoreCase);

            rows = rows
                .Where(c => seasonCircuits.ContainsKey(c.CircuitId))
                .OrderBy(c => seasonCircuits[c.CircuitId])
                .ToList();
        }

        return rows;
    }

    public async Task<CircuitDetailDto?> GetCircuitDetailAsync(string circuitId, CancellationToken ct = default)
    {
        var circuits = await GetCircuitsAsync(SeasonScope.AllTime(), ct);
        var circuit = circuits.FirstOrDefault(c => string.Equals(c.CircuitId, circuitId, StringComparison.OrdinalIgnoreCase));
        if (circuit is null)
        {
            return null;
        }

        await using var conn = _connectionFactory.CreateConnection();
        var gps = (await conn.QueryAsync<CircuitGpRow>(@"
            SELECT Temporada AS Season, Rodada AS Round, Nome AS GrandPrixName, DataCorridaUTC AS RaceDateUtc
            FROM dbo.vw_GrandesPremiosTodos
            WHERE CircuitoId = @CircuitId
            ORDER BY Temporada DESC, Rodada DESC;", new { CircuitId = circuitId })).ToList();

        var winners = (await conn.QueryAsync<CircuitWinnerRow>(@"
            SELECT r.Temporada, r.Rodada, RTRIM(p.Nome + ' ' + p.Sobrenome) AS WinnerName, e.Nome AS WinnerTeam
            FROM dbo.vw_ResultadosTodos r
            JOIN dbo.Pilotos p ON p.PilotoId = r.PilotoId
            LEFT JOIN dbo.Equipes e ON e.EquipeId = r.EquipeId
            WHERE r.Posicao = 1;"))
            .GroupBy(x => (x.Temporada, x.Rodada))
            .ToDictionary(g => g.Key, g => g.First());

        var poles = (await conn.QueryAsync<CircuitPoleRow>(@"
            SELECT r.Temporada, r.Rodada, RTRIM(p.Nome + ' ' + p.Sobrenome) AS PoleName
            FROM dbo.vw_ResultadosTodos r
            JOIN dbo.Pilotos p ON p.PilotoId = r.PilotoId
            WHERE r.Largada = 1;"))
            .GroupBy(x => (x.Temporada, x.Rodada))
            .ToDictionary(g => g.Key, g => g.First().PoleName);

        var fastestLaps = (await conn.QueryAsync<CircuitFastestLapRow>(@"
            SELECT r.Temporada, r.Rodada, RTRIM(p.Nome + ' ' + p.Sobrenome) AS FastestLapName,
                   e.Nome AS FastestLapTeam, r.VoltaRapidaNum AS FastestLapNumber, r.VoltaRapidaMs AS FastestLapMs
            FROM (
                -- tabelas base: vw_ResultadosTodos não expõe VoltaRapidaNum/VoltaRapidaMs
                SELECT Temporada, Rodada, PilotoId, EquipeId, VoltaRapidaRank, VoltaRapidaNum, VoltaRapidaMs
                FROM dbo.HistResultadosCorrida
                UNION ALL
                SELECT 2026, Rodada, PilotoId, EquipeId, VoltaRapidaRank, VoltaRapidaNum, VoltaRapidaMs
                FROM dbo.ResultadosCorrida
            ) r
            JOIN dbo.Pilotos p ON p.PilotoId = r.PilotoId
            LEFT JOIN dbo.Equipes e ON e.EquipeId = r.EquipeId
            WHERE r.VoltaRapidaRank = 1 AND r.VoltaRapidaMs > 0
            ORDER BY r.Temporada, r.Rodada, r.VoltaRapidaMs;"))
            .GroupBy(x => (x.Temporada, x.Rodada))
            .ToDictionary(g => g.Key, g => g.First());

        var history = gps.Select(g =>
        {
            winners.TryGetValue((g.Season, g.Round), out var w);
            poles.TryGetValue((g.Season, g.Round), out var pName);
            fastestLaps.TryGetValue((g.Season, g.Round), out var fastestLap);
            return new CircuitRaceHistoryItemDto(
                g.Season,
                g.Round,
                g.GrandPrixName,
                g.RaceDateUtc,
                w?.WinnerName,
                w?.WinnerTeam,
                pName,
                fastestLap?.FastestLapName,
                fastestLap?.FastestLapTeam,
                fastestLap?.FastestLapNumber,
                fastestLap?.FastestLapMs);
        }).ToList();

        return new CircuitDetailDto(circuit, history);
    }

    public async Task<IReadOnlyList<SeasonSummary>> GetTimelineAsync(CancellationToken ct = default)
    {
        await using var conn = _connectionFactory.CreateConnection();
        var rows = await conn.QueryAsync<SeasonSummary>(@"
            SELECT t.Temporada AS Season,
                   t.Corridas AS RacesCount,
                   v.CampeaoPilotos AS DriverChampion,
                   cp.PilotoId AS DriverChampionId,
                   v.Nacionalidade AS DriverNationality,
                   v.EquipeDoCampeao AS DriverTeam,
                   v.PontosCampeao AS DriverPoints,
                   v.VitoriasCampeao AS DriverWins,
                   v.CampeaConstrutores AS ConstructorChampion,
                   v.PontosConstrutores AS ConstructorPoints,
                   CAST(CASE WHEN v.TemBiografia = 1 THEN 1 ELSE 0 END AS BIT) AS HasBiography
            FROM dbo.vw_Campeoes v
            JOIN dbo.HistTemporadas t ON t.Temporada = v.Temporada
            LEFT JOIN dbo.HistClassificacaoPilotos cp ON cp.Temporada = v.Temporada AND cp.Posicao = 1
            ORDER BY v.Temporada DESC;");
        return rows.ToList();
    }

    public async Task<IReadOnlyList<ChampionBio>> GetHallOfFameAsync(CancellationToken ct = default)
    {
        await using var conn = _connectionFactory.CreateConnection();
        var bioRows = (await conn.QueryAsync<CampeaoBiografiaRow>("SELECT * FROM dbo.CampeoesBiografia ORDER BY CampeaoId")).ToList();
        var photoRows = (await conn.QueryAsync<FotoHistoricaRow>("SELECT * FROM dbo.FotosHistoricas ORDER BY CampeaoId, Ordem")).ToList();
        var careerChampions = (await conn.QueryAsync<VwCarreiraPilotosRow>("SELECT * FROM dbo.vw_CarreiraPilotos WHERE Titulos > 0 ORDER BY Titulos DESC, Vitorias DESC")).ToList();
        var pilotos = (await conn.QueryAsync<PilotoRow>("SELECT * FROM dbo.Pilotos")).ToDictionary(p => p.PilotoId, StringComparer.OrdinalIgnoreCase);

        var titleYearsByDriver = (await conn.QueryAsync<DriverTitleYearRow>(
            "SELECT PilotoId, Temporada FROM dbo.HistClassificacaoPilotos WHERE Posicao = 1 ORDER BY Temporada"))
            .GroupBy(x => x.PilotoId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(x => x.Temporada)), StringComparer.OrdinalIgnoreCase);

        var photosByChampion = photoRows
            .GroupBy(x => x.CampeaoId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<HistoricalPhoto>)g.Select(p => new HistoricalPhoto
            {
                ChampionId = p.CampeaoId,
                Order = p.Ordem,
                Location = p.Local,
                Caption = p.Legenda
            }).ToList());

        var result = new List<ChampionBio>();
        var coveredDriverIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var b in bioRows)
        {
            VwCarreiraPilotosRow? career = null;
            PilotoRow? piloto = null;
            if (!string.IsNullOrWhiteSpace(b.PilotoId))
            {
                coveredDriverIds.Add(b.PilotoId);
                career = careerChampions.FirstOrDefault(c => string.Equals(c.PilotoId, b.PilotoId, StringComparison.OrdinalIgnoreCase));
                pilotos.TryGetValue(b.PilotoId, out piloto);
            }

            result.Add(new ChampionBio
            {
                ChampionId = b.CampeaoId,
                DriverId = b.PilotoId,
                OriginalName = b.NomeOriginal,
                DriverFullName = career?.Piloto ?? b.NomeOriginal,
                Nationality = career?.Nacionalidade ?? b.Nacionalidade,
                TitleYears = b.AnosTitulo,
                Biography = b.Biografia,
                Source = b.Fonte,
                PhotoUrl = piloto?.FotoUrl,
                TitlesCount = career?.Titulos ?? (b.AnosTitulo?.Split(['-', ','], StringSplitOptions.RemoveEmptyEntries).Length ?? 1),
                CareerWins = career?.Vitorias ?? 0,
                CareerPodiums = career?.Podios ?? 0,
                CareerPoles = career?.Poles ?? 0,
                CareerRaces = career?.Corridas ?? 0,
                HistoricalPhotos = photosByChampion.GetValueOrDefault(b.CampeaoId, [])
            });
        }

        // Also include any world champion in vw_CarreiraPilotos not yet in CampeoesBiografia
        var nextId = 1000;
        foreach (var c in careerChampions)
        {
            if (coveredDriverIds.Contains(c.PilotoId))
            {
                continue;
            }

            pilotos.TryGetValue(c.PilotoId, out var piloto);
            result.Add(new ChampionBio
            {
                ChampionId = nextId++,
                DriverId = c.PilotoId,
                OriginalName = c.Piloto ?? c.PilotoId,
                DriverFullName = c.Piloto ?? c.PilotoId,
                Nationality = c.Nacionalidade,
                TitleYears = titleYearsByDriver.GetValueOrDefault(c.PilotoId),
                Biography = null,
                Source = "PaddockSpace Historical Database",
                PhotoUrl = piloto?.FotoUrl,
                TitlesCount = c.Titulos,
                CareerWins = c.Vitorias ?? 0,
                CareerPodiums = c.Podios ?? 0,
                CareerPoles = c.Poles ?? 0,
                CareerRaces = c.Corridas ?? 0,
                HistoricalPhotos = []
            });
        }

        return result
            .OrderByDescending(x => x.TitlesCount)
            .ThenByDescending(x => x.CareerWins)
            .ToList();
    }

    private static ScopeCapabilitiesDto ToCapabilities(SeasonScope scope) =>
        new(scope.HasRichTelemetry, scope.HasQualifyingSplits, scope.HasSprintRaces, scope.HasConstructorChampionship);

    private static Driver MapDriver(DriverRowProjection r) =>
        new()
        {
            DriverId = r.DriverId,
            Code = r.Code,
            PermanentNumber = r.PermanentNumber,
            FirstName = r.FirstName,
            LastName = r.LastName,
            DateOfBirth = r.DateOfBirthUtc.HasValue ? DateOnly.FromDateTime(r.DateOfBirthUtc.Value) : null,
            Nationality = r.Nationality,
            PhotoUrl = r.PhotoUrl,
            Championships = r.Championships,
            Races = r.Races,
            Wins = r.Wins,
            Podiums = r.Podiums,
            Poles = r.Poles,
            FastestLaps = r.FastestLaps,
            CareerPoints = r.CareerPoints,
            FirstSeason = r.FirstSeason,
            LastSeason = r.LastSeason,
            WinPercentage = r.WinPercentage
        };

    private sealed class DriverRowProjection
    {
        public required string DriverId { get; set; }
        public string? Code { get; set; }
        public int? PermanentNumber { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? DateOfBirthUtc { get; set; }
        public string? Nationality { get; set; }
        public string? PhotoUrl { get; set; }
        public int Championships { get; set; }
        public int Races { get; set; }
        public int Wins { get; set; }
        public int Podiums { get; set; }
        public int Poles { get; set; }
        public int FastestLaps { get; set; }
        public decimal CareerPoints { get; set; }
        public int? FirstSeason { get; set; }
        public int? LastSeason { get; set; }
        public decimal WinPercentage { get; set; }
    }

    private sealed class GrandePremioWithCircuitRow
    {
        public int Rodada { get; set; }
        public required string Nome { get; set; }
        public string? CircuitoId { get; set; }
        public string? CircuitName { get; set; }
        public string? Country { get; set; }
        public DateTime? DataCorridaUTC { get; set; }
        public bool TemSprint { get; set; }
        public string? Status { get; set; }
        public int? Laps { get; set; }
    }

    private sealed class TeamPtsRow
    {
        public string EquipeId { get; set; } = string.Empty;
        public decimal TotalPts { get; set; }
    }

    private sealed class CircuitRoundRow
    {
        public string CircuitId { get; set; } = string.Empty;
        public int MinRound { get; set; }
    }

    private sealed class CircuitGpRow
    {
        public int Season { get; set; }
        public int Round { get; set; }
        public string GrandPrixName { get; set; } = string.Empty;
        public DateTime? RaceDateUtc { get; set; }
    }

    private sealed class CircuitWinnerRow
    {
        public int Temporada { get; set; }
        public int Rodada { get; set; }
        public string WinnerName { get; set; } = string.Empty;
        public string? WinnerTeam { get; set; }
    }

    private sealed class CircuitPoleRow
    {
        public int Temporada { get; set; }
        public int Rodada { get; set; }
        public string PoleName { get; set; } = string.Empty;
    }

    private sealed class CircuitFastestLapRow
    {
        public int Temporada { get; set; }
        public int Rodada { get; set; }
        public string FastestLapName { get; set; } = string.Empty;
        public string? FastestLapTeam { get; set; }
        public int? FastestLapNumber { get; set; }
        public int? FastestLapMs { get; set; }
    }

    private sealed class DriverTitleYearRow
    {
        public string PilotoId { get; set; } = string.Empty;
        public int Temporada { get; set; }
    }
}
