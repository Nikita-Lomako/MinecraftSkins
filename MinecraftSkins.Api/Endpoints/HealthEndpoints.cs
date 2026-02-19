using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace MinecraftSkins.Api.Endpoints;

/// <summary>
/// Endpoints для health checks системы.
/// Возвращает JSON формат с информацией о состоянии всех компонентов.
/// </summary>
public static class HealthEndpoints
{
    /// <summary>
    /// Настраивает health checks endpoint с JSON форматом ответа.
    /// Endpoint публичный и не требует авторизации.
    /// </summary>
    public static void MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                
                var result = new
                {
                    status = report.Status.ToString(),
                    totalDuration = report.TotalDuration.ToString(),
                    entries = report.Entries.ToDictionary(
                        e => e.Key,
                        e => new
                        {
                            status = e.Value.Status.ToString(),
                            duration = e.Value.Duration.ToString(),
                            description = e.Value.Description
                        }
                    )
                };
                
                await context.Response.WriteAsJsonAsync(result);
            }
        }).AllowAnonymous();
    }
}

