using PaddockSpace.Infrastructure.Sync;

namespace PaddockSpace.SyncWorker;

public sealed class Worker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Worker> _logger;

    public Worker(IServiceProvider serviceProvider, ILogger<Worker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<IF1SyncService>();
                await syncService.ExecuteMaintenanceCycleAsync("background-incremental", stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha no ciclo periódico do PaddockSpace.SyncWorker.");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
