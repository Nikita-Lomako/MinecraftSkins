using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using MinecraftSkins.Domain.Interfaces;
using MinecraftSkins.Domain.Models;

namespace MinecraftSkins.Infrastructure.Services;

public class BinanceBtcRateProvider : IBtcRateProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BinanceBtcRateProvider> _logger;

    public BinanceBtcRateProvider(HttpClient httpClient, ILogger<BinanceBtcRateProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<BtcRateResult> GetBtcUsdRateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching BTC rate from Binance API");
        
        var response = await _httpClient.GetFromJsonAsync<BinanceResponse>(
            "api/v3/ticker/price?symbol=BTCUSDT", 
            cancellationToken);

        if (response?.Price == null)
        {
            throw new InvalidOperationException("Invalid response format from Binance API");
        }

        if (!decimal.TryParse(response.Price, out var rate))
        {
            throw new InvalidOperationException($"Failed to parse price from Binance API: {response.Price}");
        }

        _logger.LogDebug("Successfully fetched BTC rate from Binance: {Rate}", rate);

        return new BtcRateResult
        {
            Rate = rate,
            AsOfUtc = DateTime.UtcNow,
            Source = "External",
            AgeSeconds = 0
        };
    }

    private class BinanceResponse
    {
        [JsonPropertyName("price")]
        public string? Price { get; set; }
    }
}

