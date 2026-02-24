namespace MinecraftSkins.Application.Dtos;

public class BtcRateDto
{
    public decimal Rate { get; set; }
    public DateTime AsOfUtc { get; set; }
    public string Source { get; set; } = string.Empty; // Cache, External, Fallback
    public int? AgeSeconds { get; set; } // Возраст данных в секундах
}

