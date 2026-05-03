using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Domain.Entities;
using TapitAI.Infrastructure.Data;

namespace TapitAI.Infrastructure.Services.Settings;

public class AdminSettingService(AppDbContext db, IDistributedCache cache) : IAdminSettingService
{
    private const int CacheTtlSeconds = 300;

    public async Task<double> GetDoubleAsync(string key, double defaultValue = 0, CancellationToken ct = default)
    {
        var raw = await GetRawAsync(key, ct);
        return raw is not null && double.TryParse(raw, out var v) ? v : defaultValue;
    }

    public async Task<int> GetIntAsync(string key, int defaultValue = 0, CancellationToken ct = default)
    {
        var raw = await GetRawAsync(key, ct);
        return raw is not null && int.TryParse(raw, out var v) ? v : defaultValue;
    }

    public async Task<string> GetStringAsync(string key, string defaultValue = "", CancellationToken ct = default)
        => await GetRawAsync(key, ct) ?? defaultValue;

    public async Task SetAsync(string key, string value, CancellationToken ct = default)
    {
        var setting = await db.Set<AdminSetting>().FirstOrDefaultAsync(s => s.Key == key, ct);
        if (setting is not null)
        {
            setting.UpdateValue(value);
            await db.SaveChangesAsync(ct);
        }
        InvalidateCache(key);
    }

    public void InvalidateCache(string key) => cache.Remove(CacheKey(key));

    private async Task<string?> GetRawAsync(string key, CancellationToken ct)
    {
        var cached = await cache.GetStringAsync(CacheKey(key), ct);
        if (cached is not null) return cached;

        var setting = await db.Set<AdminSetting>()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key, ct);

        if (setting is null) return null;

        await cache.SetStringAsync(CacheKey(key), setting.Value,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(CacheTtlSeconds) }, ct);

        return setting.Value;
    }

    private static string CacheKey(string key) => $"admin_setting:{key}";
}
