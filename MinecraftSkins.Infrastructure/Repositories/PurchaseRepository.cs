using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MinecraftSkins.Domain.IRepositories;
using MinecraftSkins.Domain.Models;
using MinecraftSkins.Infrastructure.Data;

namespace MinecraftSkins.Infrastructure.Repositories;

public class PurchaseRepository : IPurchaseRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<PurchaseRepository> _logger;

    public PurchaseRepository(AppDbContext db, ILogger<PurchaseRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ICollection<Purchase>> GetAllAsync(string? buyerId, string? buyerUserName, Guid? skinId, DateTime? from, DateTime? to, int skip, int take, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogDebug("Getting purchases with buyerId={BuyerId}, buyerUserName={BuyerUserName}, skinId={SkinId}, from={From}, to={To}, skip={Skip}, take={Take}",
            buyerId, buyerUserName, skinId, from, to, skip, take);

        var query = _db.Purchases
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(buyerId))
        {
            query = query.Where(p => p.BuyerId == buyerId);
        }

        if (!string.IsNullOrWhiteSpace(buyerUserName))
        {
            var normalized = buyerUserName.Trim().ToUpperInvariant();
            query = query.Where(p => _db.Users.Any(u =>
                u.Id == p.BuyerId &&
                u.NormalizedUserName != null &&
                u.NormalizedUserName.Contains(normalized)));
        }


        if (skinId.HasValue)
        {
            query = query.Where(p => p.SkinId == skinId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(p => p.PurchasedAtUtc >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(p => p.PurchasedAtUtc <= to.Value);
        }

        var purchases = await query
            .OrderByDescending(p => p.PurchasedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        // Load Skin navigation with IgnoreQueryFilters to include soft-deleted skins
        // Users who purchased a skin should be able to view its details even if it was deleted
        var skinIds = purchases.Select(p => p.SkinId).Distinct().ToList();
        if (skinIds.Any())
        {
            var skins = await _db.Skins
                .IgnoreQueryFilters()
                .Where(s => skinIds.Contains(s.Id))
                .ToListAsync(cancellationToken);
            
            var skinDict = skins.ToDictionary(s => s.Id);
            
            // Manually assign Skin navigation property
            foreach (var purchase in purchases)
            {
                if (skinDict.TryGetValue(purchase.SkinId, out var skin))
                {
                    purchase.Skin = skin;
                }
            }
        }

        _logger.LogDebug("Retrieved {Count} purchases", purchases.Count);
        return purchases;
    }

    public async Task<Purchase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogDebug("Getting purchase {Id}", id);

        var purchase = await _db.Purchases
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (purchase != null)
        {
            // Load Skin navigation with IgnoreQueryFilters to include soft-deleted skins
            // Users who purchased a skin should be able to view its details even if it was deleted
            var skin = await _db.Skins
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == purchase.SkinId, cancellationToken);
            
            if (skin != null)
            {
                purchase.Skin = skin;
            }
            
            _logger.LogDebug("Found purchase {Id}", id);
        }
        else
        {
            _logger.LogDebug("Purchase {Id} not found", id);
        }

        return purchase;
    }

    public async Task CreateAsync(Purchase purchase, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogDebug("Creating purchase {Id}", purchase.Id);

        // Use transaction to ensure atomicity and check skin state with optimistic concurrency
        using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Re-check skin state in transaction to detect concurrent modifications
            var skin = await _db.Skins
                .FirstOrDefaultAsync(s => s.Id == purchase.SkinId, cancellationToken);

            if (skin == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new KeyNotFoundException($"Skin with id {purchase.SkinId} not found");
            }

            if (skin.IsDeleted)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new InvalidOperationException($"Skin with id {purchase.SkinId} was deleted and is no longer available");
            }

            if (!skin.IsAvailable)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new InvalidOperationException($"Skin with id {purchase.SkinId} is not available for purchase");
            }

            // If we reach here, skin is valid - create purchase
            // Note: We don't modify Skin, so DbUpdateConcurrencyException won't occur here
            // But if Skin was modified (e.g., IsAvailable changed), we already checked above
            await _db.Purchases.AddAsync(purchase, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogDebug("Successfully created purchase {Id}", purchase.Id);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("IX_Purchases_BuyerId_SkinId") == true || 
                                            ex.InnerException?.Message?.Contains("duplicate key") == true ||
                                            ex.InnerException?.Message?.Contains("unique constraint") == true)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogWarning(ex, "Duplicate purchase attempt for buyer {BuyerId} and skin {SkinId}", purchase.BuyerId, purchase.SkinId);
            throw new InvalidOperationException("You have already purchased this skin");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogWarning(ex, "Concurrency conflict when creating purchase {Id}", purchase.Id);
            throw;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error creating purchase {Id}. Transaction rolled back.", purchase.Id);
            throw; // Пробрасываем для обработки в GlobalExceptionHandler
        }
    }

    public async Task<ICollection<Purchase>> GetByBuyerIdAsync(string buyerId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogDebug("Getting purchases for buyer {BuyerId}", buyerId);

        var purchases = await _db.Purchases
            .AsNoTracking()
            .Where(p => p.BuyerId == buyerId)
            .OrderByDescending(p => p.PurchasedAtUtc)
            .ToListAsync(cancellationToken);

        // Load Skin navigation with IgnoreQueryFilters to include soft-deleted skins
        var skinIds = purchases.Select(p => p.SkinId).Distinct().ToList();
        if (skinIds.Any())
        {
            var skins = await _db.Skins
                .IgnoreQueryFilters()
                .Where(s => skinIds.Contains(s.Id))
                .ToListAsync(cancellationToken);
            
            var skinDict = skins.ToDictionary(s => s.Id);
            
            foreach (var purchase in purchases)
            {
                if (skinDict.TryGetValue(purchase.SkinId, out var skin))
                {
                    purchase.Skin = skin;
                }
            }
        }

        _logger.LogDebug("Retrieved {Count} purchases for buyer {BuyerId}", purchases.Count, buyerId);
        return purchases;
    }
}
