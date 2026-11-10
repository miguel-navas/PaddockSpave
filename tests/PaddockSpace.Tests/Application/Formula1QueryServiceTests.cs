using PaddockSpace.Application.Abstractions;
using PaddockSpace.Application.DTOs;
using PaddockSpace.Application.Services;
using PaddockSpace.Domain.Entities;
using PaddockSpace.Domain.ValueObjects;

namespace PaddockSpace.Tests.Application;

public class Formula1QueryServiceTests
{
    [Fact]
    public async Task GetDashboardAsync_ShouldRouteBySeasonScopeCorrectly()
    {
        var spy = new SpyFormula1ReadRepository();
        var service = new Formula1QueryService(spy);

        var current = await service.GetDashboardAsync("f1", "2026");
        var historical = await service.GetDashboardAsync("f1", "1991");
        var allTime = await service.GetDashboardAsync("f1", "all");

        Assert.Equal("2026", current.ScopeKey);
        Assert.Equal("Current", current.ScopeKind);
        Assert.Equal(1, spy.CurrentDashboardCalls);

        Assert.Equal("1991", historical.ScopeKey);
        Assert.Equal("Historical", historical.ScopeKind);
        Assert.Equal(1991, spy.LastHistoricalYearCalled);

        Assert.Equal("all", allTime.ScopeKey);
        Assert.Equal("AllTime", allTime.ScopeKind);
        Assert.Equal(1, spy.AllTimeDashboardCalls);
    }

    [Fact]
    public async Task GetDashboardAsync_ShouldThrowNotSupported_WhenCategoryIsFutureCategory()
    {
        var spy = new SpyFormula1ReadRepository();
        var service = new Formula1QueryService(spy);

        await Assert.ThrowsAsync<NotSupportedException>(() => service.GetDashboardAsync("indy", "2026"));
    }

    private sealed class SpyFormula1ReadRepository : IFormula1ReadRepository
    {
        public int CurrentDashboardCalls { get; private set; }
        public int? LastHistoricalYearCalled { get; private set; }
        public int AllTimeDashboardCalls { get; private set; }

        public Task<IReadOnlyList<int>> GetAvailableSeasonsAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<int>>([2026, 2013, 1991, 1950]);

        public Task<SyncExecutionInfoDto?> GetLastSyncExecutionAsync(CancellationToken ct = default) =>
            Task.FromResult<SyncExecutionInfoDto?>(null);

        public Task<DashboardDataDto> GetCurrentSeasonDashboardAsync(CancellationToken ct = default)
        {
            CurrentDashboardCalls++;
            return Task.FromResult(CreateDummyDashboard("2026", "Current", 2026));
        }

        public Task<DashboardDataDto> GetHistoricalSeasonDashboardAsync(int year, CancellationToken ct = default)
        {
            LastHistoricalYearCalled = year;
            return Task.FromResult(CreateDummyDashboard(year.ToString(), "Historical", year));
        }

        public Task<DashboardDataDto> GetAllTimeDashboardAsync(CancellationToken ct = default)
        {
            AllTimeDashboardCalls++;
            return Task.FromResult(CreateDummyDashboard("all", "AllTime", null));
        }

        public Task<IReadOnlyList<Driver>> GetDriversAsync(SeasonScope scope, string? search = null, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Driver>>([]);

        public Task<DriverProfileDto?> GetDriverProfileAsync(string driverId, SeasonScope scope, CancellationToken ct = default) =>
            Task.FromResult<DriverProfileDto?>(null);

        public Task<IReadOnlyList<ConstructorTeam>> GetTeamsAsync(SeasonScope scope, string? search = null, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConstructorTeam>>([]);

        public Task<TeamProfileDto?> GetTeamProfileAsync(string teamId, SeasonScope scope, CancellationToken ct = default) =>
            Task.FromResult<TeamProfileDto?>(null);

        public Task<IReadOnlyList<DriverStandingDto>> GetDriverStandingsAsync(SeasonScope scope, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<DriverStandingDto>>([]);

        public Task<IReadOnlyList<TeamStandingDto>> GetTeamStandingsAsync(SeasonScope scope, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<TeamStandingDto>>([]);

        public Task<IReadOnlyList<GrandPrix>> GetRacesAsync(SeasonScope scope, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<GrandPrix>>([]);

        public Task<GrandPrixDetailDto?> GetGrandPrixDetailAsync(int season, int round, bool includeTelemetry, CancellationToken ct = default) =>
            Task.FromResult<GrandPrixDetailDto?>(null);

        public Task<RaceTelemetryDto?> GetRaceTelemetryAsync(int season, int round, CancellationToken ct = default) =>
            Task.FromResult<RaceTelemetryDto?>(null);

        public Task<IReadOnlyList<Circuit>> GetCircuitsAsync(SeasonScope? scope = null, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Circuit>>([]);

        public Task<CircuitDetailDto?> GetCircuitDetailAsync(string circuitId, CancellationToken ct = default) =>
            Task.FromResult<CircuitDetailDto?>(null);

        public Task<IReadOnlyList<SeasonSummary>> GetTimelineAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<SeasonSummary>>([]);

        public Task<IReadOnlyList<ChampionBio>> GetHallOfFameAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ChampionBio>>([]);

        private static DashboardDataDto CreateDummyDashboard(string key, string kind, int? year) =>
            new(
                "f1",
                key,
                kind,
                year,
                new ScopeCapabilitiesDto(year == 2026, true, true, true),
                new DashboardKpisDto(0, 0, 0, 0, 0, null, null, null, null, null, null),
                [], [], [], [], [], [], [], [], [], [], []);
    }
}

