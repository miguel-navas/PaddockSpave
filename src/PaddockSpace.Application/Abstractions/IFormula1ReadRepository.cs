using PaddockSpace.Application.DTOs;
using PaddockSpace.Domain.Entities;
using PaddockSpace.Domain.ValueObjects;

namespace PaddockSpace.Application.Abstractions;

public interface IFormula1ReadRepository
{
    Task<IReadOnlyList<int>> GetAvailableSeasonsAsync(CancellationToken ct = default);
    Task<SyncExecutionInfoDto?> GetLastSyncExecutionAsync(CancellationToken ct = default);

    Task<DashboardDataDto> GetCurrentSeasonDashboardAsync(CancellationToken ct = default);
    Task<DashboardDataDto> GetHistoricalSeasonDashboardAsync(int year, CancellationToken ct = default);
    Task<DashboardDataDto> GetAllTimeDashboardAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Driver>> GetDriversAsync(SeasonScope scope, string? search = null, CancellationToken ct = default);
    Task<DriverProfileDto?> GetDriverProfileAsync(string driverId, SeasonScope scope, CancellationToken ct = default);

    Task<IReadOnlyList<ConstructorTeam>> GetTeamsAsync(SeasonScope scope, string? search = null, CancellationToken ct = default);
    Task<TeamProfileDto?> GetTeamProfileAsync(string teamId, SeasonScope scope, CancellationToken ct = default);

    Task<IReadOnlyList<DriverStandingDto>> GetDriverStandingsAsync(SeasonScope scope, CancellationToken ct = default);
    Task<IReadOnlyList<TeamStandingDto>> GetTeamStandingsAsync(SeasonScope scope, CancellationToken ct = default);

    Task<IReadOnlyList<GrandPrix>> GetRacesAsync(SeasonScope scope, CancellationToken ct = default);
    Task<GrandPrixDetailDto?> GetGrandPrixDetailAsync(int season, int round, bool includeTelemetry, CancellationToken ct = default);
    Task<RaceTelemetryDto?> GetRaceTelemetryAsync(int season, int round, CancellationToken ct = default);

    Task<IReadOnlyList<Circuit>> GetCircuitsAsync(SeasonScope? scope = null, CancellationToken ct = default);
    Task<CircuitDetailDto?> GetCircuitDetailAsync(string circuitId, CancellationToken ct = default);

    Task<IReadOnlyList<SeasonSummary>> GetTimelineAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ChampionBio>> GetHallOfFameAsync(CancellationToken ct = default);
}

public interface IFormula1QueryService
{
    Task<GlobalContextDto> GetGlobalContextAsync(CancellationToken ct = default);
    Task<DashboardDataDto> GetDashboardAsync(string? category, string? seasonScope, CancellationToken ct = default);
    Task<IReadOnlyList<Driver>> GetDriversAsync(string? category, string? seasonScope, string? search = null, CancellationToken ct = default);
    Task<DriverProfileDto?> GetDriverProfileAsync(string? category, string driverId, string? seasonScope, CancellationToken ct = default);
    Task<IReadOnlyList<ConstructorTeam>> GetTeamsAsync(string? category, string? seasonScope, string? search = null, CancellationToken ct = default);
    Task<TeamProfileDto?> GetTeamProfileAsync(string? category, string teamId, string? seasonScope, CancellationToken ct = default);
    Task<IReadOnlyList<DriverStandingDto>> GetDriverStandingsAsync(string? category, string? seasonScope, CancellationToken ct = default);
    Task<IReadOnlyList<TeamStandingDto>> GetTeamStandingsAsync(string? category, string? seasonScope, CancellationToken ct = default);
    Task<IReadOnlyList<GrandPrix>> GetRacesAsync(string? category, string? seasonScope, CancellationToken ct = default);
    Task<GrandPrixDetailDto?> GetGrandPrixDetailAsync(string? category, int season, int round, bool includeTelemetry, CancellationToken ct = default);
    Task<RaceTelemetryDto?> GetRaceTelemetryAsync(string? category, int season, int round, CancellationToken ct = default);
    Task<IReadOnlyList<Circuit>> GetCircuitsAsync(string? category, string? seasonScope = null, CancellationToken ct = default);
    Task<CircuitDetailDto?> GetCircuitDetailAsync(string? category, string circuitId, CancellationToken ct = default);
    Task<IReadOnlyList<SeasonSummary>> GetTimelineAsync(string? category, CancellationToken ct = default);
    Task<IReadOnlyList<ChampionBio>> GetHallOfFameAsync(string? category, CancellationToken ct = default);
}

