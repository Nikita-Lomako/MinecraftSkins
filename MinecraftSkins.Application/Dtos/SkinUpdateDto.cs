namespace MinecraftSkins.Application.Dtos;

public class SkinUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public decimal BasePriceUsd { get; set; }
    public bool IsAvailable { get; set; }
}

