using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace TapitAI.Infrastructure.Hubs;

[Authorize(Policy = "AdminOrUser")]
public class ConnectionHub(ILogger<ConnectionHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        logger.LogInformation("[Hub] Client connected: connectionId={ConnectionId} userId={UserId}",
            Context.ConnectionId, userId ?? "unknown");

        if (userId is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (exception is not null)
            logger.LogWarning("[Hub] Client disconnected with error: userId={UserId} error={Error}",
                userId ?? "unknown", exception.Message);
        else
            logger.LogInformation("[Hub] Client disconnected: userId={UserId}", userId ?? "unknown");

        if (userId is not null)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        await base.OnDisconnectedAsync(exception);
    }

    private string? GetUserId()
        => Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? Context.User?.FindFirstValue("sub");
}
