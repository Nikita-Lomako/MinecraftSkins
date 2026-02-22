using System.Threading.RateLimiting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace MinecraftSkins.Api.Configuration;

/// <summary>
/// Centralized configuration for Polly resilience policies.
/// This allows easy maintenance and extension of resilience patterns.
/// Configuration belongs in API layer as it's application-specific setup.
/// 
/// Note: Uses Microsoft.Extensions.Http.Resilience (Polly v8) which provides
/// AddStandardResilienceHandler with built-in retry, circuit breaker, timeout, and rate limiting.
/// </summary>
public static class PollyResilienceConfiguration
{
    /// <summary>
    /// Configures standard resilience handler with all patterns:
    /// - Retry with exponential backoff and jitter
    /// - Circuit Breaker
    /// - Timeout per attempt
    /// - Rate Limiter (configured)
    /// - Logging via Serilog through telemetry
    /// </summary>
    public static void ConfigureStandardResilienceHandler(
        Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions options,
        Microsoft.Extensions.Logging.ILogger? logger = null)
    {
        // ATTEMPT TIMEOUT - реализация паттерна TIMEOUT
        // Ограничивает время выполнения ОДНОЙ попытки запроса (для CoinGecko и др. внешних API)
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);
        
        // RETRY POLICY - реализация паттерна RETRY с экспоненциальной задержкой
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.Delay = TimeSpan.FromSeconds(1);
        // BackoffType is set to Exponential by default in HttpStandardResilienceOptions
        // UseJitter is enabled by default
        
        // CIRCUIT BREAKER - реализация паттерна CIRCUIT BREAKER
        // SamplingDuration должен быть минимум в 2 раза больше AttemptTimeout (15s → 30s)
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
        options.CircuitBreaker.FailureRatio = 0.5; // 50% ошибок -> размыкание цепи
        options.CircuitBreaker.MinimumThroughput = 2; // Минимум 2 запроса для анализа
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(5); // Время "отдыха"
        
        // RATE LIMITER настраивается в ServiceCollectionExtensions через AddResilienceHandler
        // с кастомным TokenBucketRateLimiter (150 запросов в минуту вместо дефолтных 1000/сек)
       
    }
}

