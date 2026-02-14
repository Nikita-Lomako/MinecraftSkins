using System.Threading;
using System.Threading.Tasks;
using MinecraftSkins.Application.Dtos;

namespace MinecraftSkins.Application.Services;

public interface IPurchaseService
{
    Task<PurchaseDto> PurchaseSkinAsync(Guid skinId, string buyerId, CancellationToken cancellationToken = default);
    Task<List<PurchaseDto>> GetPurchasesAsync(string? buyerId, Guid? skinId, DateTime? from, DateTime? to, int skip, int take, CancellationToken cancellationToken = default);
    Task<PurchaseDto?> GetPurchaseByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

