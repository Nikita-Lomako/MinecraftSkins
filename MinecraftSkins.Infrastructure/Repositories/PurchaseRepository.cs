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

    public async Task<ICollection<Purchase>> GetAllAsync(string? buyerId, Guid? skinId, DateTime? from, DateTime? to, int skip, int take, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogDebug("Getting purchases with buyerId={BuyerId}, skinId={SkinId}, from={From}, to={To}, skip={Skip}, take={Take}",
            buyerId, skinId, from, to, skip, take);

        var query = _db.Purchases
            .Include(p => p.Skin)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(buyerId))
        {
            query = query.Where(p => p.BuyerId == buyerId);
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

        var result = await query
            .OrderByDescending(p => p.PurchasedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Retrieved {Count} purchases", result.Count);
        return result;
    }

    public async Task<Purchase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogDebug("Getting purchase {Id}", id);

        var result = await _db.Purchases
            .Include(p => p.Skin)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (result != null)
        {
            _logger.LogDebug("Found purchase {Id}", id);
        }
        else
        {
            _logger.LogDebug("Purchase {Id} not found", id);
        }

        return result;
    }

    public async Task CreateAsync(Purchase purchase, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogDebug("Creating purchase {Id}", purchase.Id);

        await _db.Purchases.AddAsync(purchase, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Successfully created purchase {Id}", purchase.Id);
    }

    public async Task<ICollection<Purchase>> GetByBuyerIdAsync(string buyerId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogDebug("Getting purchases for buyer {BuyerId}", buyerId);

        var result = await _db.Purchases
            .Include(p => p.Skin)
            .AsNoTracking()
            .Where(p => p.BuyerId == buyerId)
            .OrderByDescending(p => p.PurchasedAtUtc)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Retrieved {Count} purchases for buyer {BuyerId}", result.Count, buyerId);
        return result;
    }
}
