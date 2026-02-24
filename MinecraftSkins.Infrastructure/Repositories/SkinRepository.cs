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

        // Use tracking to get RowVersion for optimistic concurrency
        var result = await _db.Skins
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

    public async Task<Skin?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogDebug("Getting skin {Id} including soft-deleted", id);

        // Use IgnoreQueryFilters to get skin even if it's soft-deleted
        // This is used for users who purchased a skin and need to view its details
        var result = await _db.Skins
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (result != null)
        {
            _logger.LogDebug("Found skin {Id} (IsDeleted: {IsDeleted})", id, result.IsDeleted);
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
        _logger.LogDebug("Creating skin {Id} with name {Name}", skin.Id, skin.Name);

        // Check for duplicate name (case-insensitive) including soft-deleted skins
        // Use IgnoreQueryFilters() to check ALL skins, including soft-deleted ones
        // This prevents reusing names of soft-deleted skins, as they may be referenced in purchases
        var existingSkin = await _db.Skins
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Name.ToLower() == skin.Name.ToLower(), cancellationToken);
        
        if (existingSkin != null)
        {
            _logger.LogWarning("Skin with name {Name} already exists (Id: {ExistingId}, IsDeleted: {IsDeleted})", 
                skin.Name, existingSkin.Id, existingSkin.IsDeleted);
            throw new InvalidOperationException($"Skin with name '{skin.Name}' already exists");
        }

        await _db.Skins.AddAsync(skin, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Successfully created skin {Id}", skin.Id);
    }

    public async Task UpdateAsync(Skin skin, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogDebug("Updating skin {Id} with name {Name}", skin.Id, skin.Name);

        // Check for duplicate name (excluding current skin) including soft-deleted skins
        // Use IgnoreQueryFilters() to check ALL skins, including soft-deleted ones
        var existingSkin = await _db.Skins
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Name.ToLower() == skin.Name.ToLower() && s.Id != skin.Id, cancellationToken);
        
        if (existingSkin != null)
        {
            _logger.LogWarning("Skin with name {Name} already exists (Id: {ExistingId}, IsDeleted: {IsDeleted})", 
                skin.Name, existingSkin.Id, existingSkin.IsDeleted);
            throw new InvalidOperationException($"Skin with name '{skin.Name}' already exists");
        }

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

