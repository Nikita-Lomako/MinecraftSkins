using MinecraftSkins.Application.Dtos;
using MinecraftSkins.Domain.IRepositories;

namespace MinecraftSkins.Application.Services;

public class CartService : ICartService
{
    private readonly ICartRepository _cartRepository;
    private readonly ISkinRepository _skinRepository;

    public CartService(ICartRepository cartRepository, ISkinRepository skinRepository)
    {
        _cartRepository = cartRepository;
        _skinRepository = skinRepository;
    }

    public async Task<CartDto> GetCartAsync(string buyerId, CancellationToken cancellationToken = default)
    {
        var cart = await _cartRepository.GetByBuyerIdAsync(buyerId, cancellationToken);
        return await MapAsync(cart, cancellationToken);
    }

    public async Task<CartDto> AddItemAsync(string buyerId, AddCartItemDto dto, CancellationToken cancellationToken = default)
    {
        await _cartRepository.AddOrIncrementItemAsync(buyerId, dto.SkinId, Math.Max(1, dto.Quantity), cancellationToken);
        var cart = await _cartRepository.GetByBuyerIdAsync(buyerId, cancellationToken);
        return await MapAsync(cart, cancellationToken);
    }

    public async Task<CartDto> RemoveItemAsync(string buyerId, Guid cartItemId, CancellationToken cancellationToken = default)
    {
        await _cartRepository.RemoveItemAsync(buyerId, cartItemId, cancellationToken);
        var cart = await _cartRepository.GetByBuyerIdAsync(buyerId, cancellationToken);
        return await MapAsync(cart, cancellationToken);
    }

    public Task ClearAsync(string buyerId, CancellationToken cancellationToken = default) =>
        _cartRepository.ClearAsync(buyerId, cancellationToken);

    private async Task<CartDto> MapAsync(Domain.Models.Cart cart, CancellationToken ct)
    {
        // собираем ID скинов из всех элементов
        var skinIds = cart.Items.Select(i => i.SkinId).Distinct().ToList();

        // загружаем имена скинов (даже удалённых)
        var skinNames = new Dictionary<Guid, string>();
        foreach (var id in skinIds)
        {
            var skin = await _skinRepository.GetByIdIncludingDeletedAsync(id, ct);
            skinNames[id] = skin?.Name ?? "Unknown skin";
        }

        var items = cart.Items.Select(i => new CartItemDto
        {
            Id = i.Id,
            SkinId = i.SkinId,
            SkinName = skinNames.TryGetValue(i.SkinId, out var name) ? name : "Unknown skin",
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