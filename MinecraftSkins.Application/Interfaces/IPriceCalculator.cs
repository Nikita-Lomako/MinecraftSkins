using System.Threading.Tasks;

namespace MinecraftSkins.Application.Interfaces;

public interface IPriceCalculator
{
    decimal CalculateFinalPrice(decimal basePriceUsd, decimal currentBtcRate);
}

