using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using PaddockSpace.Application.Abstractions;

namespace PaddockSpace.Infrastructure.Sync;

public sealed class RateLimitedHttpGate
{
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly TimeSpan _minInterval;
    private long _lastCallTicks;

    public TimeSpan MinInterval => _minInterval;

    public RateLimitedHttpGate(TimeSpan minInterval)
    {
        _minInterval = minInterval;
    }

    public async Task WaitTurnAsync(CancellationToken ct = default)
    {
        await _mutex.WaitAsync(ct);
        try
        {
            if (_lastCallTicks > 0)
            {
                var elapsed = Stopwatch.GetElapsedTime(_lastCallTicks);
                var remaining = _minInterval - elapsed;
                if (remaining > TimeSpan.Zero)
                {
                    await Task.Delay(remaining, ct);
                }
            }

            _lastCallTicks = Stopwatch.GetTimestamp();
        }
        finally
        {
            _mutex.Release();
        }
    }
}

public sealed class JolpicaApiClient
{
    public const string DefaultBaseUrl = "https://api.jolpi.ca/ergast/f1";

    private readonly HttpClient _httpClient;
    private readonly RateLimitedHttpGate _gate;

    public TimeSpan ConfiguredInterval => _gate.MinInterval;

    public JolpicaApiClient(HttpClient httpClient, bool historicalPace = false, TimeSpan? customInterval = null)
    {
        _httpClient = httpClient;
        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(DefaultBaseUrl.TrimEnd('/') + "/");
        }

        var interval = customInterval ?? (historicalPace ? TimeSpan.FromSeconds(7.5) : TimeSpan.FromSeconds(0.6));
        _gate = new RateLimitedHttpGate(interval);
    }

    public async Task<JsonDocument?> GetJsonAsync(string relativePath, CancellationToken ct = default)
    {
        for (var attempt = 1; attempt <= 4; attempt++)
        {
            await _gate.WaitTurnAsync(ct);
            using var response = await _httpClient.GetAsync(relativePath.TrimStart('/'), ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt < 4)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(2 * attempt);
                await Task.Delay(retryAfter, ct);
                continue;
            }

            response.EnsureSuccessStatusCode();
            var stream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        }

        throw new HttpRequestException($"Limite de requisições persistente ao consultar Jolpica: {relativePath}");
    }

    public async Task<(IReadOnlyList<CircuitoIngestionItem> Circuitos, IReadOnlyList<GrandePremioIngestionItem> GrandesPremios)> FetchSeasonCalendarAsync(
        int season,
        CancellationToken ct = default)
    {
        using var doc = await GetJsonAsync($"{season}/races/?limit=100", ct);
        if (doc is null)
        {
            return ([], []);
        }

        return ParseCalendarDocument(doc);
    }

    public static (IReadOnlyList<CircuitoIngestionItem> Circuitos, IReadOnlyList<GrandePremioIngestionItem> GrandesPremios) ParseCalendarDocument(JsonDocument doc)
    {
        var circuitos = new Dictionary<string, CircuitoIngestionItem>(StringComparer.OrdinalIgnoreCase);
        var gps = new List<GrandePremioIngestionItem>();

        if (!doc.RootElement.TryGetProperty("MRData", out var mrData) ||
            !mrData.TryGetProperty("RaceTable", out var raceTable) ||
            !raceTable.TryGetProperty("Races", out var races) ||
            races.ValueKind != JsonValueKind.Array)
        {
            return ([], []);
        }

        foreach (var r in races.EnumerateArray())
        {
            var round = int.Parse(r.GetProperty("round").GetString() ?? "0");
            var raceName = r.GetProperty("raceName").GetString() ?? $"Round {round}";
            var dateStr = r.TryGetProperty("date", out var dProp) ? dProp.GetString() : null;
            var timeStr = r.TryGetProperty("time", out var tProp) ? tProp.GetString() : "00:00:00Z";
            DateTime? raceDateUtc = null;
            if (!string.IsNullOrWhiteSpace(dateStr) &&
                DateTimeOffset.TryParse($"{dateStr}T{timeStr}", out var dto))
            {
                raceDateUtc = dto.UtcDateTime;
            }

            string? circuitId = null;
            if (r.TryGetProperty("Circuit", out var c) && c.ValueKind == JsonValueKind.Object)
            {
                circuitId = c.GetProperty("circuitId").GetString();
                var circuitName = c.GetProperty("circuitName").GetString() ?? circuitId ?? "Circuito";
                string? city = null;
                string? country = null;
                decimal? lat = null;
                decimal? lng = null;

                if (c.TryGetProperty("Location", out var loc) && loc.ValueKind == JsonValueKind.Object)
                {
                    city = loc.TryGetProperty("locality", out var lCity) ? lCity.GetString() : null;
                    country = loc.TryGetProperty("country", out var lCountry) ? lCountry.GetString() : null;
                    if (loc.TryGetProperty("lat", out var lLat) && decimal.TryParse(lLat.GetString(), System.Globalization.CultureInfo.InvariantCulture, out var parsedLat))
                    {
                        lat = parsedLat;
                    }
                    if (loc.TryGetProperty("long", out var lLng) && decimal.TryParse(lLng.GetString(), System.Globalization.CultureInfo.InvariantCulture, out var parsedLng))
                    {
                        lng = parsedLng;
                    }
                }

                if (!string.IsNullOrWhiteSpace(circuitId))
                {
                    circuitos[circuitId] = new CircuitoIngestionItem(circuitId, circuitName, city, country, lat, lng);
                }
            }

            var hasSprint = r.TryGetProperty("Sprint", out _);
            var status = raceDateUtc.HasValue && raceDateUtc.Value < DateTime.UtcNow ? "CONCLUIDO" : "AGENDADO";
            gps.Add(new GrandePremioIngestionItem(round, raceName, circuitId, raceDateUtc, hasSprint, status));
        }

        return (circuitos.Values.ToList(), gps);
    }
}

