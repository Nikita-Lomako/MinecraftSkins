using MinecraftSkins.Domain.Interfaces;

namespace MinecraftSkins.Domain.Models;

public class Cart : IEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BuyerId { get; set; } = string.Empty;

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
