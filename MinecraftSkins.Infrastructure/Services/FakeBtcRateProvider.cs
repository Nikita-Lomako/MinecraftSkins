using MinecraftSkins.Domain.Interfaces;
using MinecraftSkins.Domain.Models;

namespace MinecraftSkins.Infrastructure.Services;

public class FakeBtcRateProvider : IBtcRateProvider
{
    private readonly decimal _rate;
    public string Name => "Fake";

    public FakeBtcRateProvider(decimal rate) => _rate = rate;

    public Task<BtcRateResult> GetBtcUsdRateAsync(CancellationToken ct = default)
    {
        return Task.FromResult(new BtcRateResult
        {
            Rate = _rate,
            AsOfUtc = DateTime.UtcNow,
            Source = "Fake"
        });
    }
}