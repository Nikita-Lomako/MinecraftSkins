using Microsoft.Extensions.Options;
using MinecraftSkins.Application.Options;

namespace MinecraftSkins.Tests.UnitTests.Application.Services;

public class PriceCalculatorPropertyTests
{
    private readonly StandardPriceCalculator _standard;
    private readonly PromoPriceCalculator _promo;
    private readonly Faker _faker = new();

    public PriceCalculatorPropertyTests()
    {
        var options = Options.Create(new PriceCalculatorOptions
        {
            ReferenceBtcRate = 68000m,
            LiquidityFee = 0.02m,
            PromoDiscount = 0.9m
        });

        _standard = new StandardPriceCalculator(options);
        _promo = new PromoPriceCalculator(options);

        Randomizer.Seed = new Random(1337);
    }

    [Fact]
    public void StandardPrice_ForPositiveInputs_StaysWithinClampedBounds()
    {
        for (var i = 0; i < 150; i++)
        {
            var basePrice = _faker.Random.Decimal(0.01m, 10_000m);
            var btcRate = _faker.Random.Decimal(1m, 500_000m);

            var actual = _standard.CalculateFinalPrice(basePrice, btcRate);

            var minExpected = Math.Round(basePrice * 0.5m * 1.02m, 2, MidpointRounding.AwayFromZero);
            var maxExpected = Math.Round(basePrice * 3.0m * 1.02m, 2, MidpointRounding.AwayFromZero);

            actual.Should().BeGreaterThanOrEqualTo(minExpected);
            actual.Should().BeLessThanOrEqualTo(maxExpected);
        }
    }

    [Fact]
    public void StandardPrice_WhenBasePriceGrows_ResultDoesNotDecrease()
    {
        for (var i = 0; i < 120; i++)
        {
            var basePriceA = _faker.Random.Decimal(0.01m, 5_000m);
            var basePriceB = basePriceA + _faker.Random.Decimal(0.01m, 5_000m);
            var btcRate = _faker.Random.Decimal(1m, 500_000m);

            var resultA = _standard.CalculateFinalPrice(basePriceA, btcRate);
            var resultB = _standard.CalculateFinalPrice(basePriceB, btcRate);

            resultB.Should().BeGreaterThanOrEqualTo(resultA);
        }
    }

    [Fact]
    public void PromoPrice_ForAnyValidInput_IsNotGreaterThanStandard()
    {
        for (var i = 0; i < 150; i++)
        {
            var basePrice = _faker.Random.Decimal(0.01m, 10_000m);
            var btcRate = _faker.Random.Decimal(1m, 500_000m);

            var standard = _standard.CalculateFinalPrice(basePrice, btcRate);
            var promo = _promo.CalculateFinalPrice(basePrice, btcRate);

            promo.Should().BeLessThanOrEqualTo(standard);
        }
    }
}

