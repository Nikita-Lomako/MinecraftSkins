using System;

namespace MinecraftSkins.Application.Services;

public static class SkinPriceCalculator
{
    // Константы для защиты от экстремальных значений (Limits)
    private const decimal MinPriceMultiplier = 0.5m; // Цена не может упасть ниже 50% от базы
    private const decimal MaxPriceMultiplier = 3.0m; // Цена не может вырасти выше 300% от базы
    private const int RoundingPrecision = 2;         // Округление до центов (0.01 USD)

    /// <summary>
    /// Рассчитывает финальную цену скина в USD на основе динамики курса BTC.
    /// </summary>
    /// <param name="basePriceUsd">Цена скина в USD на момент его выхода (фиксированная величина).</param>
    /// <param name="btcPriceAtRelease">Курс BTC в USD .</param>
    /// <param name="btcPriceAtPurchase">Текущий курс BTC в USD на момент покупки.</param>
    /// <param name="liquidityFee">Комиссия за ликвидность (например, 0.02 для 2%).</param>
    /// <returns>Детерминированная цена в USD.</returns>
    public static decimal CalculateFinalPrice(
        decimal basePriceUsd, 
        decimal btcPriceAtRelease, 
        decimal btcPriceAtPurchase, 
        decimal liquidityFee = 0.02m)
    {
        // 1. Проверка на экстремальные значения (Guard Clauses)
        if (btcPriceAtRelease <= 0) throw new ArgumentException("Курс BTC на момент релиза должен быть больше 0.");
        if (btcPriceAtPurchase <= 0) btcPriceAtPurchase = btcPriceAtRelease; // Защита от ошибок API

        // 2. Расчет коэффициента изменения BTC
        // Это детерминированный множитель: насколько вырос или упал BTC
        decimal btcGrowthFactor = btcPriceAtPurchase / btcPriceAtRelease;

        // 3. Ограничение влияния волатильности (Clamping)
        // Чтобы цена скина не улетела в бесконечность при сильном росте BTC
        decimal clampedFactor = Math.Max(MinPriceMultiplier, Math.Min(MaxPriceMultiplier, btcGrowthFactor));

        // 4. Основная формула
        // Финальная цена = (База * Коэффициент роста) * (1 + Комиссия)
        // ВАЖНО: (1 - liquidityFee) увеличивает цену. Если fee = 0.02, то цена * 1.02.
        
        // Если бы это была наценка, было бы (1 + liquidityFee). Оставляем как просил пользователь.
        decimal rawPrice = (basePriceUsd * clampedFactor) * (1 + liquidityFee);

        // 5. Детерминированное округление
        // Используем MidpointRounding.AwayFromZero для финансовой точности (0.005 -> 0.01)
        decimal finalPrice = Math.Round(rawPrice, RoundingPrecision, MidpointRounding.AwayFromZero);

        return finalPrice;
    }
}

