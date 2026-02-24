using Bogus;
using MinecraftSkins.Domain.Models;

namespace MinecraftSkins.Tests.Mocks.Helpers;

public static class TestDataFactory
{
    private static DateTime EnsureUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static readonly Faker<Skin> SkinFaker = new Faker<Skin>()
        .RuleFor(s => s.Id, f => f.Random.Guid())
        .RuleFor(s => s.Name, f => f.Commerce.ProductName())
        .RuleFor(s => s.BasePriceUsd, f => f.Random.Decimal(1m, 100m))
        .RuleFor(s => s.IsAvailable, f => f.Random.Bool())
        .RuleFor(s => s.CreatedAtUtc, f => EnsureUtc(f.Date.Past()))
        .RuleFor(
            s => s.UpdatedAtUtc,
            f => f.Random.Bool(0.5f)
                ? EnsureUtc(f.Date.Between(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow))
                : (DateTime?)null)
        .RuleFor(s => s.IsDeleted, _ => false)
        .RuleFor(s => s.DeletedAtUtc, _ => (DateTime?)null)
        .RuleFor(s => s.RowVersion, f => f.Random.Bytes(8));

    private static readonly Faker<Purchase> PurchaseFaker = new Faker<Purchase>()
        .RuleFor(p => p.Id, f => f.Random.Guid())
        .RuleFor(p => p.SkinId, f => f.Random.Guid())
        .RuleFor(p => p.BuyerId, f => f.Random.Guid().ToString())
        .RuleFor(p => p.PriceUsdFinal, f => f.Random.Decimal(1m, 200m))
        .RuleFor(p => p.BtcUsdRate, f => f.Random.Decimal(50000m, 100000m))
        .RuleFor(p => p.PurchasedAtUtc, f => EnsureUtc(f.Date.Past()));

    public static Skin CreateSkin(
        Guid? id = null,
        string? name = null,
        decimal? basePriceUsd = null,
        bool? isAvailable = null,
        bool isDeleted = false)
    {
        var skin = SkinFaker.Generate();
        
        if (id.HasValue) skin.Id = id.Value;
        if (name != null) skin.Name = name;
        if (basePriceUsd.HasValue) skin.BasePriceUsd = basePriceUsd.Value;
        if (isAvailable.HasValue) skin.IsAvailable = isAvailable.Value;
        skin.IsDeleted = isDeleted;
        
        return skin;
    }

    public static List<Skin> CreateSkins(int count)
    {
        return SkinFaker.Generate(count);
    }

    public static Purchase CreatePurchase(
        Guid? id = null,
        Guid? skinId = null,
        string? buyerId = null,
        decimal? priceUsdFinal = null,
        decimal? btcUsdRate = null)
    {
        var purchase = PurchaseFaker.Generate();
        
        if (id.HasValue) purchase.Id = id.Value;
        if (skinId.HasValue) purchase.SkinId = skinId.Value;
        if (buyerId != null) purchase.BuyerId = buyerId;
        if (priceUsdFinal.HasValue) purchase.PriceUsdFinal = priceUsdFinal.Value;
        if (btcUsdRate.HasValue) purchase.BtcUsdRate = btcUsdRate.Value;
        
        return purchase;
    }

    public static List<Purchase> CreatePurchases(int count)
    {
        return PurchaseFaker.Generate(count);
    }

    public static BtcRateResult CreateBtcRateResult(decimal rate = 70000m, string? source = null)
    {
        return new BtcRateResult
        {
            Rate = rate,
            AsOfUtc = DateTime.UtcNow,
            Source = source ?? "Test"
        };
    }
}
