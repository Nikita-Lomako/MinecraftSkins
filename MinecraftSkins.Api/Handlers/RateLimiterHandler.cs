using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.RateLimiting;

namespace MinecraftSkins.Api.Handlers;

/// <summary>
/// DelegatingHandler для применения Rate Limiter к HTTP запросам.
/// Демонстрирует понимание настройки Rate Limiter с изменением дефолтных значений.
/// </summary>
public class RateLimiterHandler : DelegatingHandler
{
    private readonly RateLimiter _rateLimiter;

    public RateLimiterHandler()
    {
        // Настраиваем TokenBucketRateLimiter: 150 запросов в минуту
        // Вместо дефолтных 1000 запросов в секунду из AddStandardResilienceHandler
        var rateLimiterOptions = new TokenBucketRateLimiterOptions
        {
            TokenLimit = 150, // Максимум 150 токенов (вместо дефолтных 1000)
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 10, // Максимум 10 запросов в очереди
            ReplenishmentPeriod = TimeSpan.FromMinutes(1), // Пополнение каждую минуту (вместо секунды)
            TokensPerPeriod = 150, // 150 токенов за период (150 запросов в минуту вместо 1000/сек)
            AutoReplenishment = true
        };
        
        _rateLimiter = new TokenBucketRateLimiter(rateLimiterOptions);
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Ожидаем получения токена от Rate Limiter
        using var lease = await _rateLimiter.AcquireAsync(permitCount: 1, cancellationToken);
        
        if (!lease.IsAcquired)
        {
            // Если токен не получен (rate limit превышен), возвращаем 429 Too Many Requests
            return new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests)
            {
                Content = new StringContent("Rate limit exceeded. Maximum 150 requests per minute.")
            };
        }
        
        // Если токен получен, выполняем запрос
        return await base.SendAsync(request, cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _rateLimiter?.Dispose();
        }
        base.Dispose(disposing);
    }
}

