using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using TapitAI.Domain.Interfaces.Services;
using TapitAI.Infrastructure.Settings;

namespace TapitAI.Infrastructure.Services.Cache;

public class RedisCacheService(IDistributedCache cache, IOptions<RedisSettings> options) : ICacheService
{
    private readonly TimeSpan _defaultExpiry = TimeSpan.FromMinutes(options.Value.DefaultExpiryMinutes);

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var data = await cache.GetStringAsync(key, ct);
        return data is null ? default : JsonSerializer.Deserialize<T>(data);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry ?? _defaultExpiry
        };
        await cache.SetStringAsync(key, JsonSerializer.Serialize(value), options, ct);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
        => await cache.RemoveAsync(key, ct);

    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        // Requires Redis SCAN - can be implemented with StackExchange.Redis IServer if needed
        // For now, this is a no-op stub that can be enhanced with IConnectionMultiplexer injection
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        => await cache.GetStringAsync(key, ct) is not null;

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached is not null) return cached;

        var value = await factory();
        await SetAsync(key, value, expiry, ct);
        return value;
    }
}
