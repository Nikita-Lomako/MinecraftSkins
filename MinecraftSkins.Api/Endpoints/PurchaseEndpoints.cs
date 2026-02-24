using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MinecraftSkins.Application.Dtos;
using MinecraftSkins.Application.Services;

namespace MinecraftSkins.Api.Endpoints;

public static class PurchaseEndpoints
{
    public static void MapPurchaseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/purchases").WithTags("Purchases");

            group.MapPost("/", CreatePurchase)
                .WithName("CreatePurchase")
                .Accepts<PurchaseCreateDto>("application/json")
                .Produces<PurchaseDto>(201)
                .Produces(400)
                .Produces(409)
                .RequireAuthorization()
                .AddEndpointFilter<MinecraftSkins.Api.Filters.IdempotencyFilter>();

        group.MapGet("/", GetAllPurchases)
            .WithName("GetAllPurchases")
            .Produces<List<PurchaseDto>>(200)
            .RequireAuthorization(); // Requires auth to view purchases

        group.MapGet("/{id}", GetPurchaseById)
            .WithName("GetPurchaseById")
            .Produces<PurchaseDto>(200)
            .Produces(404)
            .RequireAuthorization(); // Requires auth to view purchase details
    }

    private static async Task<IResult> CreatePurchase(
        [FromServices] IPurchaseService purchaseService,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromBody] PurchaseCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        // Extract BuyerId from JWT claims
        var user = httpContextAccessor.HttpContext?.User;
        var buyerId = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(buyerId))
        {
            return Results.Unauthorized();
        }

        var purchase = await purchaseService.PurchaseSkinAsync(dto.SkinId, buyerId, cancellationToken);
        return Results.Created($"/api/purchases/{purchase.Id}", purchase);
    }

    private static async Task<IResult> GetAllPurchases(
        [FromServices] IPurchaseService purchaseService,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromQuery] string? buyerId,
        [FromQuery] Guid? skinId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 10,
        CancellationToken cancellationToken = default)
    {
        var user = httpContextAccessor.HttpContext?.User;
        var currentUserId = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(buyerId) && !string.IsNullOrEmpty(currentUserId))
            buyerId = currentUserId;
        var purchases = await purchaseService.GetPurchasesAsync(buyerId, skinId, from, to, skip, take, cancellationToken);
        return Results.Ok(purchases);
    }

    private static async Task<IResult> GetPurchaseById(
        Guid id,
        [FromServices] IPurchaseService purchaseService,
        CancellationToken cancellationToken = default)
    {
        var purchase = await purchaseService.GetPurchaseByIdAsync(id, cancellationToken);
        if (purchase == null)
            return Results.NotFound();
        return Results.Ok(purchase);
    }
}
