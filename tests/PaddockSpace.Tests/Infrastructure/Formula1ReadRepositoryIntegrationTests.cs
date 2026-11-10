using Microsoft.EntityFrameworkCore;
using PaddockSpace.Domain.ValueObjects;
using PaddockSpace.Infrastructure.Persistence;
using PaddockSpace.Infrastructure.Repositories;

namespace PaddockSpace.Tests.Infrastructure;

public class Formula1ReadRepositoryIntegrationTests
{
    private static PaddockSpaceDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PaddockSpaceDbContext>()
            .UseSqlServer(SqlConnectionFactory.DefaultLocalConnectionString)
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;

        return new PaddockSpaceDbContext(options);
    }

    [Fact]
    public async Task DbContext_ShouldQueryBaseTablesAndAnalyticalViewsWithNoTracking()
    {
        await using var db = CreateDbContext();

        Assert.Equal(QueryTrackingBehavior.NoTracking, db.ChangeTracker.QueryTrackingBehavior);

        var pilotosCount = await db.Pilotos.CountAsync();
        var equipesCount = await db.Equipes.CountAsync();
        var circuitosCount = await db.Circuitos.CountAsync();
        var campeoesViewCount = await db.VwCampeoes.CountAsync();
        var carreiraPilotosCount = await db.VwCarreiraPilotos.CountAsync();
        var carreiraEquipesCount = await db.VwCarreiraEquipes.CountAsync();

        Assert.True(pilotosCount > 700);
        Assert.True(equipesCount > 150);
        Assert.True(circuitosCount > 60);
        Assert.True(campeoesViewCount >= 64);
        Assert.True(carreiraPilotosCount > 700);
        Assert.True(carreiraEquipesCount > 150);
    }

    [Fact]
    public async Task Repository_ShouldReturnConsistentDataFor2026_1991_AndAllTime()
    {
        await using var db = CreateDbContext();
        var factory = new SqlConnectionFactory();
        var repo = new Formula1ReadRepository(factory, db);

        // 1. Current season (2026)
        var dash2026 = await repo.GetCurrentSeasonDashboardAsync();
        Assert.Equal("2026", dash2026.ScopeKey);
        Assert.True(dash2026.Capabilities.HasRichTelemetry);
        Assert.NotEmpty(dash2026.DriverStandings);
        Assert.NotEmpty(dash2026.TeamStandings);
        Assert.NotEmpty(dash2026.Races);
        Assert.NotEmpty(dash2026.FastestPitStops);

        // 2. Historical season (1991 - Senna champion)
        var dash1991 = await repo.GetHistoricalSeasonDashboardAsync(1991);
        Assert.Equal("1991", dash1991.ScopeKey);
        Assert.False(dash1991.Capabilities.HasRichTelemetry);
        Assert.NotEmpty(dash1991.DriverStandings);
        Assert.Contains("Senna", dash1991.DriverStandings[0].DriverName, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(dash1991.Races);

        // 3. All-Time ('all')
        var dashAll = await repo.GetAllTimeDashboardAsync();
        Assert.Equal("all", dashAll.ScopeKey);
        Assert.NotEmpty(dashAll.CareerTopDrivers);
        Assert.NotEmpty(dashAll.CareerTopTeams);
        Assert.NotEmpty(dashAll.RecentChampions);

        // 4. Circuits & Timeline
        var circuits = await repo.GetCircuitsAsync();
        var timeline = await repo.GetTimelineAsync();
        Assert.NotEmpty(circuits);
        Assert.NotEmpty(timeline);

        // 5. Race detail for 2026 Round 1 (with telemetry) and 1991 Round 1
        var gp2026R1 = await repo.GetGrandPrixDetailAsync(2026, 1, includeTelemetry: true);
        Assert.NotNull(gp2026R1);
        Assert.NotEmpty(gp2026R1!.RaceResults);
        Assert.NotNull(gp2026R1.Telemetry);

        var gp1991R1 = await repo.GetGrandPrixDetailAsync(1991, 1, includeTelemetry: true);
        Assert.NotNull(gp1991R1);
        Assert.NotEmpty(gp1991R1!.RaceResults);
        Assert.Null(gp1991R1.Telemetry);
    }
}

