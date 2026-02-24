using MinecraftSkins.Domain.Interfaces;
using MinecraftSkins.Domain.Models;

namespace MinecraftSkins.Tests.Mocks;

public class FakeBtcRateProvider : IBtcRateProvider
{
    private readonly decimal _rate;
    private readonly bool _shouldThrow;

    public FakeBtcRateProvider(decimal rate = 68000m, bool shouldThrow = false)
    {
        _rate = rate;
        _shouldThrow = shouldThrow;
    }

    public Task<BtcRateResult> GetBtcUsdRateAsync(CancellationToken cancellationToken = default)
    {
        if (_shouldThrow)
        {
            throw new HttpRequestException("External provider unavailable");
        }

        return Task.FromResult(new BtcRateResult
        {
            Rate = _rate,
            AsOfUtc = DateTime.UtcNow,
            Source = "FakeProvider"
        });
    }
}
