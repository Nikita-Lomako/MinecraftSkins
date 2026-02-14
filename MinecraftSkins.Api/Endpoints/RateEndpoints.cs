using System.Threading;
using Microsoft.AspNetCore.Mvc;
using MinecraftSkins.Application.Dtos;

namespace MinecraftSkins.Api.Endpoints;

public static class RateEndpoints
{
    public static void MapRateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/rates").WithTags("Rates");

        group.MapGet("/btc-usd", GetBtcUsdRate)
            .WithName("GetBtcUsdRate")
            .Produces<BtcRateDto>(200)
            .Produces(503);
    }

    private static async Task<IResult> GetBtcUsdRate(
        CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Блок 3 - Реализация через IBtcRateService
            // Пока возвращаем заглушку
            return Results.Ok(new BtcRateDto
            {
                Rate = 68000m,
                AsOfUtc = DateTime.UtcNow,
                Source = "Placeholder",
                AgeSeconds = 0
            });
        }
        catch (OperationCanceledException)
        {
            return Results.StatusCode(499);
        }
        catch (Exception)
        {
            return Results.Problem("An error occurred while retrieving BTC rate", statusCode: 500);
        }
    }
}

