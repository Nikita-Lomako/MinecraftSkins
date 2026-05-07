using MinecraftSkins.Domain.Models;

namespace MinecraftSkins.Domain.IRepositories;

public interface ICartRepository
{
    Task<Cart> GetOrCreateByBuyerIdAsync(string buyerId, CancellationToken cancellationToken = default);
    Task<Cart> GetByBuyerIdAsync(string buyerId, CancellationToken cancellationToken = default);
    Task AddOrIncrementItemAsync(string buyerId, Guid skinId, int quantity, CancellationToken cancellationToken = default);
    Task RemoveItemAsync(string buyerId, Guid cartItemId, CancellationToken cancellationToken = default);
    Task ClearAsync(string buyerId, CancellationToken cancellationToken = default);
}
