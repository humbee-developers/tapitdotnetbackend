using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using TapitAI.Domain.Interfaces.Services;
using TapitAI.Infrastructure.Hubs;

namespace TapitAI.Infrastructure.Services.RealTime;

public class SignalRNotificationService(
    IHubContext<ConnectionHub> hubContext,
    ILogger<SignalRNotificationService> logger) : IRealTimeService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    public async Task SendToUserAsync(string userId, string eventName, object payload, CancellationToken ct = default)
    {
        logger.LogInformation("[Socket] → {Event} to {UserId} | {Payload}",
            eventName, userId, JsonSerializer.Serialize(payload, JsonOpts));

        await hubContext.Clients.Group($"user_{userId}").SendAsync(eventName, payload, ct);
    }

    public async Task SendToUsersAsync(IEnumerable<string> userIds, string eventName, object payload, CancellationToken ct = default)
    {
        var ids = userIds.ToList();

        logger.LogInformation("[Socket] → {Event} to [{UserIds}] | {Payload}",
            eventName, string.Join(", ", ids), JsonSerializer.Serialize(payload, JsonOpts));

        var groups = ids.Select(uid => $"user_{uid}").ToList();
        await hubContext.Clients.Groups(groups).SendAsync(eventName, payload, ct);
    }
}
