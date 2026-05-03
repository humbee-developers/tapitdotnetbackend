using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Domain.Constants;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Enums;
using TapitAI.Domain.Interfaces.Services;
using TapitAI.Infrastructure.Data;

namespace TapitAI.Infrastructure.BackgroundServices;

public class ConnectionExpiryBackgroundService(IServiceScopeFactory scopeFactory, ILogger<ConnectionExpiryBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            await ExpireConnectionsAsync(stoppingToken);
        }
    }

    private async Task ExpireConnectionsAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var realTime = scope.ServiceProvider.GetRequiredService<IRealTimeService>();
            var settings = scope.ServiceProvider.GetRequiredService<IAdminSettingService>();

            var expiryMinutes = await settings.GetIntAsync(AdminSettingKeys.ConnectionExpiryMinutes, 30, ct);
            var cutoff = DateTime.UtcNow;

            var toExpire = await db.Set<Connection>()
                .Where(c =>
                    (c.InvitationStatus == InvitationStatus.Pending && c.ExpiresAt <= cutoff)
                    || (c.InvitationStatus == InvitationStatus.Accepted
                        && (c.SenderConnectionStatus == ParticipantConnectionStatus.Pending
                         || c.ReceiverConnectionStatus == ParticipantConnectionStatus.Pending)
                        && c.AcceptedAt.HasValue
                        && c.AcceptedAt.Value.AddMinutes(expiryMinutes) <= cutoff))
                .ToListAsync(ct);

            foreach (var conn in toExpire)
            {
                conn.Expire();
                await realTime.SendToUsersAsync(
                    new[] { conn.SenderUserId, conn.ReceiverUserId },
                    "ConnectionExpired", new { ConnectionId = conn.Id, Message = "This connection has expired." }, ct);
            }

            if (toExpire.Count > 0)
            {
                await db.SaveChangesAsync(ct);
                logger.LogInformation("Expired {Count} connections", toExpire.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in ConnectionExpiryBackgroundService");
        }
    }
}
