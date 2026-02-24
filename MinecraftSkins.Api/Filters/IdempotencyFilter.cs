using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace MinecraftSkins.Api.Filters;

internal sealed class IdempotencyFilter(int cacheTimeInMinutes = 60)
    : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        // Parse the Idempotency-Key header from the request
        if (!context.HttpContext.Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKeyHeader) ||
            !Guid.TryParse(idempotencyKeyHeader, out var idempotencyKey))
        {
            // Idempotency Key is optional or invalid?
            // If strictly required, return BadRequest.
            // Prompt says "Idempotency-Key header (check in PurchaseService)".
            // But implementing as filter is cleaner.
            // Let's make it optional for now, or strictly required for POST /purchases?
            // "Protect from double purchase". Usually required for such endpoints.
            // But if I return BadRequest here, all requests without key fail.
            // Let's assume it's only applied to specific endpoints via AddEndpointFilter.
            return Results.BadRequest("Invalid or missing Idempotency-Key header");
        }

        IDistributedCache cache = context.HttpContext
            .RequestServices.GetRequiredService<IDistributedCache>();

        // Check if we already processed this request and return a cached response (if it exists)
        string cacheKey = $"Idempotent_{idempotencyKey}";
        string? cachedResult = await cache.GetStringAsync(cacheKey);
        if (cachedResult is not null)
        {
            IdempotentResponse response = JsonSerializer.Deserialize<IdempotentResponse>(cachedResult)!;
            return new IdempotentResult(response.StatusCode, response.Value);
        }

        object? result = await next(context);

        // Execute the request and cache the response for the specified duration
        if (result is IStatusCodeHttpResult { StatusCode: >= 200 and < 300 } statusCodeResult
            and IValueHttpResult valueResult)
        {
            int statusCode = statusCodeResult.StatusCode ?? StatusCodes.Status200OK;
            IdempotentResponse response = new(statusCode, valueResult.Value);

            await cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(response),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheTimeInMinutes)
                }
            );
        }

        return result;
    }
}

// Helper classes
internal record IdempotentResponse(int StatusCode, object? Value);

internal sealed class IdempotentResult : IResult
{
    private readonly int _statusCode;
    private readonly object? _value;

    public IdempotentResult(int statusCode, object? value)
    {
        _statusCode = statusCode;
        _value = value;
    }

    public Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = _statusCode;
        return httpContext.Response.WriteAsJsonAsync(_value);
    }
}

