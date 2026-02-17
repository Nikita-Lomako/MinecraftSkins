using System.Threading;
using System.Threading.Tasks;
using MinecraftSkins.Application.Dtos;

namespace MinecraftSkins.Application.Services;

public interface ISkinService
{
    Task<List<SkinDto>> GetAllSkinsAsync(bool? availableOnly, string? search, int skip, int take, CancellationToken cancellationToken = default);
    Task<SkinDto?> GetSkinByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SkinDto> CreateSkinAsync(SkinCreateDto dto, CancellationToken cancellationToken = default);
    Task<SkinDto?> UpdateSkinAsync(Guid id, SkinUpdateDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteSkinAsync(Guid id, CancellationToken cancellationToken = default);
}

