using PaddockSpace.Application.Abstractions;
using PaddockSpace.Infrastructure;
using PaddockSpace.Infrastructure.Persistence;

namespace PaddockSpace.Bff.Web;

public class WebBffMarker
{
    public static void Main(string[] args)
    {
        if (args.Length >= 2 && string.Equals(args[0], "--export-sqlite", StringComparison.OrdinalIgnoreCase))
        {
            SqliteDatabaseExporter.ExportSqlServerToSqliteAsync(
                SqlConnectionFactory.DefaultLocalConnectionString,
                args[1]).GetAwaiter().GetResult();
            return;
        }

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

        // Parâmetros inválidos (ex.: temporada fora de 1950–2026, categoria não suportada) -> 400, nunca 500.
        app.Use(async (context, next) =>
        {
            try
            {
                await next();
            }
            catch (Exception ex) when ((ex is ArgumentException or NotSupportedException) && !context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { error = ex.Message });
            }
        });
        app.MapOpenApi("/openapi/v1.json");

        var api = app.MapGroup("/api/web/v1");

        api.MapGet("/context", async (IFormula1QueryService service, CancellationToken ct) =>
            Results.Ok(await service.GetGlobalContextAsync(ct)));

        api.MapGet("/{category}/dashboard", async (string category, string? season, IFormula1QueryService service, CancellationToken ct) =>
        {
            try
            {
                var data = await service.GetDashboardAsync(category, season, ct);
                return Results.Ok(data);
            }
            catch (NotSupportedException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        api.MapGet("/{category}/drivers", async (string category, string? season, string? search, IFormula1QueryService service, CancellationToken ct) =>
            Results.Ok(await service.GetDriversAsync(category, season, search, ct)));

        api.MapGet("/{category}/drivers/{driverId}", async (string category, string driverId, string? season, IFormula1QueryService service, CancellationToken ct) =>
        {
            var profile = await service.GetDriverProfileAsync(category, driverId, season, ct);
            return profile is not null ? Results.Ok(profile) : Results.NotFound();
        });

        api.MapGet("/{category}/teams", async (string category, string? season, string? search, IFormula1QueryService service, CancellationToken ct) =>
            Results.Ok(await service.GetTeamsAsync(category, season, search, ct)));

        api.MapGet("/{category}/teams/{teamId}", async (string category, string teamId, string? season, IFormula1QueryService service, CancellationToken ct) =>
        {
            var profile = await service.GetTeamProfileAsync(category, teamId, season, ct);
            return profile is not null ? Results.Ok(profile) : Results.NotFound();
        });

        api.MapGet("/{category}/standings/drivers", async (string category, string? season, IFormula1QueryService service, CancellationToken ct) =>
            Results.Ok(await service.GetDriverStandingsAsync(category, season, ct)));

        api.MapGet("/{category}/standings/teams", async (string category, string? season, IFormula1QueryService service, CancellationToken ct) =>
            Results.Ok(await service.GetTeamStandingsAsync(category, season, ct)));

        api.MapGet("/{category}/races", async (string category, string? season, IFormula1QueryService service, CancellationToken ct) =>
            Results.Ok(await service.GetRacesAsync(category, season, ct)));

        api.MapGet("/{category}/races/{season:int}/{round:int}", async (string category, int season, int round, IFormula1QueryService service, CancellationToken ct) =>
        {
            try
            {
                var detail = await service.GetGrandPrixDetailAsync(category, season, round, includeTelemetry: true, ct);
                return detail is not null ? Results.Ok(detail) : Results.NotFound();
            }
            catch (ArgumentException ex) // inclui ArgumentOutOfRangeException de SeasonScope.ForYear (temporada fora de 1950–2026)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        api.MapGet("/{category}/circuits", async (string category, string? season, IFormula1QueryService service, CancellationToken ct) =>
            Results.Ok(await service.GetCircuitsAsync(category, season, ct)));

        api.MapGet("/{category}/circuits/{circuitId}", async (string category, string circuitId, IFormula1QueryService service, CancellationToken ct) =>
        {
            var detail = await service.GetCircuitDetailAsync(category, circuitId, ct);
            return detail is not null ? Results.Ok(detail) : Results.NotFound();
        });

        api.MapGet("/{category}/timeline", async (string category, IFormula1QueryService service, CancellationToken ct) =>
            Results.Ok(await service.GetTimelineAsync(category, ct)));

        api.MapGet("/{category}/hall-of-fame", async (string category, IFormula1QueryService service, CancellationToken ct) =>
            Results.Ok(await service.GetHallOfFameAsync(category, ct)));

        app.Run();
    }
}
