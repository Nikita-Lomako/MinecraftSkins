using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Serilog;

namespace MinecraftSkins.Api.Handlers;

/// <summary>
/// DelegatingHandler для логирования событий Polly через Serilog.
/// Перехватывает запросы и ответы для логирования retry, circuit breaker и других событий.
/// 
/// Расположение: между HttpClient и Polly resilience handler.
/// Логирует все HTTP запросы, включая повторные попытки (retry) и события circuit breaker.
/// </summary>
public class PollyLoggingHandler : DelegatingHandler
{
    private readonly Microsoft.Extensions.Logging.ILogger _logger;
    private static int _requestCounter = 0;

    public PollyLoggingHandler(Microsoft.Extensions.Logging.ILogger<PollyLoggingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var requestUri = request.RequestUri?.ToString() ?? "Unknown";
        var requestId = Interlocked.Increment(ref _requestCounter);
        
        try
        {
            _logger.LogDebug(
                "Polly HTTP Request [{RequestId}]: {Method} {Uri}",
                requestId,
                request.Method,
                requestUri);
            
            var response = await base.SendAsync(request, cancellationToken);
            
            stopwatch.Stop();
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Polly HTTP Success [{RequestId}]: {StatusCode} {Method} {Uri} - {ElapsedMs}ms",
                    requestId,
                    (int)response.StatusCode,
                    request.Method,
                    requestUri,
                    stopwatch.ElapsedMilliseconds);
            }
            else
            {
                // Неуспешный ответ - может быть retry или circuit breaker
                _logger.LogWarning(
                    "Polly HTTP Warning [{RequestId}]: {StatusCode} {Method} {Uri} - {ElapsedMs}ms (may trigger retry)",
                    requestId,
                    (int)response.StatusCode,
                    request.Method,
                    requestUri,
                    stopwatch.ElapsedMilliseconds);
            }
            
            return response;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || ex.CancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            // Timeout может быть из-за retry или circuit breaker
            _logger.LogWarning(
                ex,
                "Polly HTTP Timeout [{RequestId}]: {Method} {Uri} - {ElapsedMs}ms (likely retry attempt or circuit breaker)",
                requestId,
                request.Method,
                requestUri,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            // HttpRequestException часто означает circuit breaker открыт или сетевые проблемы
            _logger.LogError(
                ex,
                "Polly HTTP Error [{RequestId}]: {Method} {Uri} - {ElapsedMs}ms (likely circuit breaker opened or network issue)",
                requestId,
                request.Method,
                requestUri,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Polly HTTP Exception [{RequestId}]: {Method} {Uri} - {ElapsedMs}ms",
                requestId,
                request.Method,
                requestUri,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}

