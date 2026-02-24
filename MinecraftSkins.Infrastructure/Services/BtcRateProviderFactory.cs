using System.Collections.Frozen;
using Microsoft.Extensions.Logging;
using MinecraftSkins.Domain.Interfaces;

namespace MinecraftSkins.Infrastructure.Services;

/// <summary>
/// Factory implementation using Dictionary pattern for provider registration.
/// This allows easy extension by adding new providers to the dictionary.
/// </summary>
public class BtcRateProviderFactory : IBtcRateProviderFactory
{
    private readonly FrozenDictionary<string, IBtcRateProvider> _providers;
    private readonly string _defaultProviderName;
    private readonly ILogger<BtcRateProviderFactory> _logger;

    public BtcRateProviderFactory(
        Dictionary<string, IBtcRateProvider> providers,
        string defaultProviderName,
        ILogger<BtcRateProviderFactory> logger)
    {
        if (providers == null || providers.Count == 0)
        {
            throw new ArgumentException("At least one provider must be registered", nameof(providers));
        }

        if (string.IsNullOrWhiteSpace(defaultProviderName))
        {
            throw new ArgumentException("Default provider name cannot be empty", nameof(defaultProviderName));
        }

        if (!providers.ContainsKey(defaultProviderName))
        {
            throw new ArgumentException($"Default provider '{defaultProviderName}' not found in providers dictionary", nameof(defaultProviderName));
        }

        _providers = providers.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        _defaultProviderName = defaultProviderName;
        _logger = logger;
        
        _logger.LogInformation("BtcRateProviderFactory initialized with {Count} providers. Default: {DefaultProvider}", 
            _providers.Count, _defaultProviderName);
    }

    public IBtcRateProvider GetProvider(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            _logger.LogWarning("Provider name is empty, using default provider: {DefaultProvider}", _defaultProviderName);
            return GetDefaultProvider();
        }

        if (!_providers.TryGetValue(providerName, out var provider))
        {
            _logger.LogWarning("Provider '{ProviderName}' not found. Available providers: {AvailableProviders}. Using default: {DefaultProvider}",
                providerName, string.Join(", ", _providers.Keys), _defaultProviderName);
            return GetDefaultProvider();
        }

        _logger.LogDebug("Returning provider: {ProviderName}", providerName);
        return provider;
    }

    public IBtcRateProvider GetDefaultProvider()
    {
        return _providers[_defaultProviderName];
    }

    public IEnumerable<string> GetAvailableProviders()
    {
        return _providers.Keys;
    }
}

