using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MinecraftSkins.Application.Dtos;
using MinecraftSkins.Application.Services;
using MinecraftSkins.Domain.IRepositories;
using MinecraftSkins.Domain.Models;
using MinecraftSkins.Tests.Mocks.Helpers;

namespace MinecraftSkins.Tests.UnitTests.Application.Services;

public class PurchaseServiceTests
{
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly ISkinRepository _skinRepository;
    private readonly IBtcRateService _btcRateService;
    private readonly IPriceCalculator _priceCalculator;
    private readonly IMapper _mapper;
    private readonly IValidator<PurchaseCreateDto> _validator;
    private readonly ILogger<PurchaseService> _logger;
    private readonly PurchaseService _service;
    private readonly Fixture _fixture;

    public PurchaseServiceTests()
    {
        _purchaseRepository = Substitute.For<IPurchaseRepository>();
        _skinRepository = Substitute.For<ISkinRepository>();
        _btcRateService = Substitute.For<IBtcRateService>();
        _priceCalculator = Substitute.For<IPriceCalculator>();
        _mapper = CreateMapper();
        _validator = Substitute.For<IValidator<PurchaseCreateDto>>();
        _logger = Substitute.For<ILogger<PurchaseService>>();
        _service = new PurchaseService(
            _purchaseRepository,
            _skinRepository,
            _btcRateService,
            _priceCalculator,
            _mapper,
            _validator,
            _logger);
        _fixture = new Fixture();
    }

    private static IMapper CreateMapper()
    {
        var configExpression = new MapperConfigurationExpression();
        configExpression.AddProfile<MinecraftSkins.Application.MappingConfig>();
        var config = new MapperConfiguration(configExpression, LoggerFactory.Create(_ => { }));
        config.AssertConfigurationIsValid();
        return config.CreateMapper();
    }

    [Fact]
    public async Task PurchaseSkinAsync_WithValidSkin_ReturnsPurchaseDto()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var skinId = Guid.NewGuid();
        var buyerId = "buyer-123";
        var skin = TestDataFactory.CreateSkin(id: skinId, isAvailable: true, basePriceUsd: 10m);
        var btcRate = TestDataFactory.CreateBtcRateResult(70000m);
        var expectedPrice = 10.5m;

        _skinRepository.GetByIdAsync(skinId, Arg.Any<CancellationToken>())
            .Returns(skin);
        _purchaseRepository.GetAllAsync(buyerId, skinId, null, null, 0, 1, Arg.Any<CancellationToken>())
            .Returns(new List<Purchase>());
        _btcRateService.GetBtcUsdRateAsync(Arg.Any<CancellationToken>())
            .Returns(btcRate);
        _priceCalculator.CalculateFinalPrice(skin.BasePriceUsd, btcRate.Rate)
            .Returns(expectedPrice);

        // Act
        var result = await _service.PurchaseSkinAsync(skinId, buyerId, ct);

