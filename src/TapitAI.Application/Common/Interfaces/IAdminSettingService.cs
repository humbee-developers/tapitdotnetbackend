namespace TapitAI.Application.Common.Interfaces;

public interface IAdminSettingService
{
    Task<double> GetDoubleAsync(string key, double defaultValue = 0, CancellationToken ct = default);
    Task<int> GetIntAsync(string key, int defaultValue = 0, CancellationToken ct = default);
    Task<string> GetStringAsync(string key, string defaultValue = "", CancellationToken ct = default);
    Task SetAsync(string key, string value, CancellationToken ct = default);
    void InvalidateCache(string key);
}
