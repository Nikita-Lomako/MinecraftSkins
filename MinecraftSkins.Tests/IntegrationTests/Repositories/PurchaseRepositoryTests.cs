using Microsoft.EntityFrameworkCore;
using MinecraftSkins.Domain.Models;
using MinecraftSkins.Infrastructure.Data;
using MinecraftSkins.Infrastructure.Repositories;
using MinecraftSkins.Tests.IntegrationTests;
using MinecraftSkins.Tests.Mocks.Helpers;

namespace MinecraftSkins.Tests.IntegrationTests.Repositories;

public class PurchaseRepositoryTests : IntegrationTestBase
{
    private readonly PurchaseRepository _repository;

    public PurchaseRepositoryTests(CustomWebApplicationFactory factory) : base(factory)
    {
        var logger = Factory.Services.GetRequiredService<ILogger<PurchaseRepository>>();
        _repository = new PurchaseRepository(DbContext, logger);
    }

    public override async ValueTask InitializeAsync()
    {
        await CleanupAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CreateAsync_WithValidPurchase_CreatesPurchase()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var skin = await CreateTestSkinAsync(isAvailable: true, cancellationToken: ct);
        var purchase = TestDataFactory.CreatePurchase(
            skinId: skin.Id,
            buyerId: "buyer-123",
            priceUsdFinal: 10.50m);

        // Act
        await _repository.CreateAsync(purchase, ct);

        // Assert
        var created = await DbContext.Purchases.FirstOrDefaultAsync(p => p.Id == purchase.Id, ct);
        created.Should().NotBeNull();
        created!.SkinId.Should().Be(skin.Id);
        created.BuyerId.Should().Be("buyer-123");
    }

    [Fact]
    public async Task CreateAsync_WithDuplicatePurchase_ThrowsException()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var skin = await CreateTestSkinAsync(isAvailable: true, cancellationToken: ct);
        var purchase1 = TestDataFactory.CreatePurchase(
            skinId: skin.Id,
            buyerId: "buyer-123");
        var purchase2 = TestDataFactory.CreatePurchase(
            skinId: skin.Id,
            buyerId: "buyer-123");

        await _repository.CreateAsync(purchase1, ct);

        // Act & Assert
        var act = () => _repository.CreateAsync(purchase2, ct);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetAllAsync_WithBuyerIdFilter_ReturnsFilteredPurchases()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var skin1 = await CreateTestSkinAsync(isAvailable: true, cancellationToken: ct);
        var skin2 = await CreateTestSkinAsync(isAvailable: true, cancellationToken: ct);
        
        var purchase1 = TestDataFactory.CreatePurchase(skinId: skin1.Id, buyerId: "buyer-1");
        var purchase2 = TestDataFactory.CreatePurchase(skinId: skin2.Id, buyerId: "buyer-1");
        var purchase3 = TestDataFactory.CreatePurchase(skinId: skin1.Id, buyerId: "buyer-2");

        await _repository.CreateAsync(purchase1, ct);
        await _repository.CreateAsync(purchase2, ct);
        await _repository.CreateAsync(purchase3, ct);

        // Act
        var result = await _repository.GetAllAsync("buyer-1", null, null, null, 0, 10, ct);

        // Assert
        result.Count.Should().Be(2);
        result.All(p => p.BuyerId == "buyer-1").Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsPurchase()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var skin = await CreateTestSkinAsync(isAvailable: true, cancellationToken: ct);
        var purchase = TestDataFactory.CreatePurchase(skinId: skin.Id);
        await _repository.CreateAsync(purchase, ct);

        // Act
        var result = await _repository.GetByIdAsync(purchase.Id, ct);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(purchase.Id);
    }
}

