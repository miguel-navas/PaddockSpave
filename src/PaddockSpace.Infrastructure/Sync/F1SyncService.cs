using Microsoft.Extensions.Logging;
using PaddockSpace.Application.Abstractions;

namespace PaddockSpace.Infrastructure.Sync;

public interface IF1SyncService
{
    Task<SyncRunSummaryDto> ExecuteMaintenanceCycleAsync(string mode = "incremental", CancellationToken ct = default);
}

public sealed class F1SyncService : IF1SyncService
{
    private readonly IF1DataIngestionRepository _ingestionRepository;
    private readonly JolpicaApiClient _jolpicaClient;
    private readonly ILogger<F1SyncService> _logger;

    public F1SyncService(
        IF1DataIngestionRepository ingestionRepository,
        JolpicaApiClient jolpicaClient,
        ILogger<F1SyncService> logger)
    {
        _ingestionRepository = ingestionRepository;
        _jolpicaClient = jolpicaClient;
        _logger = logger;
    }

    public async Task<SyncRunSummaryDto> ExecuteMaintenanceCycleAsync(string mode = "incremental", CancellationToken ct = default)
    {
        var execId = await _ingestionRepository.BeginExecutionLogAsync(mode, ct);
        try
        {
            var loadedSeasons = await _ingestionRepository.GetLoadedHistoricalSeasonsAsync(ct);
            var missingHistoricalYears = Enumerable.Range(1950, 2025 - 1950 + 1)
                .Where(y => !loadedSeasons.Contains(y))
                .ToList();

            var loadedCount = 0;
            if (string.Equals(mode, "calendar-2026", StringComparison.OrdinalIgnoreCase))
            {
                var (circuitos, gps) = await _jolpicaClient.FetchSeasonCalendarAsync(2026, ct);
                if (circuitos.Count > 0)
                {
                    await _ingestionRepository.UpsertCircuitosAsync(circuitos, ct);
                }

                if (gps.Count > 0)
                {
                    await _ingestionRepository.UpsertGrandesPremiosAsync(gps, ct);
                }

                loadedCount = gps.Count;
            }

            var championMap = await _ingestionRepository.GetChampionDriverBySeasonAsync(ct);
            var bioCount = 0;
            var photoCount = 0;

            if (File.Exists(MdbExtrasReader.DefaultMdbExtrasPath))
            {
                var payload = MdbExtrasReader.LoadFromFile(MdbExtrasReader.DefaultMdbExtrasPath);
                var (bios, photos) = MdbExtrasReader.MapToIngestionItems(payload, championMap);
                await _ingestionRepository.UpsertChampionBiographiesAndPhotosAsync(bios, photos, ct);
                bioCount = bios.Count;
                photoCount = photos.Count;
            }

            var msg = $"Sincronização '{mode}' concluída: {loadedSeasons.Count}/76 temporadas históricas na base ({missingHistoricalYears.Count} pendentes), {bioCount} biografias e {photoCount} fotos históricas sincronizadas.";
            await _ingestionRepository.CompleteExecutionLogAsync(execId, loadedCount, true, msg, ct);
            _logger.LogInformation("{Message}", msg);

            return new SyncRunSummaryDto(mode, loadedCount, bioCount, photoCount, true, msg);
        }
        catch (Exception ex)
        {
            await _ingestionRepository.CompleteExecutionLogAsync(execId, 0, false, ex.Message, ct);
            _logger.LogError(ex, "Erro durante o ciclo de sincronização '{Mode}'", mode);
            throw;
        }
    }
}

