namespace MinecraftSkins.Domain.Models;

public class SearchQueryHistory
{
    public long Id { get; set; }
    public string? UserId { get; set; }
    public string QueryText { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "skins";
    public int? ResultCount { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
