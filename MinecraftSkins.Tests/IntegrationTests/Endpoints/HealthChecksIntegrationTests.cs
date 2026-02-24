using System.Net;
using System.Text.Json;
using MinecraftSkins.Tests.IntegrationTests;

namespace MinecraftSkins.Tests.IntegrationTests.Endpoints;

public class HealthChecksIntegrationTests : IntegrationTestBase
{
    public HealthChecksIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Health_ReturnsOkAndExpectedJsonShape()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await Client.GetAsync("/health", ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("status", out var status).Should().BeTrue();
        status.GetString().Should().NotBeNullOrWhiteSpace();
        doc.RootElement.TryGetProperty("entries", out var entries).Should().BeTrue();
        entries.ValueKind.Should().Be(JsonValueKind.Object);
    }
}
