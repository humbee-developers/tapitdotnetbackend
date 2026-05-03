namespace TapitAI.Domain.Interfaces.Services;

public interface IRealTimeService
{
    Task SendToUserAsync(string userId, string eventName, object payload, CancellationToken ct = default);
    Task SendToUsersAsync(IEnumerable<string> userIds, string eventName, object payload, CancellationToken ct = default);
}
