using System.Threading;
using System.Threading.Tasks;
using MinecraftSkins.Domain.Models;

namespace MinecraftSkins.Domain.IRepositories;

public interface IPurchaseRepository
{
    Task<ICollection<Purchase>> GetAllAsync(string? buyerId, Guid? skinId, DateTime? from, DateTime? to, int skip, int take, CancellationToken cancellationToken = default);
    Task<Purchase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task CreateAsync(Purchase purchase, CancellationToken cancellationToken = default);
    Task<ICollection<Purchase>> GetByBuyerIdAsync(string buyerId, CancellationToken cancellationToken = default);
}

