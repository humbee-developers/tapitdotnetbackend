using Microsoft.Extensions.Options;
using TapitAI.Infrastructure.Services.Media.Providers;
using TapitAI.Infrastructure.Settings;

namespace TapitAI.Infrastructure.Services.Media;

public class MediaStorageFactory(
    IEnumerable<IStorageProvider> providers,
    IOptions<StorageSettings> options)
{
    private readonly string _activeProvider = options.Value.ActiveProvider;

    public IStorageProvider GetActiveProvider()
    {
        var provider = providers.FirstOrDefault(p =>
            p.ProviderType.Equals(_activeProvider, StringComparison.OrdinalIgnoreCase));

        return provider ?? throw new InvalidOperationException(
            $"Storage provider '{_activeProvider}' is not registered. " +
            $"Available: {string.Join(", ", providers.Select(p => p.ProviderType))}");
    }

    public IStorageProvider GetProvider(string providerType)
    {
        var provider = providers.FirstOrDefault(p =>
            p.ProviderType.Equals(providerType, StringComparison.OrdinalIgnoreCase));

        return provider ?? throw new InvalidOperationException($"Storage provider '{providerType}' is not registered.");
    }
}
