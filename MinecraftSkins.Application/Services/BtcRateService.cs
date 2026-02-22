using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MinecraftSkins.Application.Interfaces;
using MinecraftSkins.Domain.Interfaces;
using MinecraftSkins.Domain.Models;

namespace MinecraftSkins.Application.Services;

public class BtcRateService : IBtcRateService
{
    private readonly IBtcRateProvider _btcRateProvider;
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<BtcRateService> _logger;

    private const string CacheKey = "btc_usd_rate";
    private static BtcRateResult? _lastSuccessfulRate; // Fallback in memory
    private static readonly TimeSpan MemoryCacheTtl = TimeSpan.FromSeconds(20); // L1 cache TTL
    private static readonly TimeSpan RedisCacheTtl = TimeSpan.FromSeconds(60); // L2 cache TTL — не более 1 минуты для актуального курса
    private static readonly TimeSpan FallbackTtl = TimeSpan.FromMinutes(10);

    public BtcRateService(
        IBtcRateProvider btcRateProvider,
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ILogger<BtcRateService> logger)
    {
        _btcRateProvider = btcRateProvider;
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
    }

    public async Task<BtcRateResult> GetBtcUsdRateAsync(CancellationToken cancellationToken = default)
    {
        // 1. L1 Cache (Memory) - fastest, expires after 60 seconds
        if (_memoryCache.TryGetValue(CacheKey, out BtcRateResult? cachedRate) && cachedRate != null)
        {
            cachedRate.Source = "Cache (Memory)";
            cachedRate.AgeSeconds = (int)(DateTime.UtcNow - cachedRate.AsOfUtc).TotalSeconds;
            return cachedRate;
        }

        // 2. L2 Cache (Redis) — TTL 1 минута, затем повторный запрос к провайдеру
        // Здесь мы ОБЯЗАНЫ использовать try-catch для Redis, так как падение кэша не должно ломать основную логику.
        // Это не "ошибка бизнес-логики", которую ловит GlobalHandler, а "отказ инфраструктуры", который мы должны пережить.
        try
        {
            var redisValue = await _distributedCache.GetStringAsync(CacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(redisValue))
            {
                var redisRate = JsonSerializer.Deserialize<BtcRateResult>(redisValue);
                if (redisRate != null)
                {
                    // Refresh L1 cache from Redis
                    _memoryCache.Set(CacheKey, redisRate, MemoryCacheTtl);
                    redisRate.Source = "Cache (Redis)";
                    redisRate.AgeSeconds = (int)(DateTime.UtcNow - redisRate.AsOfUtc).TotalSeconds;
                    return redisRate;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable, skipping L2 cache check.");
        }

        // 3. External Provider & Fallback Logic
        // Здесь try-catch необходим для реализации FALLBACK паттерна.
        // Если внешний провайдер упал, мы не хотим 500 ошибку, мы хотим попробовать отдать старое значение.
        try
        {
            var freshRate = await _btcRateProvider.GetBtcUsdRateAsync(cancellationToken);
            return await UpdateCachesAndReturn(freshRate, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "External provider failed. Attempting fallback.");

            // 4. Fallback to cached value
            if (_lastSuccessfulRate != null)
            {
                var age = DateTime.UtcNow - _lastSuccessfulRate.AsOfUtc;
                if (age <= FallbackTtl)
                {
                    _logger.LogWarning("Using fallback rate. Age: {Age}", age);
                    return new BtcRateResult
                    {
                        Rate = _lastSuccessfulRate.Rate,
                        AsOfUtc = _lastSuccessfulRate.AsOfUtc,
                        Source = "Fallback",
                        AgeSeconds = (int)age.TotalSeconds
                    };
                }
            }
            
            // Если fallback не помог — выбрасываем исключение, которое будет маппиться в 503 Service Unavailable.
            // Это корректный статус для случая, когда внешний сервис недоступен и нет кэшированных данных.
            throw new InvalidOperationException(
                "BTC rate service is unavailable. External provider failed and no cached data available.", 
                ex); 
        }
    }
    
    private async Task<BtcRateResult> UpdateCachesAndReturn(BtcRateResult freshRate, CancellationToken cancellationToken)
    {
        // Success - update caches
        _memoryCache.Set(CacheKey, freshRate, MemoryCacheTtl);
        _lastSuccessfulRate = freshRate;
        
        // Обновляем Redis с TTL 1 минута
        try
        {
            await _distributedCache.SetStringAsync(
                CacheKey, 
                JsonSerializer.Serialize(freshRate), 
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = RedisCacheTtl },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update Redis cache");
        }

        freshRate.Source = freshRate.Source ?? "External";
        freshRate.AgeSeconds = 0;
        return freshRate;
    }
}
