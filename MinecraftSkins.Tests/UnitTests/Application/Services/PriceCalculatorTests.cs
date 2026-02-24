using Microsoft.Extensions.Options;
using MinecraftSkins.Application.Options;
using MinecraftSkins.Application.Services;

namespace MinecraftSkins.Tests.UnitTests.Application.Services;

public class StandardPriceCalculatorTests
{
    private readonly StandardPriceCalculator _calculator;
    private readonly Fixture _fixture;

    public StandardPriceCalculatorTests()
    {
        var options = Options.Create(new PriceCalculatorOptions
        {
            ReferenceBtcRate = 68000m,
            LiquidityFee = 0.02m
        });
        _calculator = new StandardPriceCalculator(options);
        _fixture = new Fixture();
    }

    [Fact]
    public void CalculateFinalPrice_WithNormalBtcRate_ReturnsCorrectPrice()
    {
        // Arrange
        var basePrice = 10m;
        var currentBtcRate = 70000m; // BTC вырос с 68000 до 70000

        // Act
        var result = _calculator.CalculateFinalPrice(basePrice, currentBtcRate);

        // Assert
        // Коэффициент: 70000 / 68000 = 1.0294
        // Clamped: 1.0294 (в пределах 0.5-3.0)
        // Цена: 10 * 1.0294 * 1.02 = 10.5 (округлено)
        result.Should().BeApproximately(10.5m, 0.1m);
    }

    [Fact]
    public void CalculateFinalPrice_WithHighBtcRate_ClampsToMaxMultiplier()
    {
        // Arrange
        var basePrice = 10m;
        var currentBtcRate = 300000m; // Очень высокий курс

        // Act
        var result = _calculator.CalculateFinalPrice(basePrice, currentBtcRate);

        // Assert
        // Коэффициент должен быть заклэмплен до 3.0
        // Цена: 10 * 3.0 * 1.02 = 30.6
        result.Should().BeApproximately(30.6m, 0.1m);
    }

    [Fact]
    public void CalculateFinalPrice_WithLowBtcRate_ClampsToMinMultiplier()
    {
        // Arrange
        var basePrice = 10m;
        var currentBtcRate = 10000m; // Очень низкий курс

        // Act
        var result = _calculator.CalculateFinalPrice(basePrice, currentBtcRate);

        // Assert
        // Коэффициент должен быть заклэмплен до 0.5
        // Цена: 10 * 0.5 * 1.02 = 5.1
        result.Should().BeApproximately(5.1m, 0.1m);
    }

    [Fact]
    public void CalculateFinalPrice_WithZeroBtcRate_UsesReferenceRate()
    {
        // Arrange
        var basePrice = 10m;
        var currentBtcRate = 0m; // Защита от ошибок API

        // Act
        var result = _calculator.CalculateFinalPrice(basePrice, currentBtcRate);

        // Assert
        // При нулевом курсе используется reference rate (68000)
        // Коэффициент: 68000 / 68000 = 1.0
        // Цена: 10 * 1.0 * 1.02 = 10.2
        result.Should().BeApproximately(10.2m, 0.1m);
    }

    public static TheoryData<decimal, decimal, decimal> PriceData => new()
    {
        { 5m, 68000m, 5.1m },
        { 10m, 68000m, 10.2m },
        { 20m, 70000m, 21.0m }
    };

    [Theory]
    [MemberData(nameof(PriceData))]
    public void CalculateFinalPrice_WithVariousInputs_ReturnsExpectedPrice(
        decimal basePrice, 
        decimal btcRate, 
        decimal expectedPrice)
    {
        // Act
        var result = _calculator.CalculateFinalPrice(basePrice, btcRate);

        // Assert
        result.Should().BeApproximately(expectedPrice, 0.1m);
    }
}

public class PromoPriceCalculatorTests
{
    private readonly PromoPriceCalculator _calculator;
    private readonly Fixture _fixture;

    public PromoPriceCalculatorTests()
    {
        var options = Options.Create(new PriceCalculatorOptions
        {
            ReferenceBtcRate = 68000m,
            LiquidityFee = 0.02m,
            PromoDiscount = 0.9m // 10% скидка
        });
        _calculator = new PromoPriceCalculator(options);
        _fixture = new Fixture();
    }

    [Fact]
    public void CalculateFinalPrice_WithPromoDiscount_AppliesDiscount()
    {
        // Arrange
        var basePrice = 10m;
        var currentBtcRate = 70000m;

        // Act
        var result = _calculator.CalculateFinalPrice(basePrice, currentBtcRate);

        // Assert
        // Стандартная цена: 10 * 1.0294 * 1.02 = 10.5
        // С промо-скидкой: 10.5 * 0.9 = 9.45
        result.Should().BeApproximately(9.45m, 0.1m);
    }

    [Fact]
    public void CalculateFinalPrice_WithPromoDiscount_IsLowerThanStandard()
    {
        // Arrange
        var basePrice = 10m;
        var currentBtcRate = 70000m;

        var standardOptions = Options.Create(new PriceCalculatorOptions
        {
            ReferenceBtcRate = 68000m,
            LiquidityFee = 0.02m
        });
        var standardCalculator = new StandardPriceCalculator(standardOptions);

        // Act
        var promoResult = _calculator.CalculateFinalPrice(basePrice, currentBtcRate);
        var standardResult = standardCalculator.CalculateFinalPrice(basePrice, currentBtcRate);

        // Assert
        promoResult.Should().BeLessThan(standardResult);
    }
}
