using MinecraftSkins.Domain.Interfaces;

namespace MinecraftSkins.Domain.Models;

public class CartItem : IEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CartId { get; set; }
    public Guid SkinId { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPriceUsd { get; set; }

    public Cart? Cart { get; set; }
    public Skin? Skin { get; set; }
}
