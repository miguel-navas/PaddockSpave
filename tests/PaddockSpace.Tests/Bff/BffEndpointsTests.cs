using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PaddockSpace.Bff.Mobile;
using PaddockSpace.Bff.Web;

namespace PaddockSpace.Tests.Bff;

public class BffEndpointsTests
{
    [Fact]
    public async Task WebBff_ShouldReturn200ForAllCoreEndpointsAndOpenApi()
    {
        await using var factory = new WebApplicationFactory<WebBffMarker>();
        using var client = factory.CreateClient();

        var endpoints = new[]
        {
            "/openapi/v1.json",
            "/api/web/v1/context",
            "/api/web/v1/f1/dashboard?season=2026",
            "/api/web/v1/f1/dashboard?season=1991",
            "/api/web/v1/f1/dashboard?season=all",
            "/api/web/v1/f1/drivers?season=all&search=Senna",
            "/api/web/v1/f1/drivers/senna?season=all",
            "/api/web/v1/f1/teams?season=all",
            "/api/web/v1/f1/teams/ferrari?season=all",
            "/api/web/v1/f1/standings/drivers?season=2026",
            "/api/web/v1/f1/standings/teams?season=2026",
            "/api/web/v1/f1/races?season=2026",
            "/api/web/v1/f1/races/2026/1",
            "/api/web/v1/f1/circuits",
            "/api/web/v1/f1/circuits/interlagos",
            "/api/web/v1/f1/timeline",
            "/api/web/v1/f1/hall-of-fame"
        };

        foreach (var url in endpoints)
        {
            var resp = await client.GetAsync(url);
            var content = await resp.Content.ReadAsStringAsync();
            var detalhe = content.Length > 1500 ? content[..1500] : content; // em Development o 500 traz a exceção no corpo
            Assert.True(resp.StatusCode == HttpStatusCode.OK, $"Expected 200 OK for {url}, got {resp.StatusCode}. Corpo: {detalhe}");
            Assert.False(string.IsNullOrWhiteSpace(content));
        }
    }

    [Fact]
    public async Task MobileBff_ShouldReturnCompactPayloadsPaginatedListsAndOnDemandTelemetry()
    {
        await using var factory = new WebApplicationFactory<MobileBffMarker>();
        using var client = factory.CreateClient();

        var openApiResp = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, openApiResp.StatusCode);

        var homeResp = await client.GetAsync("/api/mobile/v1/f1/home?season=2026");
        Assert.Equal(HttpStatusCode.OK, homeResp.StatusCode);
        using var homeDoc = JsonDocument.Parse(await homeResp.Content.ReadAsStringAsync());
        Assert.Equal(5, homeDoc.RootElement.GetProperty("top5Drivers").GetArrayLength());
        Assert.Equal(5, homeDoc.RootElement.GetProperty("top5Teams").GetArrayLength());

        var driversPageResp = await client.GetAsync("/api/mobile/v1/f1/drivers?season=all&page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, driversPageResp.StatusCode);
        using var driversDoc = JsonDocument.Parse(await driversPageResp.Content.ReadAsStringAsync());
        Assert.Equal(1, driversDoc.RootElement.GetProperty("page").GetInt32());
        Assert.Equal(10, driversDoc.RootElement.GetProperty("items").GetArrayLength());

        // Race detail without telemetry by default on Mobile
        var raceResp = await client.GetAsync("/api/mobile/v1/f1/races/2026/1");
        Assert.Equal(HttpStatusCode.OK, raceResp.StatusCode);
        using var raceDoc = JsonDocument.Parse(await raceResp.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, raceDoc.RootElement.GetProperty("telemetry").ValueKind);

        // On-demand telemetry endpoint on Mobile
        var telemetryResp = await client.GetAsync("/api/mobile/v1/f1/races/2026/1/telemetry");
        Assert.Equal(HttpStatusCode.OK, telemetryResp.StatusCode);
        using var telDoc = JsonDocument.Parse(await telemetryResp.Content.ReadAsStringAsync());
        Assert.True(telDoc.RootElement.GetProperty("pitStops").GetArrayLength() > 0);
    }
}

