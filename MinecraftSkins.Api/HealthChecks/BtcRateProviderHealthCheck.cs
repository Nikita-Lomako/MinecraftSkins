using Microsoft.Extensions.Diagnostics.HealthChecks;
using MinecraftSkins.Domain.Interfaces;

namespace MinecraftSkins.Api.HealthChecks;

public class BtcRateProviderHealthCheck : IHealthCheck
{
    private readonly IBtcRateProvider _btcRateProvider;
    private readonly ILogger<BtcRateProviderHealthCheck> _logger;

    public BtcRateProviderHealthCheck(
        IBtcRateProvider btcRateProvider,
        ILogger<BtcRateProviderHealthCheck> logger)
    {
        _btcRateProvider = btcRateProvider;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _btcRateProvider.GetBtcUsdRateAsync(cancellationToken);
            
            if (result.Rate > 0)
            {
                return HealthCheckResult.Healthy(
                    $"BTC Rate Provider is healthy. Current rate: {result.Rate:C} (Source: {result.Source})");
            }
            
            return HealthCheckResult.Unhealthy("BTC Rate Provider returned invalid rate");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BTC Rate Provider health check failed");
            return HealthCheckResult.Unhealthy(
                "BTC Rate Provider is unavailable",
                ex);
        }
    }
}

