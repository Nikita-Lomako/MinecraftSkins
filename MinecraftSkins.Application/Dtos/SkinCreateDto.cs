namespace MinecraftSkins.Application.Dtos;

public class SkinCreateDto
{
    public string Name { get; set; } = string.Empty;
    public decimal BasePriceUsd { get; set; }
    public bool IsAvailable { get; set; } = true; // По умолчанию true
}

