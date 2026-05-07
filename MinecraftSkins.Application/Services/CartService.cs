using MinecraftSkins.Application.Dtos;
using MinecraftSkins.Domain.IRepositories;

namespace MinecraftSkins.Application.Services;

public class CartService : ICartService
{
    private readonly ICartRepository _cartRepository;

    public CartService(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<CartDto> GetCartAsync(string buyerId, CancellationToken cancellationToken = default)
    {
        var cart = await _cartRepository.GetByBuyerIdAsync(buyerId, cancellationToken);
        return Map(cart);
    }

    public async Task<CartDto> AddItemAsync(string buyerId, AddCartItemDto dto, CancellationToken cancellationToken = default)
    {
        await _cartRepository.AddOrIncrementItemAsync(buyerId, dto.SkinId, Math.Max(1, dto.Quantity), cancellationToken);
        var cart = await _cartRepository.GetByBuyerIdAsync(buyerId, cancellationToken);
        return Map(cart);
    }

    public async Task<CartDto> RemoveItemAsync(string buyerId, Guid cartItemId, CancellationToken cancellationToken = default)
    {
        await _cartRepository.RemoveItemAsync(buyerId, cartItemId, cancellationToken);
        var cart = await _cartRepository.GetByBuyerIdAsync(buyerId, cancellationToken);
        return Map(cart);
    }

    public Task ClearAsync(string buyerId, CancellationToken cancellationToken = default) =>
        _cartRepository.ClearAsync(buyerId, cancellationToken);

    private static CartDto Map(Domain.Models.Cart cart)
    {
        var items = cart.Items.Select(i => new CartItemDto
        {
            Id = i.Id,
            SkinId = i.SkinId,
            SkinName = i.Skin?.Name ?? "Unknown skin",
            Quantity = i.Quantity,
            UnitPriceUsd = i.UnitPriceUsd,
            TotalPriceUsd = i.UnitPriceUsd * i.Quantity
        }).ToList();

        return new CartDto
        {
            Id = cart.Id,
            BuyerId = cart.BuyerId,
            Items = items,
            TotalPriceUsd = items.Sum(i => i.TotalPriceUsd)
        };
    }
}
