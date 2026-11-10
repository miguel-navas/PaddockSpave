using PaddockSpace.Application.Abstractions;
using PaddockSpace.Application.DTOs;
using PaddockSpace.Domain.Entities;
using PaddockSpace.Domain.ValueObjects;

namespace PaddockSpace.Application.Services;

public sealed class Formula1QueryService : IFormula1QueryService
{
    private readonly IFormula1ReadRepository _repository;

    public Formula1QueryService(IFormula1ReadRepository repository)
    {
        _repository = repository;
    }

    public async Task<GlobalContextDto> GetGlobalContextAsync(CancellationToken ct = default)
    {
        var categories = CategoryCode.ListAll()
            .Select(c => new CategoryOptionDto(c.Code, c.DisplayName, c.IsAvailable))
            .ToList();

        var seasons = await _repository.GetAvailableSeasonsAsync(ct);
        var lastSync = await _repository.GetLastSyncExecutionAsync(ct);

        return new GlobalContextDto(
            categories,
            SeasonScope.CurrentFormula1Season,
            seasons,
            lastSync);
    }

    public Task<DashboardDataDto> GetDashboardAsync(string? category, string? seasonScope, CancellationToken ct = default)
    {
        EnsureFormula1Category(category);
        var scope = SeasonScope.Parse(seasonScope);

        return scope.Kind switch
        {
            SeasonScopeKind.Current => _repository.GetCurrentSeasonDashboardAsync(ct),
            SeasonScopeKind.Historical => _repository.GetHistoricalSeasonDashboardAsync(scope.Year!.Value, ct),
            SeasonScopeKind.AllTime => _repository.GetAllTimeDashboardAsync(ct),
            _ => _repository.GetCurrentSeasonDashboardAsync(ct)
        };
    }

    public Task<IReadOnlyList<Driver>> GetDriversAsync(string? category, string? seasonScope, string? search = null, CancellationToken ct = default)
    {
        EnsureFormula1Category(category);
        var scope = SeasonScope.Parse(seasonScope);
        return _repository.GetDriversAsync(scope, search, ct);
    }

    public Task<DriverProfileDto?> GetDriverProfileAsync(string? category, string driverId, string? seasonScope, CancellationToken ct = default)
    {
        EnsureFormula1Category(category);
        var scope = SeasonScope.Parse(seasonScope);
        return _repository.GetDriverProfileAsync(driverId, scope, ct);
    }

    public Task<IReadOnlyList<ConstructorTeam>> GetTeamsAsync(string? category, string? seasonScope, string? search = null, CancellationToken ct = default)
    {
        EnsureFormula1Category(category);
        var scope = SeasonScope.Parse(seasonScope);
        return _repository.GetTeamsAsync(scope, search, ct);
    }

    public Task<TeamProfileDto?> GetTeamProfileAsync(string? category, string teamId, string? seasonScope, CancellationToken ct = default)
    {
        EnsureFormula1Category(category);
        var scope = SeasonScope.Parse(seasonScope);
        return _repository.GetTeamProfileAsync(teamId, scope, ct);
    }

    public Task<IReadOnlyList<DriverStandingDto>> GetDriverStandingsAsync(string? category, string? seasonScope, CancellationToken ct = default)
    {
        EnsureFormula1Category(category);
        var scope = SeasonScope.Parse(seasonScope);
        return _repository.GetDriverStandingsAsync(scope, ct);
    }

    public Task<IReadOnlyList<TeamStandingDto>> GetTeamStandingsAsync(string? category, string? seasonScope, CancellationToken ct = default)
    {
        EnsureFormula1Category(category);
        var scope = SeasonScope.Parse(seasonScope);
        return _repository.GetTeamStandingsAsync(scope, ct);
    }

    public Task<IReadOnlyList<GrandPrix>> GetRacesAsync(string? category, string? seasonScope, CancellationToken ct = default)
    {
        EnsureFormula1Category(category);
        var scope = SeasonScope.Parse(seasonScope);
        return _repository.GetRacesAsync(scope, ct);
    }

    public Task<GrandPrixDetailDto?> GetGrandPrixDetailAsync(string? category, int season, int round, bool includeTelemetry, CancellationToken ct = default)
    {
        EnsureFormula1Category(category);
        _ = SeasonScope.ForYear(season);
        return _repository.GetGrandPrixDetailAsync(season, round, includeTelemetry, ct);
    }

    public Task<RaceTelemetryDto?> GetRaceTelemetryAsync(string? category, int season, int round, CancellationToken ct = default)
    {
        EnsureFormula1Category(category);
        _ = SeasonScope.ForYear(season);
        return _repository.GetRaceTelemetryAsync(season, round, ct);
    }

    public Task<IReadOnlyList<Circuit>> GetCircuitsAsync(string? category, string? seasonScope = null, CancellationToken ct = default)
    {
        EnsureFormula1Category(category);
        var scope = string.IsNullOrWhiteSpace(seasonScope) ? SeasonScope.AllTime() : SeasonScope.Parse(seasonScope);
        return _repository.GetCircuitsAsync(scope, ct);
    }

    public Task<CircuitDetailDto?> GetCircuitDetailAsync(string? category, string circuitId, CancellationToken ct = default)
    {
        EnsureFormula1Category(category);
        return _repository.GetCircuitDetailAsync(circuitId, ct);
    }

    public Task<IReadOnlyList<SeasonSummary>> GetTimelineAsync(string? category, CancellationToken ct = default)
    {
        EnsureFormula1Category(category);
        return _repository.GetTimelineAsync(ct);
    }

    public Task<IReadOnlyList<ChampionBio>> GetHallOfFameAsync(string? category, CancellationToken ct = default)
    {
        EnsureFormula1Category(category);
        return _repository.GetHallOfFameAsync(ct);
    }

    private static void EnsureFormula1Category(string? category)
    {
        var parsed = CategoryCode.Parse(category);
        if (!parsed.IsFormula1)
        {
            throw new NotSupportedException($"A categoria '{parsed.DisplayName}' está mapeada no roadmap, mas apenas Fórmula 1 ('f1') possui dados ativos nesta fase.");
        }
    }
}

