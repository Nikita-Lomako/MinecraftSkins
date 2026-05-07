namespace MinecraftSkins.Application.Dtos;

public class CartDto
{
    public Guid Id { get; set; }
    public string BuyerId { get; set; } = string.Empty;
    public decimal TotalPriceUsd { get; set; }
    public List<CartItemDto> Items { get; set; } = new();
}

public class CartItemDto
{
    public Guid Id { get; set; }
    public Guid SkinId { get; set; }
    public string SkinName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPriceUsd { get; set; }
    public decimal TotalPriceUsd { get; set; }
}

public class AddCartItemDto
{
    public Guid SkinId { get; set; }
    public int Quantity { get; set; } = 1;
}
