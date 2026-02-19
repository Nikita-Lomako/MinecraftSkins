using System.Threading;
using System.Threading.Tasks;
using MinecraftSkins.Domain.Models;

namespace MinecraftSkins.Domain.IRepositories;

public interface ISkinRepository
{
    Task<ICollection<Skin>> GetAllAsync(bool? availableOnly, string? search, int skip, int take, CancellationToken cancellationToken = default);
    Task<Skin?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Skin?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);
    Task CreateAsync(Skin skin, CancellationToken cancellationToken = default);
    Task UpdateAsync(Skin skin, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

