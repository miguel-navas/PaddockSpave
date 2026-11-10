using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using PaddockSpace.Infrastructure.Persistence;
using PaddockSpace.Infrastructure.Sync;

namespace PaddockSpace.Tests.Sync;

public class SyncAndIngestionTests
{
    [Fact]
    public async Task RateLimitedHttpGate_ShouldEnforceMinimumIntervalBetweenRequests()
    {
        var gate = new RateLimitedHttpGate(TimeSpan.FromMilliseconds(120));
        var sw = Stopwatch.StartNew();

        await gate.WaitTurnAsync();
        await gate.WaitTurnAsync();

        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds >= 100, $"Expected >= 100ms, got {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void JolpicaAndOpenF1Clients_ShouldConfigureExpectedDefaultIntervals()
    {
        using var http1 = new HttpClient();
        using var http2 = new HttpClient();
        using var http3 = new HttpClient();

        var jolpicaCurrent = new JolpicaApiClient(http1, historicalPace: false);
        var jolpicaHistorical = new JolpicaApiClient(http2, historicalPace: true);
        var openF1 = new OpenF1ApiClient(http3);

        Assert.Equal(TimeSpan.FromSeconds(0.6), jolpicaCurrent.ConfiguredInterval);
        Assert.Equal(TimeSpan.FromSeconds(7.5), jolpicaHistorical.ConfiguredInterval);
        Assert.Equal(TimeSpan.FromSeconds(0.7), openF1.ConfiguredInterval);
    }

    [Fact]
    public void JolpicaApiClient_ParseCalendarDocument_ShouldExtractCircuitsAndRaces()
    {
        const string sampleJson = """
        {
          "MRData": {
            "RaceTable": {
              "Races": [
                {
                  "season": "2026",
                  "round": "1",
                  "raceName": "Australian Grand Prix",
                  "date": "2026-03-08",
                  "time": "04:00:00Z",
                  "Circuit": {
                    "circuitId": "albert_park",
                    "circuitName": "Albert Park Grand Prix Circuit",
                    "Location": {
                      "lat": "-37.8497",
                      "long": "144.968",
                      "locality": "Melbourne",
                      "country": "Australia"
                    }
                  }
                }
              ]
            }
          }
        }
        """;

        using var doc = JsonDocument.Parse(sampleJson);
        var (circuitos, gps) = JolpicaApiClient.ParseCalendarDocument(doc);

        Assert.Single(circuitos);
        Assert.Equal("albert_park", circuitos[0].CircuitoId);
        Assert.Equal("Australia", circuitos[0].Pais);

        Assert.Single(gps);
        Assert.Equal(1, gps[0].Rodada);
        Assert.Equal("Australian Grand Prix", gps[0].Nome);
    }

    [Fact]
    public void MdbExtrasReader_ShouldLoadRealMdbExtrasAndLinkToChampions()
    {
        var payload = MdbExtrasReader.LoadFromFile();
        Assert.Equal(27, payload.Campeoes.Count);
        Assert.Equal(368, payload.Fotos.Count);

        var map = new Dictionary<int, string>
        {
            [1950] = "farina",
            [1988] = "senna",
            [1994] = "michael_schumacher"
        };

        var (bios, photos) = MdbExtrasReader.MapToIngestionItems(payload, map);
        Assert.Equal(27, bios.Count);
        Assert.Equal(368, photos.Count);
        Assert.Contains(bios, b => b.PilotoId == "senna");
        Assert.Contains(bios, b => b.PilotoId == "farina");
    }

    [Theory]
    [InlineData("DROP TABLE dbo.Pilotos", "dbo.Pilotos")]
    [InlineData("ALTER TABLE dbo.Pilotos ADD X INT", "dbo.Pilotos")]
    [InlineData("TRUNCATE TABLE dbo.FotosHistoricas", "dbo.FotosHistoricas")]
    [InlineData("CREATE TABLE dbo.Teste (Id INT)", "dbo.Pilotos")]
    [InlineData("INSERT INTO dbo.TabelaNaoAutorizada VALUES (1)", "dbo.TabelaNaoAutorizada")]
    public void F1DataIngestionRepository_ShouldRejectAnyDdlOrUnauthorizedTable(string forbiddenSql, string targetTable)
    {
        Assert.Throws<InvalidOperationException>(() =>
            F1DataIngestionRepository.ValidateSqlIsDataIngestionOnly(forbiddenSql, targetTable));
    }
}

