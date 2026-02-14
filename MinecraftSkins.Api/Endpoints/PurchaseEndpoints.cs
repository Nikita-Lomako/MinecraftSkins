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
            .Produces(409);

        group.MapGet("/", GetAllPurchases)
            .WithName("GetAllPurchases")
            .Produces<List<PurchaseDto>>(200);

        group.MapGet("/{id}", GetPurchaseById)
            .WithName("GetPurchaseById")
            .Produces<PurchaseDto>(200)
            .Produces(404);
    }

    private static async Task<IResult> CreatePurchase(
        [FromServices] IPurchaseService purchaseService,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromBody] PurchaseCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Мок-авторизация через X-User-Id header
            var buyerId = httpContextAccessor.HttpContext?.Request.Headers["X-User-Id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(buyerId))
            {
                return Results.BadRequest(new { Error = "X-User-Id header is required" });
            }

            var purchase = await purchaseService.PurchaseSkinAsync(dto.SkinId, buyerId, cancellationToken);
            return Results.Created($"/api/purchases/{purchase.Id}", purchase);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { Error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { Error = ex.Message }); // 409 Conflict
        }
        catch (OperationCanceledException)
        {
            return Results.StatusCode(499);
        }
        catch (Exception)
        {
            return Results.Problem("An error occurred while creating the purchase", statusCode: 500);
        }
    }

    private static async Task<IResult> GetAllPurchases(
        [FromServices] IPurchaseService purchaseService,
        [FromQuery] string? buyerId,
        [FromQuery] Guid? skinId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var purchases = await purchaseService.GetPurchasesAsync(buyerId, skinId, from, to, skip, take, cancellationToken);
            return Results.Ok(purchases);
        }
        catch (OperationCanceledException)
        {
            return Results.StatusCode(499);
        }
        catch (Exception)
        {
            return Results.Problem("An error occurred while retrieving purchases", statusCode: 500);
        }
    }

    private static async Task<IResult> GetPurchaseById(
        Guid id,
        [FromServices] IPurchaseService purchaseService,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var purchase = await purchaseService.GetPurchaseByIdAsync(id, cancellationToken);
            if (purchase == null)
                return Results.NotFound();
            return Results.Ok(purchase);
        }
        catch (OperationCanceledException)
        {
            return Results.StatusCode(499);
        }
        catch (Exception)
        {
            return Results.Problem("An error occurred while retrieving the purchase", statusCode: 500);
        }
    }
}

