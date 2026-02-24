using Microsoft.Extensions.Options;
using MinecraftSkins.Application.Interfaces;
using MinecraftSkins.Application.Options;

namespace MinecraftSkins.Application.Services;

public class StandardPriceCalculator : IPriceCalculator
{
    private readonly PriceCalculatorOptions _options;

    public StandardPriceCalculator(IOptions<PriceCalculatorOptions> options)
    {
        _options = options.Value;
    }

    public decimal CalculateFinalPrice(decimal basePriceUsd, decimal currentBtcRate)
    {

        return SkinPriceCalculator.CalculateFinalPrice(
            basePriceUsd,
            _options.ReferenceBtcRate,
            currentBtcRate);
    }
}
