using System.Net;
using System.Net.Http.Json;
using MinecraftSkins.Application.Dtos;
using MinecraftSkins.Tests.IntegrationTests;

namespace MinecraftSkins.Tests.IntegrationTests.Endpoints;

public class RateEndpointsIntegrationTests : IntegrationTestBase
{
    public RateEndpointsIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    public override async ValueTask InitializeAsync()
    {
        await CleanupAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetBtcUsdRate_WithoutAuth_ReturnsUnauthorized()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await Client.GetAsync("/api/rates/btc-usd", ct);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBtcUsdRate_WithAdminRole_ReturnsOkAndRateDto()
    {
        var ct = TestContext.Current.CancellationToken;

        var admin = await CreateTestUserAsync("rate-admin", "Admin123!", ct);
        await UserManager.AddToRoleAsync(admin, "Admin");
        var token = await GetAuthTokenAsync("rate-admin", "Admin123!", ct);
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await Client.GetAsync("/api/rates/btc-usd", ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<BtcRateDto>(ct);
        dto.Should().NotBeNull();
        dto!.Rate.Should().BeGreaterThan(0);
        dto.Source.Should().NotBeNullOrWhiteSpace();
    }
}
