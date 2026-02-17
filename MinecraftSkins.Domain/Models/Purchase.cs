using MinecraftSkins.Domain.Interfaces;

namespace MinecraftSkins.Domain.Models;

public class Purchase : IEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SkinId { get; set; }
    public decimal PriceUsdFinal { get; set; }
    public decimal BtcUsdRate { get; set; }
    public DateTime PurchasedAtUtc { get; set; } = DateTime.UtcNow;
    public string BuyerId { get; set; } = string.Empty;

    // Navigation property (optional but recommended for EF)
    public virtual Skin? Skin { get; set; }
}

