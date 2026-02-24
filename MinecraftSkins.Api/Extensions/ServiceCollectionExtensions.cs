using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MinecraftSkins.Application.Services;
using MinecraftSkins.Application.Interfaces;
using MinecraftSkins.Domain.Interfaces;
using MinecraftSkins.Infrastructure.Services;
using MinecraftSkins.Api.Configuration;

namespace MinecraftSkins.Api.Extensions;

/// <summary>
/// Extension methods for registering BTC rate provider services.
/// This keeps Program.cs clean and focused on high-level configuration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures HttpClient with common settings and Polly resilience policies.
    /// Both Binance and CoinGecko use the same retry/circuit breaker configuration.
    /// </summary>
    private static void ConfigureBtcRateProviderHttpClient<T>(
        this IServiceCollection services,
        string baseAddress) where T : class
    {
        services.AddHttpClient<T>(client =>
        {
            client.BaseAddress = new Uri(baseAddress);
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("User-Agent", "MinecraftSkinsApp/1.0");
        })
        // Добавляем кастомный Rate Limiter Handler: 50 запросов в минуту
        // Демонстрирует понимание настройки Rate Limiter с изменением дефолтных значений
        // (вместо дефолтных 1000 запросов в секунду из AddStandardResilienceHandler)
        .AddHttpMessageHandler<MinecraftSkins.Api.Handlers.RateLimiterHandler>()
        // Добавляем logging handler для логирования всех HTTP запросов через Serilog
        .AddHttpMessageHandler<MinecraftSkins.Api.Handlers.PollyLoggingHandler>()
        // Добавляем остальные Polly resilience policies (retry, circuit breaker, timeout)
        .AddStandardResilienceHandler(options =>
        {
            PollyResilienceConfiguration.ConfigureStandardResilienceHandler(options, null);
        });
    }

    /// <summary>
    /// Registers BTC rate provider service based on configuration:
    /// - Registers only the selected provider (Binance or CoinGecko) with HttpClient and Polly resilience
    /// - BtcRateService with caching and fallback support
    /// </summary>
    public static IServiceCollection AddBtcRateProviders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var btcProviderName = configuration["BtcRateProvider:Provider"] ?? "CoinGecko";
        
        // Register only the selected provider based on configuration
        if (btcProviderName.Equals("Binance", StringComparison.OrdinalIgnoreCase))
        {
            // Register Binance provider with common HttpClient and Polly resilience configuration
            services.ConfigureBtcRateProviderHttpClient<BinanceBtcRateProvider>("https://api.binance.com/");
            
            // Register Binance as IBtcRateProvider
            services.AddScoped<IBtcRateProvider>(sp => sp.GetRequiredService<BinanceBtcRateProvider>());
        }
        else
        {
            // Register CoinGecko provider with common HttpClient and Polly resilience configuration (default)
            services.ConfigureBtcRateProviderHttpClient<CoinGeckoBtcRateProvider>("https://api.coingecko.com/api/v3/");
            
            // Register CoinGecko as IBtcRateProvider
            services.AddScoped<IBtcRateProvider>(sp => sp.GetRequiredService<CoinGeckoBtcRateProvider>());
        }
        
        // Register BtcRateService - DI will automatically inject all dependencies
        services.AddScoped<IBtcRateService, BtcRateService>();
        
        return services;
    }
}

