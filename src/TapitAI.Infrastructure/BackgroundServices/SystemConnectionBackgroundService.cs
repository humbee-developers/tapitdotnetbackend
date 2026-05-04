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

public class SystemConnectionBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<SystemConnectionBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var settings = scope.ServiceProvider.GetRequiredService<IAdminSettingService>();
                var intervalSeconds = await settings.GetIntAsync(AdminSettingKeys.SystemConnectionIntervalSeconds, 30, stoppingToken);

                await TryCreateSystemConnectionsAsync(scope.ServiceProvider, stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in SystemConnectionBackgroundService");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    private async Task TryCreateSystemConnectionsAsync(IServiceProvider services, CancellationToken ct)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var settings = services.GetRequiredService<IAdminSettingService>();
        var realTime = services.GetRequiredService<IRealTimeService>();
        var firebase = services.GetRequiredService<IFirebaseService>();

        var connectionRadius = await settings.GetDoubleAsync(AdminSettingKeys.ConnectionRadiusMiles, 25, ct);
        var connectionLimit = await settings.GetIntAsync(AdminSettingKeys.ConnectionsPerDayLimit, 3, ct);
        var sendLimit = await settings.GetIntAsync(AdminSettingKeys.ConnectionRequestsSendPerDay, 5, ct);
        var receiveLimit = await settings.GetIntAsync(AdminSettingKeys.ConnectionRequestsReceivePerDay, 5, ct);
        var expiryMinutes = await settings.GetIntAsync(AdminSettingKeys.ConnectionExpiryMinutes, 30, ct);

        var today = DateTime.UtcNow.Date;
        var radiusMeters = connectionRadius * 1609.34;

        var tapInUserIds = await db.Set<TapStatus>()
            .Where(ts => ts.Status == TapStatusEnum.TapIn)
            .Select(ts => ts.UserId)
            .ToListAsync(ct);

        var profiledUserIds = await db.Set<UserDatingProfile>()
            .Where(p => tapInUserIds.Contains(p.UserId))
            .Select(p => p.UserId)
            .ToListAsync(ct);

        var locations = await db.Set<UserLocation>()
            .Where(ul => profiledUserIds.Contains(ul.UserId) && ul.IsLatest)
            .ToListAsync(ct);

        var profiles = await db.Set<UserDatingProfile>()
            .Where(p => profiledUserIds.Contains(p.UserId))
            .ToListAsync(ct);

        var usersWithPending = await db.Set<Connection>()
            .Where(c => c.InvitationStatus == InvitationStatus.Pending)
            .Select(c => new[] { c.SenderUserId, c.ReceiverUserId })
            .ToListAsync(ct);
        var blockedUserIds = usersWithPending.SelectMany(x => x).ToHashSet();

        var alreadyPaired = new HashSet<string>();
        var connectionsCreated = 0;

        foreach (var senderLocation in locations.OrderBy(_ => Random.Shared.Next()))
        {
            if (alreadyPaired.Contains(senderLocation.UserId)) continue;
            if (blockedUserIds.Contains(senderLocation.UserId)) continue;

            var senderProfile = profiles.FirstOrDefault(p => p.UserId == senderLocation.UserId);
            if (senderProfile is null) continue;

            var senderSentToday = await db.Set<Connection>()
                .CountAsync(c => c.SenderUserId == senderLocation.UserId && c.InvitedAt.Date == today, ct);
            if (senderSentToday >= sendLimit) continue;

            var senderConnectionsToday = await db.Set<Connection>()
                .CountAsync(c =>
                    (c.SenderUserId == senderLocation.UserId || c.ReceiverUserId == senderLocation.UserId)
                    && c.ConnectedAt.HasValue && c.ConnectedAt.Value.Date == today, ct);
            if (senderConnectionsToday >= connectionLimit) continue;

            var senderInterestedGenders = new HashSet<string>(senderProfile.GenderPreference, StringComparer.OrdinalIgnoreCase);
            var senderGender = senderProfile.Gender;

            var candidates = locations
                .Where(ul =>
                    ul.UserId != senderLocation.UserId
                    && !alreadyPaired.Contains(ul.UserId)
                    && !blockedUserIds.Contains(ul.UserId)
                    && CalculateDistance(senderLocation.Location.Y, senderLocation.Location.X,
                        ul.Location.Y, ul.Location.X) <= radiusMeters)
                .ToList();

            foreach (var receiverLocation in candidates.OrderBy(_ => Random.Shared.Next()))
            {
                var receiverProfile = profiles.FirstOrDefault(p => p.UserId == receiverLocation.UserId);
                if (receiverProfile is null) continue;

                var theirGender = receiverProfile.Gender;
                var theirInterested = new HashSet<string>(receiverProfile.GenderPreference, StringComparer.OrdinalIgnoreCase);

                if (!senderInterestedGenders.Contains(theirGender)) continue;
                if (!theirInterested.Contains(senderGender)) continue;

                var receiverReceiveToday = await db.Set<Connection>()
                    .CountAsync(c => c.ReceiverUserId == receiverLocation.UserId && c.InvitedAt.Date == today, ct);
                if (receiverReceiveToday >= receiveLimit) continue;

                var existingConn = await db.Set<Connection>()
                    .AnyAsync(c =>
                        ((c.SenderUserId == senderLocation.UserId && c.ReceiverUserId == receiverLocation.UserId)
                        || (c.SenderUserId == receiverLocation.UserId && c.ReceiverUserId == senderLocation.UserId))
                        && c.InvitationStatus == InvitationStatus.Pending, ct);

                if (existingConn) continue;

                var message = PickupLineProvider.GetRandom();
                var connection = Connection.Create(
                    senderLocation.UserId, receiverLocation.UserId,
                    senderLocation.Location.Y, senderLocation.Location.X,
                    receiverLocation.Location.Y, receiverLocation.Location.X,
                    ConnectionInitiatedVia.System, message, expiryMinutes);

                db.Set<Connection>().Add(connection);
                await db.SaveChangesAsync(ct);

                alreadyPaired.Add(senderLocation.UserId);
                alreadyPaired.Add(receiverLocation.UserId);

                await realTime.SendToUserAsync(receiverLocation.UserId, HubEvents.ConnectionRequestReceived, new
                {
                    ConnectionId = connection.Id,
                    SenderMaskedName = MaskName(senderProfile.DisplayName),
                    Message = message,
                    InitiatedVia = "System",
                    ExpiresAt = connection.ExpiresAt
                }, ct);

                await realTime.SendToUserAsync(senderLocation.UserId, HubEvents.ConnectionRequestSent, new
                {
                    ConnectionId = connection.Id,
                    ReceiverMaskedName = MaskName(receiverProfile.DisplayName),
                    Message = "We found a match nearby!",
                    ExpiresAt = connection.ExpiresAt
                }, ct);

                await firebase.SendToUserAsync(receiverLocation.UserId, "New Match Nearby!",
                    "Someone wants to connect with you!", ct: ct);
                await firebase.SendToUserAsync(senderLocation.UserId, "We Found a Match!",
                    "We found someone nearby for you!", ct: ct);

                connectionsCreated++;
                break;
            }
        }

        if (connectionsCreated > 0)
            logger.LogInformation("System created {Count} connection requests", connectionsCreated);
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static string MaskName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return name;
        var chars = name.ToCharArray();
        for (var i = 1; i < chars.Length; i += 2)
            if (chars[i] != ' ') chars[i] = '*';
        return new string(chars);
    }
}

file static class PickupLineProvider
{
    private static readonly string[] Lines =
    [
        "Hey there! Someone nearby thinks you look amazing 😊",
        "A nearby match is hoping to connect with you!",
        "You've caught someone's eye nearby — they'd love to meet you!",
        "Something tells me you two would get along great ✨",
        "Life is short — why not make a new connection nearby? 😄",
        "There's someone close by who'd love to get to know you!",
        "Sparks might fly — someone nearby wants to connect!",
        "Your next great story might start right here 🌟",
        "Hey! A nearby match thinks you're worth knowing 💫",
        "A new adventure might be just around the corner 🗺️",
        "You never know — this could be the start of something wonderful!",
        "Fate brought you two close together — make the most of it!",
        "A local connection is waiting to happen ✌️",
        "Someone nearby is hoping you'll say yes!",
        "This match was found just for you — don't miss it! 🎯"
    ];

    internal static string GetRandom() => Lines[Random.Shared.Next(Lines.Length)];
}
