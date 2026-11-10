using PaddockSpace.Application.Abstractions;
using PaddockSpace.Infrastructure;
using PaddockSpace.Infrastructure.Sync;
using PaddockSpace.SyncWorker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddPaddockSpaceCore(builder.Configuration);
builder.Services.AddScoped<IF1DataIngestionRepository, F1DataIngestionRepository>();
builder.Services.AddSingleton(_ => new JolpicaApiClient(new HttpClient()));
builder.Services.AddSingleton(_ => new OpenF1ApiClient(new HttpClient()));
builder.Services.AddScoped<IF1SyncService, F1SyncService>();

if (args.Contains("--run-once", StringComparer.OrdinalIgnoreCase))
{
    using var host = builder.Build();
    using var scope = host.Services.CreateScope();
    var sync = scope.ServiceProvider.GetRequiredService<IF1SyncService>();
    var summary = await sync.ExecuteMaintenanceCycleAsync("run-once-extras");
    Console.WriteLine($"[SyncWorker] {summary.Message}");
    return;
}

builder.Services.AddHostedService<Worker>();
var app = builder.Build();
app.Run();
