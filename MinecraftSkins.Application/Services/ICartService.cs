using MinecraftSkins.Application.Dtos;

namespace MinecraftSkins.Application.Services;

public interface ICartService
{
    Task<CartDto> GetCartAsync(string buyerId, CancellationToken cancellationToken = default);
    Task<CartDto> AddItemAsync(string buyerId, AddCartItemDto dto, CancellationToken cancellationToken = default);
    Task<CartDto> RemoveItemAsync(string buyerId, Guid cartItemId, CancellationToken cancellationToken = default);
    Task ClearAsync(string buyerId, CancellationToken cancellationToken = default);
}
