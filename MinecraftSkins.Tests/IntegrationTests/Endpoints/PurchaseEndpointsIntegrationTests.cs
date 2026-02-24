using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using MinecraftSkins.Application.Dtos;
using MinecraftSkins.Domain.Models;
using MinecraftSkins.Tests.IntegrationTests;
using MinecraftSkins.Tests.Mocks.Helpers;

namespace MinecraftSkins.Tests.IntegrationTests.Endpoints;

public class PurchaseEndpointsIntegrationTests : IntegrationTestBase
{
    public PurchaseEndpointsIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    public override async ValueTask InitializeAsync()
    {
        await CleanupAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CreatePurchase_WithValidSkin_ReturnsCreated()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var user = await CreateTestUserAsync(cancellationToken: ct);
        var token = await GetAuthTokenAsync(user.UserName!, "Test123!", ct);
        var skin = await CreateTestSkinAsync(isAvailable: true, basePriceUsd: 10m, cancellationToken: ct);
        
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var purchaseDto = new PurchaseCreateDto { SkinId = skin.Id };
        var idempotencyKey = Guid.NewGuid().ToString();
        Client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);

        // Act
        var response = await Client.PostAsJsonAsync("/api/purchases", purchaseDto, ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var purchase = await response.Content.ReadFromJsonAsync<PurchaseDto>(ct);
        purchase.Should().NotBeNull();
        purchase!.SkinId.Should().Be(skin.Id);
        purchase.BuyerId.Should().Be(user.Id);
        purchase.PriceUsdFinal.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreatePurchase_WithIdempotencyKey_ReturnsSameResult()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var user = await CreateTestUserAsync(cancellationToken: ct);
        var token = await GetAuthTokenAsync(user.UserName!, "Test123!", ct);
        var skin = await CreateTestSkinAsync(isAvailable: true, cancellationToken: ct);
        
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var purchaseDto = new PurchaseCreateDto { SkinId = skin.Id };
        var idempotencyKey = Guid.NewGuid().ToString();
        Client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);

        // Act - первый запрос
        var response1 = await Client.PostAsJsonAsync("/api/purchases", purchaseDto, ct);
        var purchase1 = await response1.Content.ReadFromJsonAsync<PurchaseDto>(ct);

        // Act - второй запрос с тем же ключом
        var response2 = await Client.PostAsJsonAsync("/api/purchases", purchaseDto, ct);
        var purchase2 = await response2.Content.ReadFromJsonAsync<PurchaseDto>(ct);

        // Assert
        purchase1!.Id.Should().Be(purchase2!.Id);
        response2.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreatePurchase_WithAlreadyPurchasedSkin_ReturnsConflict()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var user = await CreateTestUserAsync(cancellationToken: ct);
        var token = await GetAuthTokenAsync(user.UserName!, "Test123!", ct);
        var skin = await CreateTestSkinAsync(isAvailable: true, cancellationToken: ct);
        
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var purchaseDto = new PurchaseCreateDto { SkinId = skin.Id };
        var idempotencyKey1 = Guid.NewGuid().ToString();
        Client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey1);

        // Первая покупка
        await Client.PostAsJsonAsync("/api/purchases", purchaseDto, ct);

        // Вторая покупка того же скина
        var idempotencyKey2 = Guid.NewGuid().ToString();
        Client.DefaultRequestHeaders.Remove("Idempotency-Key");
        Client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey2);

        // Act
        var response = await Client.PostAsJsonAsync("/api/purchases", purchaseDto, ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreatePurchase_WithNonExistentSkin_ReturnsNotFound()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var user = await CreateTestUserAsync(cancellationToken: ct);
        var token = await GetAuthTokenAsync(user.UserName!, "Test123!", ct);
        
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var purchaseDto = new PurchaseCreateDto { SkinId = Guid.NewGuid() };
        var idempotencyKey = Guid.NewGuid().ToString();
        Client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);

        // Act
        var response = await Client.PostAsJsonAsync("/api/purchases", purchaseDto, ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPurchases_WithAuthenticatedUser_ReturnsPurchases()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var user = await CreateTestUserAsync(cancellationToken: ct);
        var token = await GetAuthTokenAsync(user.UserName!, "Test123!", ct);
        var skin = await CreateTestSkinAsync(isAvailable: true, cancellationToken: ct);
        
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Создаем покупку
        var purchaseDto = new PurchaseCreateDto { SkinId = skin.Id };
        var idempotencyKey = Guid.NewGuid().ToString();
        Client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);
        await Client.PostAsJsonAsync("/api/purchases", purchaseDto, ct);

        // Act
        var response = await Client.GetAsync("/api/purchases", ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var purchases = await response.Content.ReadFromJsonAsync<List<PurchaseDto>>(ct);
        purchases.Should().NotBeNull();
        purchases!.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetPurchases_WithFilters_ReturnsFilteredPurchases()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var user = await CreateTestUserAsync(cancellationToken: ct);
        var token = await GetAuthTokenAsync(user.UserName!, "Test123!", ct);
        var skin1 = await CreateTestSkinAsync(name: "Skin 1", isAvailable: true, cancellationToken: ct);
        var skin2 = await CreateTestSkinAsync(name: "Skin 2", isAvailable: true, cancellationToken: ct);
        
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Создаем покупки
        var idempotencyKey1 = Guid.NewGuid().ToString();
        Client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey1);
        await Client.PostAsJsonAsync("/api/purchases", new PurchaseCreateDto { SkinId = skin1.Id }, ct);

        var idempotencyKey2 = Guid.NewGuid().ToString();
        Client.DefaultRequestHeaders.Remove("Idempotency-Key");
        Client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey2);
        await Client.PostAsJsonAsync("/api/purchases", new PurchaseCreateDto { SkinId = skin2.Id }, ct);

        // Act - фильтр по skinId
        var response = await Client.GetAsync($"/api/purchases?skinId={skin1.Id}", ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var purchases = await response.Content.ReadFromJsonAsync<List<PurchaseDto>>(ct);
        purchases.Should().NotBeNull();
        purchases!.All(p => p.SkinId == skin1.Id).Should().BeTrue();
    }
}
