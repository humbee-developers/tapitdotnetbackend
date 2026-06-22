using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TapitAI.Application.Common.Helpers;
using TapitAI.Application.Common.Interfaces;
using static TapitAI.Application.Common.Helpers.ConnectionEventPayload;
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
            var identity = scope.ServiceProvider.GetRequiredService<IIdentityService>();
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

            // Batch-resolve all Auth0 subs to internal UUIDs in one query.
            var allUserIds = toExpire.SelectMany(c => new[] { c.SenderUserId, c.ReceiverUserId }).Distinct().ToList();
            var idMap = await identity.ResolveInternalUserIdsAsync(allUserIds, ct);

            // Batch-load profiles + photos for all participants.
            var profileMap = (await db.Set<Domain.Entities.UserDatingProfile>()
                .Include(p => p.Photos)
                .Where(p => allUserIds.Contains(p.UserId))
                .ToListAsync(ct))
                .ToDictionary(p => p.UserId);

            var placeholders = await db.Set<Domain.Entities.PlaceholderPhoto>()
                .Where(pp => pp.IsActive)
                .ToListAsync(ct);

            foreach (var conn in toExpire)
            {
                conn.Expire();

                var senderInternalId   = idMap.GetValueOrDefault(conn.SenderUserId,   conn.SenderUserId);
                var receiverInternalId = idMap.GetValueOrDefault(conn.ReceiverUserId, conn.ReceiverUserId);

                profileMap.TryGetValue(conn.SenderUserId,   out var sp);
                profileMap.TryGetValue(conn.ReceiverUserId, out var rp);

                var eventProfiles = new ConnectionEventProfiles(
                    SenderDisplayName:           sp?.DisplayName,
                    SenderPhotoUrl:              sp?.Photos.FirstOrDefault(ph => ph.IsPrimary)?.PublicUrl,
                    SenderPlaceholderPhotoUrl:   GetPlaceholder(placeholders, sp?.Gender ?? "MALE",   conn.SenderUserId),
                    ReceiverDisplayName:         rp?.DisplayName,
                    ReceiverPhotoUrl:            rp?.Photos.FirstOrDefault(ph => ph.IsPrimary)?.PublicUrl,
                    ReceiverPlaceholderPhotoUrl: GetPlaceholder(placeholders, rp?.Gender ?? "MALE", conn.ReceiverUserId)
                );

                await realTime.SendToUserAsync(conn.SenderUserId, HubEvents.ConnectionExpired,
                    ConnectionEventPayload.Build(conn, senderInternalId, receiverInternalId, eventProfiles, senderInternalId), ct);

                await realTime.SendToUserAsync(conn.ReceiverUserId, HubEvents.ConnectionExpired,
                    ConnectionEventPayload.Build(conn, senderInternalId, receiverInternalId, eventProfiles, receiverInternalId), ct);
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
