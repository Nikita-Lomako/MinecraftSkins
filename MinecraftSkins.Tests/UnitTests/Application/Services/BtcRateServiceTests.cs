using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using MinecraftSkins.Application.Services;
using MinecraftSkins.Domain.Interfaces;
using MinecraftSkins.Domain.Models;
using MinecraftSkins.Tests.Mocks;
using MinecraftSkins.Tests.Mocks.Helpers;

namespace MinecraftSkins.Tests.UnitTests.Application.Services;

public class BtcRateServiceTests
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly IBtcRateProvider _btcRateProvider;
    private readonly ILogger<BtcRateService> _logger;
    private readonly BtcRateService _service;
    private readonly Fixture _fixture;

    public BtcRateServiceTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _distributedCache = Substitute.For<IDistributedCache>();
        _btcRateProvider = Substitute.For<IBtcRateProvider>();
        _logger = Substitute.For<ILogger<BtcRateService>>();
        _service = new BtcRateService(_btcRateProvider, _memoryCache, _distributedCache, _logger);
        _fixture = new Fixture();
    }

    [Fact]
    public async Task GetBtcUsdRateAsync_ReturnsFromMemoryCache_WhenAvailable()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var cachedRate = TestDataFactory.CreateBtcRateResult(70000m, "Memory");
        _memoryCache.Set("btc_usd_rate", cachedRate, TimeSpan.FromSeconds(20));

        // Act
        var result = await _service.GetBtcUsdRateAsync(ct);

        // Assert
        result.Rate.Should().Be(70000m);
        result.Source.Should().Be("Cache (Memory)");
        await _btcRateProvider.DidNotReceive().GetBtcUsdRateAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetBtcUsdRateAsync_ReturnsFromRedisCache_WhenMemoryCacheEmpty()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var redisRate = TestDataFactory.CreateBtcRateResult(71000m, "Redis");
        var redisJson = JsonSerializer.Serialize(redisRate);
        _distributedCache.GetAsync("btc_usd_rate", Arg.Any<CancellationToken>())
            .Returns(Encoding.UTF8.GetBytes(redisJson));

        // Act
        var result = await _service.GetBtcUsdRateAsync(ct);

        // Assert
        result.Rate.Should().Be(71000m);
        result.Source.Should().Be("Cache (Redis)");
        await _btcRateProvider.DidNotReceive().GetBtcUsdRateAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetBtcUsdRateAsync_ReturnsFromProvider_WhenCacheEmpty()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var providerRate = TestDataFactory.CreateBtcRateResult(72000m, "External");
        _btcRateProvider.GetBtcUsdRateAsync(Arg.Any<CancellationToken>())
            .Returns(providerRate);
        _distributedCache.GetAsync("btc_usd_rate", Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        // Act
        var result = await _service.GetBtcUsdRateAsync(ct);

        // Assert
        result.Rate.Should().Be(72000m);
        result.Source.Should().Be("External");
        await _btcRateProvider.Received(1).GetBtcUsdRateAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetBtcUsdRateAsync_ReturnsFallback_WhenProviderFails()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        var fallbackRate = TestDataFactory.CreateBtcRateResult(70000m, "Fallback");
        fallbackRate.AsOfUtc = DateTime.UtcNow.AddMinutes(-5); // В пределах 10 минут
        
        // Создаем новый сервис с предустановленным fallback
        var serviceWithFallback = new BtcRateService(_btcRateProvider, _memoryCache, _distributedCache, _logger);
        
        // Сначала успешный запрос для установки fallback
        _btcRateProvider.GetBtcUsdRateAsync(Arg.Any<CancellationToken>())
            .Returns(fallbackRate);
        _distributedCache.GetAsync("btc_usd_rate", Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);
        await serviceWithFallback.GetBtcUsdRateAsync(ct);
        _memoryCache.Remove("btc_usd_rate");
        
        // Теперь провайдер падает
        _btcRateProvider.ClearSubstitute();
        _btcRateProvider.GetBtcUsdRateAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Provider failed"));
        _distributedCache.GetAsync("btc_usd_rate", Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        // Act
        var result = await serviceWithFallback.GetBtcUsdRateAsync(ct);

        // Assert
        result.Rate.Should().Be(70000m);
        result.Source.Should().Be("Fallback");
    }

    [Fact]
    public async Task GetBtcUsdRateAsync_ThrowsException_WhenProviderFailsAndNoFallback()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        _btcRateProvider.GetBtcUsdRateAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Provider failed"));
        _distributedCache.GetAsync("btc_usd_rate", Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        // Act & Assert
        var act = () => _service.GetBtcUsdRateAsync(ct);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetBtcUsdRateAsync_HandlesRedisFailure_Gracefully()
    {
        var ct = TestContext.Current.CancellationToken;

        // Arrange
        _distributedCache.GetAsync("btc_usd_rate", Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Redis unavailable"));
        var providerRate = TestDataFactory.CreateBtcRateResult(72000m, "External");
        _btcRateProvider.GetBtcUsdRateAsync(Arg.Any<CancellationToken>())
            .Returns(providerRate);

        // Act
        var result = await _service.GetBtcUsdRateAsync(ct);

        // Assert
        result.Rate.Should().Be(72000m);
        result.Source.Should().Be("External");
    }
}
