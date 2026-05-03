namespace TapitAI.Domain.Interfaces.Services;

public interface IFirebaseService
{
    Task SendToUserAsync(string userId, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default);
    Task SendToTokenAsync(string token, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default);
}
