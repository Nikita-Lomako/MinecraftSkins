namespace MinecraftSkins.Application.Dtos;

public class SkinDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal BasePriceUsd { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public decimal? FinalPrice { get; set; } // Будет рассчитана в сервисе
    public decimal? CurrentBtcRate { get; set; } // Текущий курс BTC/USD
}

