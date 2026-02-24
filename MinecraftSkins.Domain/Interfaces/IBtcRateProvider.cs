using System.Threading;
using System.Threading.Tasks;
using MinecraftSkins.Domain.Models;

namespace MinecraftSkins.Domain.Interfaces;

public interface IBtcRateProvider
{
    Task<BtcRateResult> GetBtcUsdRateAsync(CancellationToken cancellationToken = default);
}

