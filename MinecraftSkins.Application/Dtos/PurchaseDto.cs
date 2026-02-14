namespace MinecraftSkins.Application.Dtos;

public class PurchaseDto
{
    public Guid Id { get; set; }
    public Guid SkinId { get; set; }
    public string? SkinName { get; set; }
    public decimal PriceUsdFinal { get; set; }
    public decimal BtcUsdRate { get; set; }
    public DateTime PurchasedAtUtc { get; set; }
    public string BuyerId { get; set; } = string.Empty;
}