public sealed class OpenF1ApiClient
{
    public const string DefaultBaseUrl = "https://api.openf1.org/v1";

    private readonly HttpClient _httpClient;
    private readonly RateLimitedHttpGate _gate;

    public TimeSpan ConfiguredInterval => _gate.MinInterval;

    public OpenF1ApiClient(HttpClient httpClient, TimeSpan? customInterval = null)
    {
        _httpClient = httpClient;
        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(DefaultBaseUrl.TrimEnd('/') + "/");
        }

        _gate = new RateLimitedHttpGate(customInterval ?? TimeSpan.FromSeconds(0.7));
    }

    public async Task<JsonDocument?> GetJsonAsync(string relativePath, CancellationToken ct = default)
    {
        await _gate.WaitTurnAsync(ct);
        using var response = await _httpClient.GetAsync(relativePath.TrimStart('/'), ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var stream = await response.Content.ReadAsStreamAsync(ct);
        return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
    }
}

public sealed class MdbExtrasPayload
{
    [JsonPropertyName("fonte")]
    public string? Fonte { get; set; }

    [JsonPropertyName("campeoes")]
    public List<MdbChampionItem> Campeoes { get; set; } = [];

    [JsonPropertyName("fotos")]
    public List<MdbPhotoItem> Fotos { get; set; } = [];
}

public sealed class MdbChampionItem
{
    [JsonPropertyName("Id")]
    public int Id { get; set; }

    [JsonPropertyName("NomeOriginal")]
    public required string NomeOriginal { get; set; }

    [JsonPropertyName("Nacionalidade")]
    public string? Nacionalidade { get; set; }

    [JsonPropertyName("AnosTitulo")]
    public string? AnosTitulo { get; set; }

    [JsonPropertyName("Biografia")]
    public string? Biografia { get; set; }
}

public sealed class MdbPhotoItem
{
    [JsonPropertyName("CampeaoId")]
    public int CampeaoId { get; set; }

    [JsonPropertyName("Ordem")]
    public int Ordem { get; set; }

    [JsonPropertyName("Local")]
    public string? Local { get; set; }

    [JsonPropertyName("Legenda")]
    public string? Legenda { get; set; }
}

public static class MdbExtrasReader
{
    public const string DefaultMdbExtrasPath = @"C:\AI\Agentes\Formula1\mdb_extras.json";

    public static MdbExtrasPayload LoadFromFile(string path = DefaultMdbExtrasPath)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Arquivo mdb_extras.json não encontrado em '{path}'.", path);
        }

        var json = File.ReadAllText(path);
        return ParseJson(json);
    }

    public static MdbExtrasPayload ParseJson(string json)
    {
        return JsonSerializer.Deserialize<MdbExtrasPayload>(json)
               ?? new MdbExtrasPayload();
    }

    public static (IReadOnlyList<CampeaoBiografiaIngestionItem> Biographies, IReadOnlyList<FotoHistoricaIngestionItem> Photos) MapToIngestionItems(
        MdbExtrasPayload payload,
        IReadOnlyDictionary<int, string> championDriverBySeason)
    {
        var source = string.IsNullOrWhiteSpace(payload.Fonte)
            ? "designerinside.mdb"
            : (payload.Fonte.Length > 200 ? payload.Fonte[..200] : payload.Fonte);

        var bios = new List<CampeaoBiografiaIngestionItem>();
        foreach (var c in payload.Campeoes)
        {
            var years = (c.AnosTitulo ?? string.Empty)
                .Replace(",", "-")
                .Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out var y) ? (int?)y : null)
                .Where(y => y.HasValue)
                .Select(y => y!.Value)
                .ToList();

            string? matchedDriverId = null;
            foreach (var y in years)
            {
                if (championDriverBySeason.TryGetValue(y, out var driverId))
                {
                    matchedDriverId = driverId;
                    break;
                }
            }

            bios.Add(new CampeaoBiografiaIngestionItem(
                c.Id,
                matchedDriverId,
                c.NomeOriginal,
                c.Nacionalidade,
                c.AnosTitulo,
                c.Biografia,
                source));
        }

        var photos = payload.Fotos
            .Select(f => new FotoHistoricaIngestionItem(f.CampeaoId, f.Ordem, f.Local, f.Legenda))
            .ToList();

        return (bios, photos);
    }
}

