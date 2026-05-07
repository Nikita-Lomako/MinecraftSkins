using Microsoft.EntityFrameworkCore;
using MinecraftSkins.Domain.IRepositories;
using MinecraftSkins.Domain.Models;
using MinecraftSkins.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MinecraftSkins.Infrastructure.Repositories;

public class CartRepository : ICartRepository
{
    private readonly AppDbContext _db;

    public CartRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Cart> GetOrCreateByBuyerIdAsync(string buyerId, CancellationToken cancellationToken = default)
    {
        var cart = await _db.Carts
            .AsNoTracking()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.BuyerId == buyerId, cancellationToken);

        if (cart != null)
            return cart;

        cart = new Cart { Id = Guid.NewGuid(), BuyerId = buyerId };
        _db.Carts.Add(cart);
        await _db.SaveChangesAsync(cancellationToken);

        // Возвращаем без Items (их нет)
        cart.Items = new List<CartItem>();
        return cart;
    }

    public async Task<Cart> GetByBuyerIdAsync(string buyerId, CancellationToken cancellationToken = default)
    {
        var cart = await _db.Carts
            .AsNoTracking()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.BuyerId == buyerId, cancellationToken);

        if (cart != null)
            return cart;

        // Создаём новую корзину
        cart = new Cart { Id = Guid.NewGuid(), BuyerId = buyerId, Items = new List<CartItem>() };
        _db.Carts.Add(cart);
        await _db.SaveChangesAsync(cancellationToken);
        return cart;
    }

    public async Task AddOrIncrementItemAsync(string buyerId, Guid skinId, int quantity, CancellationToken cancellationToken = default)
    {
        // Проверяем существование скина напрямую (без загрузки сущности)
        var skinExists = await _db.Skins
            .IgnoreQueryFilters()
            .AnyAsync(s => s.Id == skinId, cancellationToken);

        if (!skinExists)
            throw new KeyNotFoundException("Skin not found");

        // Получаем цену скина через AsNoTracking().Select() (чистое значение)
        var price = await _db.Skins
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => s.Id == skinId)
            .Select(s => (decimal?)s.BasePriceUsd)
            .FirstOrDefaultAsync(cancellationToken) ?? 0m;

        // Находим Id корзины пользователя (без Include)
        var cartId = await _db.Carts
            .Where(c => c.BuyerId == buyerId)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (cartId == null)
        {
            cartId = Guid.NewGuid();
            // Сырой SQL – полностью минует отслеживание и фильтры
            await _db.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Carts\" (\"Id\", \"BuyerId\") VALUES ({0}, {1})",
                cartId.Value, buyerId);
        }

        // Проверяем, есть ли уже такой товар в корзине
        var existing = await _db.CartItems
            .Where(ci => ci.CartId == cartId.Value && ci.SkinId == skinId)
            .Select(ci => new { ci.Id, ci.Quantity })
            .FirstOrDefaultAsync(cancellationToken);

        if (existing != null)
        {
            // Обновляем количество
            await _db.Database.ExecuteSqlRawAsync(
                "UPDATE \"CartItems\" SET \"Quantity\" = \"Quantity\" + {0} WHERE \"Id\" = {1}",
                quantity, existing.Id);
        }
        else
        {
            // Вставляем новый элемент
            var newItemId = Guid.NewGuid();
            await _db.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"CartItems\" (\"Id\", \"CartId\", \"SkinId\", \"Quantity\", \"UnitPriceUsd\") VALUES ({0}, {1}, {2}, {3}, {4})",
                newItemId, cartId.Value, skinId, quantity, price);
        }
    }

    public async Task RemoveItemAsync(string buyerId, Guid cartItemId, CancellationToken cancellationToken = default)
    {
        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.BuyerId == buyerId, cancellationToken);

        if (cart == null) return;

        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);
        if (item != null)
        {
            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ClearAsync(string buyerId, CancellationToken cancellationToken = default)
    {
        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.BuyerId == buyerId, cancellationToken);

        if (cart != null && cart.Items.Any())
        {
            _db.CartItems.RemoveRange(cart.Items);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}