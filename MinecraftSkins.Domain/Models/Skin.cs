using MinecraftSkins.Domain.Interfaces;

namespace MinecraftSkins.Domain.Models;

public class Skin : ISoftDeletable, IEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public decimal BasePriceUsd { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    
    // Soft Delete properties
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}
