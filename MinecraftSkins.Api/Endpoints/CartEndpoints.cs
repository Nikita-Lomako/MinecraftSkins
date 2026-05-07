using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MinecraftSkins.Application.Dtos;
using MinecraftSkins.Application.Services;

namespace MinecraftSkins.Api.Endpoints;

public static class CartEndpoints
{
    public static void MapCartEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cart").WithTags("Cart").RequireAuthorization();

        group.MapGet("/", GetCart).Produces<CartDto>(200);
        group.MapPost("/items", AddItem).Accepts<AddCartItemDto>("application/json").Produces<CartDto>(200);
        group.MapDelete("/items/{cartItemId:guid}", RemoveItem).Produces<CartDto>(200);
        group.MapDelete("/", ClearCart).Produces(204);
    }

    private static async Task<IResult> GetCart(
        [FromServices] ICartService cartService,
        [FromServices] IHttpContextAccessor contextAccessor,
        CancellationToken cancellationToken = default)
    {
        var userId = contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Results.Unauthorized();
        var cart = await cartService.GetCartAsync(userId, cancellationToken);
        return Results.Ok(cart);
    }

    private static async Task<IResult> AddItem(
    [FromServices] ICartService cartService,
    [FromServices] IHttpContextAccessor contextAccessor,
    [FromBody] AddCartItemDto dto,
    CancellationToken cancellationToken = default)
    {
        var userId = contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Results.Unauthorized();

        try
        {
            var cart = await cartService.AddItemAsync(userId, dto, cancellationToken);
            return Results.Ok(cart);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(ex.Message);
        }
    }

    private static async Task<IResult> RemoveItem(
        Guid cartItemId,
        [FromServices] ICartService cartService,
        [FromServices] IHttpContextAccessor contextAccessor,
        CancellationToken cancellationToken = default)
    {
        var userId = contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Results.Unauthorized();
        var cart = await cartService.RemoveItemAsync(userId, cartItemId, cancellationToken);
        return Results.Ok(cart);
    }

    private static async Task<IResult> ClearCart(
        [FromServices] ICartService cartService,
        [FromServices] IHttpContextAccessor contextAccessor,
        CancellationToken cancellationToken = default)
    {
        var userId = contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Results.Unauthorized();
        await cartService.ClearAsync(userId, cancellationToken);
        return Results.NoContent();
    }
}
