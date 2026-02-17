namespace MinecraftSkins.Application.Options;

public class PriceCalculatorOptions
{
    public const string SectionName = "PriceCalculator";

    public string Strategy { get; set; } = "Standard"; 
    public decimal ReferenceBtcRate { get; set; } = 68000m; 
    public decimal LiquidityFee { get; set; } = 0.02m; // 2% fee 
    public decimal PromoDiscount { get; set; } = 0.9m; // 10% discount
}
