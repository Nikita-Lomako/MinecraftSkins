using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using MinecraftSkins.Domain.Interfaces;
using MinecraftSkins.Domain.Models;

namespace MinecraftSkins.Infrastructure.Services;

public class CoinGeckoBtcRateProvider : IBtcRateProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CoinGeckoBtcRateProvider> _logger;

    public CoinGeckoBtcRateProvider(HttpClient httpClient, ILogger<CoinGeckoBtcRateProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<BtcRateResult> GetBtcUsdRateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching BTC rate from CoinGecko API");
        
        // HttpClientFactory usage is implicit here because _httpClient is injected 
        // as a Typed Client configured in Program.cs via AddHttpClient<IBtcRateProvider, CoinGeckoBtcRateProvider>
        
        var response = await _httpClient.GetFromJsonAsync<CoinGeckoResponse>(
            "simple/price?ids=bitcoin&vs_currencies=usd", 
            cancellationToken);

        if (response?.Bitcoin?.Usd == null)
        {
            throw new InvalidOperationException("Invalid response format from CoinGecko API");
        }

        var rate = response.Bitcoin.Usd.Value;
        _logger.LogDebug("Successfully fetched BTC rate: {Rate}", rate);

        return new BtcRateResult
        {
            Rate = rate,
            AsOfUtc = DateTime.UtcNow,
            Source = "External",
            AgeSeconds = 0
        };
    }

    private class CoinGeckoResponse
    {
        [JsonPropertyName("bitcoin")]
        public BitcoinData? Bitcoin { get; set; }
    }

    private class BitcoinData
    {
        [JsonPropertyName("usd")]
        public decimal? Usd { get; set; }
    }
}