        // Assert
        result.Should().NotBeNull();
        result.SkinId.Should().Be(skinId);
        result.BuyerId.Should().Be(buyerId);
        result.PriceUsdFinal.Should().Be(expectedPrice);
        result.BtcUsdRate.Should().Be(btcRate.Rate);
        await _purchaseRepository.Received(1).CreateAsync(Arg.Any<Purchase>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PurchaseSkinAsync_WithNonExistentSkin_ThrowsKeyNotFoundException()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var skinId = Guid.NewGuid();
        var buyerId = "buyer-123";

        _skinRepository.GetByIdAsync(skinId, Arg.Any<CancellationToken>())
            .Returns((Skin?)null);

        // Act & Assert
        var act = () => _service.PurchaseSkinAsync(skinId, buyerId, ct);
        await act.Should().ThrowAsync<KeyNotFoundException>();
        await _purchaseRepository.DidNotReceive().CreateAsync(Arg.Any<Purchase>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PurchaseSkinAsync_WithUnavailableSkin_ThrowsInvalidOperationException()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var skinId = Guid.NewGuid();
        var buyerId = "buyer-123";
        var skin = TestDataFactory.CreateSkin(id: skinId, isAvailable: false);

        _skinRepository.GetByIdAsync(skinId, Arg.Any<CancellationToken>())
            .Returns(skin);

        // Act & Assert
        var act = () => _service.PurchaseSkinAsync(skinId, buyerId, ct);
        await act.Should().ThrowAsync<InvalidOperationException>();
        await _purchaseRepository.DidNotReceive().CreateAsync(Arg.Any<Purchase>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PurchaseSkinAsync_WithAlreadyPurchasedSkin_ThrowsInvalidOperationException()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var skinId = Guid.NewGuid();
        var buyerId = "buyer-123";
        var skin = TestDataFactory.CreateSkin(id: skinId, isAvailable: true);
        var existingPurchase = TestDataFactory.CreatePurchase(skinId: skinId, buyerId: buyerId);

        _skinRepository.GetByIdAsync(skinId, Arg.Any<CancellationToken>())
            .Returns(skin);
        _purchaseRepository.GetAllAsync(buyerId, skinId, null, null, 0, 1, Arg.Any<CancellationToken>())
            .Returns(new List<Purchase> { existingPurchase });

        // Act & Assert
        var act = () => _service.PurchaseSkinAsync(skinId, buyerId, ct);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already purchased*");
        await _purchaseRepository.DidNotReceive().CreateAsync(Arg.Any<Purchase>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPurchasesAsync_WithFilters_ReturnsFilteredPurchases()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var buyerId = "buyer-123";
        var skinId = Guid.NewGuid();
        var purchases = TestDataFactory.CreatePurchases(3);
        purchases[0].BuyerId = buyerId;
        purchases[0].SkinId = skinId;

        _purchaseRepository.GetAllAsync(buyerId, skinId, null, null, 0, 10, Arg.Any<CancellationToken>())
            .Returns(purchases.Where(p => p.BuyerId == buyerId && p.SkinId == skinId).ToList());

        // Act
        var result = await _service.GetPurchasesAsync(buyerId, skinId, null, null, 0, 10, ct);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetPurchaseByIdAsync_WithValidId_ReturnsPurchaseDto()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var purchaseId = Guid.NewGuid();
        var purchase = TestDataFactory.CreatePurchase(id: purchaseId);

        _purchaseRepository.GetByIdAsync(purchaseId, Arg.Any<CancellationToken>())
            .Returns(purchase);

        // Act
        var result = await _service.GetPurchaseByIdAsync(purchaseId, ct);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(purchaseId);
    }

    [Fact]
    public async Task GetPurchaseByIdAsync_WithInvalidId_ReturnsNull()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var purchaseId = Guid.NewGuid();

        _purchaseRepository.GetByIdAsync(purchaseId, Arg.Any<CancellationToken>())
            .Returns((Purchase?)null);

        // Act
        var result = await _service.GetPurchaseByIdAsync(purchaseId, ct);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task PurchaseSkinAsync_WithConcurrencyException_RetriesAndThrows()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var skinId = Guid.NewGuid();
        var buyerId = "buyer-123";
        var skin = TestDataFactory.CreateSkin(id: skinId, isAvailable: true);
        var btcRate = TestDataFactory.CreateBtcRateResult(70000m);

        _skinRepository.GetByIdAsync(skinId, Arg.Any<CancellationToken>())
            .Returns(skin);
        _purchaseRepository.GetAllAsync(buyerId, skinId, null, null, 0, 1, Arg.Any<CancellationToken>())
            .Returns(new List<Purchase>());
        _btcRateService.GetBtcUsdRateAsync(Arg.Any<CancellationToken>())
            .Returns(btcRate);
        _priceCalculator.CalculateFinalPrice(Arg.Any<decimal>(), Arg.Any<decimal>())
            .Returns(10.5m);
        _purchaseRepository.CreateAsync(Arg.Any<Purchase>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateConcurrencyException());

        // Act & Assert
        var act = () => _service.PurchaseSkinAsync(skinId, buyerId, ct);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
