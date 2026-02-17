using System.Threading;
using Microsoft.AspNetCore.Mvc;
using MinecraftSkins.Application.Dtos;
using MinecraftSkins.Application.Interfaces;

namespace MinecraftSkins.Api.Endpoints;

public static class RateEndpoints
{
    public static void MapRateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/rates").WithTags("Rates");

        group.MapGet("/btc-usd", GetBtcUsdRate)
            .WithName("GetBtcUsdRate")
            .Produces<BtcRateDto>(200)
            .Produces(503)
            .RequireAuthorization()
            .RequireAuthorization(policy => policy.RequireRole("Admin")); // Requires Admin role;
    }

    private static async Task<IResult> GetBtcUsdRate(
        [FromServices] IBtcRateService btcRateService,
        CancellationToken cancellationToken = default)
    {
        var result = await btcRateService.GetBtcUsdRateAsync(cancellationToken);
        
        var dto = new BtcRateDto
        {
            Rate = result.Rate,
            AsOfUtc = result.AsOfUtc,
            Source = result.Source,
            AgeSeconds = result.AgeSeconds ?? 0
        };

        return Results.Ok(dto);
    }
}
