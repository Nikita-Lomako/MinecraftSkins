using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MinecraftSkins.Domain.IRepositories;
using MinecraftSkins.Domain.Models;
using MinecraftSkins.Infrastructure.Data;

namespace MinecraftSkins.Infrastructure.Repositories;

public class SkinRepository : ISkinRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<SkinRepository> _logger;

    public SkinRepository(AppDbContext db, ILogger<SkinRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ICollection<Skin>> GetAllAsync(bool? availableOnly, string? search, int skip, int take, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogDebug("Getting skins with availableOnly={AvailableOnly}, search={Search}, skip={Skip}, take={Take}",
            availableOnly, search, skip, take);

        var query = _db.Skins.AsNoTracking().AsQueryable();

        if (availableOnly == true)
        {
            query = query.Where(s => s.IsAvailable);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => s.Name.ToLower().Contains(search.ToLower()));
        }

        var result = await query
            .OrderBy(s => s.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Retrieved {Count} skins", result.Count);
        return result;
    }

    public async Task<Skin?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogDebug("Getting skin {Id}", id);

        var result = await _db.Skins
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (result != null)
        {
            _logger.LogDebug("Found skin {Id}", id);
        }
        else
        {
            _logger.LogDebug("Skin {Id} not found", id);
        }

        return result;
    }

    public async Task CreateAsync(Skin skin, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogDebug("Creating skin {Id}", skin.Id);

        await _db.Skins.AddAsync(skin, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Successfully created skin {Id}", skin.Id);
    }

    public async Task UpdateAsync(Skin skin, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogDebug("Updating skin {Id}", skin.Id);

        _db.Skins.Update(skin);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Successfully updated skin {Id}", skin.Id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogDebug("Deleting skin {Id}", id);

        var skin = await _db.Skins.FindAsync(new object[] { id }, cancellationToken);
        if (skin != null)
        {
            // Soft delete через SaveChangesAsync в AppDbContext
            _db.Skins.Remove(skin);
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Successfully deleted skin {Id}", id);
        }
        else
        {
            _logger.LogWarning("Skin {Id} not found for deletion", id);
        }
    }
}

