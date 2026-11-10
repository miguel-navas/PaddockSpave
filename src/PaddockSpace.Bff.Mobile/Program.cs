using PaddockSpace.Application.Abstractions;
using PaddockSpace.Infrastructure;

namespace PaddockSpace.Bff.Mobile;

public sealed record MobilePagedResponse<T>(
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyList<T> Items);

public sealed record MobileHomeCardDto(
    string Category,
    string ScopeKey,
    string ScopeKind,
    int? Year,
    string? LeaderDriver,
    decimal? LeaderDriverPoints,
    string? LeaderTeam,
    decimal? LeaderTeamPoints,
    string? HighlightGrandPrix,
    DateTime? HighlightDateBrasilia,
    IReadOnlyList<object> Top5Drivers,
    IReadOnlyList<object> Top5Teams,
    IReadOnlyList<object> NextOrRecentSessions);

public class MobileBffMarker
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddOpenApi();
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader());
        });

        builder.Services.AddPaddockSpaceCore(builder.Configuration);

        var app = builder.Build();

        app.UseCors();
        app.MapOpenApi("/openapi/v1.json");

        var api = app.MapGroup("/api/mobile/v1");

        api.MapGet("/context", async (IFormula1QueryService service, CancellationToken ct) =>
            Results.Ok(await service.GetGlobalContextAsync(ct)));

        api.MapGet("/{category}/home", async (string category, string? season, IFormula1QueryService service, CancellationToken ct) =>
        {
            var dash = await service.GetDashboardAsync(category, season, ct);

            var topDrivers = dash.DriverStandings.Count > 0
                ? dash.DriverStandings.Take(5).Select(d => (object)new
                {
                    d.Position,
                    d.DriverId,
                    d.DriverName,
                    d.Code,
                    d.TeamName,
                    d.TeamColor,
                    d.Points,
                    d.Wins
                }).ToList()
                : dash.CareerTopDrivers.Take(5).Select((d, idx) => (object)new
                {
                    Position = idx + 1,
                    d.DriverId,
                    DriverName = d.FullName,
                    d.Code,
                    TeamName = $"{d.Championships}x Campeão",
                    TeamColor = (string?)null,
                    Points = d.CareerPoints,
                    d.Wins
                }).ToList();

            var topTeams = dash.TeamStandings.Count > 0
                ? dash.TeamStandings.Take(5).Select(t => (object)new
                {
                    t.Position,
                    t.TeamId,
                    t.TeamName,
                    t.ColorHex,
                    t.Points,
                    t.Wins
                }).ToList()
                : dash.CareerTopTeams.Take(5).Select((t, idx) => (object)new
                {
                    Position = idx + 1,
                    t.TeamId,
                    TeamName = t.Name,
                    t.ColorHex,
                    Points = (decimal)t.ConstructorTitles,
                    t.Wins
                }).ToList();

            var sessions = dash.UpcomingOrRecentSessions.Take(5).Select(s => (object)new
            {
                s.Round,
                s.GrandPrix,
                s.SessionName,
                s.StartBrasilia,
                s.DataLoaded
            }).ToList();

            var home = new MobileHomeCardDto(
                dash.Category,
                dash.ScopeKey,
                dash.ScopeKind,
                dash.Year,
                dash.Kpis.LeaderOrChampionDriver,
                dash.Kpis.LeaderOrChampionDriverPoints,
                dash.Kpis.LeaderOrChampionTeam,
                dash.Kpis.LeaderOrChampionTeamPoints,
                dash.Kpis.NextOrLastGrandPrix,
                dash.Kpis.NextOrLastGrandPrixDateBrasilia,
                topDrivers,
                topTeams,
                sessions);

            return Results.Ok(home);
        });

        api.MapGet("/{category}/drivers", async (string category, string? season, string? search, int page = 1, int pageSize = 20, IFormula1QueryService service = null!, CancellationToken ct = default) =>
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);
            var all = await service.GetDriversAsync(category, season, search, ct);
            var items = all
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new
                {
                    d.DriverId,
                    d.FullName,
                    d.Code,
                    d.Nationality,
                    d.Championships,
                    d.Wins,
                    d.Podiums,
                    d.Poles,
                    d.Races,
                    d.PhotoUrl
                })
                .ToList();

            var totalPages = (int)Math.Ceiling(all.Count / (double)pageSize);
            return Results.Ok(new MobilePagedResponse<object>(page, pageSize, all.Count, totalPages, items));
        });

        api.MapGet("/{category}/standings/drivers", async (string category, string? season, int page = 1, int pageSize = 20, IFormula1QueryService service = null!, CancellationToken ct = default) =>
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);
            var all = await service.GetDriverStandingsAsync(category, season, ct);
            var items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var totalPages = (int)Math.Ceiling(all.Count / (double)pageSize);
            return Results.Ok(new MobilePagedResponse<object>(page, pageSize, all.Count, totalPages, items));
        });

        api.MapGet("/{category}/races", async (string category, string? season, int page = 1, int pageSize = 15, IFormula1QueryService service = null!, CancellationToken ct = default) =>
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);
            var all = await service.GetRacesAsync(category, season, ct);
            var items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var totalPages = (int)Math.Ceiling(all.Count / (double)pageSize);
            return Results.Ok(new MobilePagedResponse<object>(page, pageSize, all.Count, totalPages, items));
        });

        // Mobile race summary WITHOUT heavy telemetry payload by default
        api.MapGet("/{category}/races/{season:int}/{round:int}", async (string category, int season, int round, IFormula1QueryService service, CancellationToken ct) =>
        {
            var detail = await service.GetGrandPrixDetailAsync(category, season, round, includeTelemetry: false, ct);
            return detail is not null ? Results.Ok(detail) : Results.NotFound();
        });

        // On-demand telemetry endpoint for Mobile tab
        api.MapGet("/{category}/races/{season:int}/{round:int}/telemetry", async (string category, int season, int round, IFormula1QueryService service, CancellationToken ct) =>
        {
            var telemetry = await service.GetRaceTelemetryAsync(category, season, round, ct);
            return telemetry is not null ? Results.Ok(telemetry) : Results.NotFound();
        });

        app.Run();
    }
}
