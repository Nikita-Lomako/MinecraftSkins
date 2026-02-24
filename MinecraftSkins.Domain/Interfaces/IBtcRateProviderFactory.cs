namespace MinecraftSkins.Domain.Interfaces;

/// <summary>
/// Factory for creating BTC rate providers based on configuration.
/// Uses Dictionary pattern for extensibility - easy to add new providers.
/// </summary>
public interface IBtcRateProviderFactory
{
    /// <summary>
    /// Gets a BTC rate provider by name.
    /// </summary>
    /// <param name="providerName">Name of the provider (e.g., "CoinGecko", "Binance")</param>
    /// <returns>Configured IBtcRateProvider instance</returns>
    /// <exception cref="ArgumentException">Thrown when provider name is not found</exception>
    IBtcRateProvider GetProvider(string providerName);
    
    /// <summary>
    /// Gets the default provider.
    /// </summary>
    IBtcRateProvider GetDefaultProvider();
    
    /// <summary>
    /// Gets all available provider names.
    /// </summary>
    IEnumerable<string> GetAvailableProviders();
}

