using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MinecraftSkins.Api.HealthChecks;

public class RedisHealthCheck : IHealthCheck
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<RedisHealthCheck> _logger;

    public RedisHealthCheck(
        IDistributedCache distributedCache,
        ILogger<RedisHealthCheck> logger)
    {
        _distributedCache = distributedCache;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            const string testKey = "health_check_test";
            const string testValue = "test";
            
            // Try to set a test value
            await _distributedCache.SetStringAsync(
                testKey,
                testValue,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10) },
                cancellationToken);
            
            // Try to get the test value
            var retrievedValue = await _distributedCache.GetStringAsync(testKey, cancellationToken);
            
            // Clean up
            await _distributedCache.RemoveAsync(testKey, cancellationToken);
            
            if (retrievedValue == testValue)
            {
                return HealthCheckResult.Healthy("Redis is healthy and responding correctly");
            }
            
            return HealthCheckResult.Unhealthy("Redis returned unexpected value");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return HealthCheckResult.Unhealthy(
                "Redis is unavailable or not responding",
                ex);
        }
    }
}

