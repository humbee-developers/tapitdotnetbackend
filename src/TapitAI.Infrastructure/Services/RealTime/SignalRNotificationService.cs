using Microsoft.AspNetCore.SignalR;
using TapitAI.Domain.Interfaces.Services;
using TapitAI.Infrastructure.Hubs;

namespace TapitAI.Infrastructure.Services.RealTime;

public class SignalRNotificationService(IHubContext<ConnectionHub> hubContext) : IRealTimeService
{
    public async Task SendToUserAsync(string userId, string eventName, object payload, CancellationToken ct = default)
        => await hubContext.Clients.Group($"user_{userId}").SendAsync(eventName, payload, ct);

    public async Task SendToUsersAsync(IEnumerable<string> userIds, string eventName, object payload, CancellationToken ct = default)
    {
        var groups = userIds.Select(uid => $"user_{uid}").ToList();
        await hubContext.Clients.Groups(groups).SendAsync(eventName, payload, ct);
    }
}
