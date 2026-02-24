using System.Threading;
using System.Threading.Tasks;
using MinecraftSkins.Domain.Models;

namespace MinecraftSkins.Application.Interfaces;

public interface IBtcRateService
{
    Task<BtcRateResult> GetBtcUsdRateAsync(CancellationToken cancellationToken = default);
}
