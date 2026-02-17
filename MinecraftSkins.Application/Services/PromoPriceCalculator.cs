using Microsoft.Extensions.Options;
using MinecraftSkins.Application.Interfaces;
using MinecraftSkins.Application.Options;

namespace MinecraftSkins.Application.Services;

public class PromoPriceCalculator : IPriceCalculator
{
    private readonly PriceCalculatorOptions _options;

    public PromoPriceCalculator(IOptions<PriceCalculatorOptions> options)
    {
        _options = options.Value;
    }

    public decimal CalculateFinalPrice(decimal basePriceUsd, decimal currentBtcRate)
    {
        var standardPrice = SkinPriceCalculator.CalculateFinalPrice(
            basePriceUsd,
            _options.ReferenceBtcRate,
            currentBtcRate);

        return Math.Round(standardPrice * _options.PromoDiscount, 2, MidpointRounding.AwayFromZero);
    }
}
