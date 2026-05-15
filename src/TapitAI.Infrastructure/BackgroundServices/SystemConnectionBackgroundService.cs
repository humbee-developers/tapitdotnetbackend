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
        var sendLimit = await settings.GetIntAsync(AdminSettingKeys.ConnectionRequestsSendPerDay, 10, ct);
        var receiveLimit = await settings.GetIntAsync(AdminSettingKeys.ConnectionRequestsReceivePerDay, 5, ct);
        var expiryMinutes = await settings.GetIntAsync(AdminSettingKeys.ConnectionExpiryMinutes, 30, ct);

        var today = DateTime.UtcNow.Date;
        var radiusMeters = connectionRadius * 1609.34;
        var staleMinutes = await settings.GetIntAsync(AdminSettingKeys.LocationStaleMinutes, 30, ct);
        var staleThreshold = DateTime.UtcNow.AddMinutes(-staleMinutes);

        // Users without a TapStatus record are TapIn by default — only exclude explicit TapOut
        var tappedOutUserIds = await db.Set<TapStatus>()
            .Where(ts => ts.Status == TapStatusEnum.TapOut)
            .Select(ts => ts.UserId)
            .ToHashSetAsync(ct);

        // Users with at least one active device token are considered logged in.
        var loggedInUserIds = await db.Set<UserDevice>()
            .Where(d => d.IsActive)
            .Select(d => d.UserId)
            .Distinct()
            .ToHashSetAsync(ct);

        var profiledUserIds = await db.Set<UserDatingProfile>()
            .Where(p => !tappedOutUserIds.Contains(p.UserId) && loggedInUserIds.Contains(p.UserId))
            .Select(p => p.UserId)
            .ToListAsync(ct);

        var locations = await db.Set<UserLocation>()
            .Where(ul => profiledUserIds.Contains(ul.UserId) && ul.IsLatest && ul.CreatedAt >= staleThreshold)
            .ToListAsync(ct);

        var profiles = await db.Set<UserDatingProfile>()
            .Where(p => profiledUserIds.Contains(p.UserId))
            .ToListAsync(ct);

        var placeholders = await db.Set<PlaceholderPhoto>()
            .Where(pp => pp.IsActive)
            .ToListAsync(ct);

        var usersWithActive = await db.Set<Connection>()
            .Where(c => c.InvitationStatus == InvitationStatus.Pending
                     || (c.InvitationStatus == InvitationStatus.Accepted
                         && c.ConnectedAt == null
                         && (c.SenderConnectionStatus == ParticipantConnectionStatus.Pending
                             || c.ReceiverConnectionStatus == ParticipantConnectionStatus.Pending)))
            .Select(c => new[] { c.SenderUserId, c.ReceiverUserId })
            .ToListAsync(ct);
        var blockedUserIds = usersWithActive.SelectMany(x => x).ToHashSet();

        // Block pairs: if A blocked B or B blocked A, skip this pair entirely
        var blockPairs = await db.Set<UserBlock>()
            .Where(b => profiledUserIds.Contains(b.BlockerUserId) || profiledUserIds.Contains(b.BlockedUserId))
            .Select(b => new { b.BlockerUserId, b.BlockedUserId })
            .ToListAsync(ct);
        var isBlockedPair = (string a, string b) =>
            blockPairs.Any(p =>
                (p.BlockerUserId == a && p.BlockedUserId == b) ||
                (p.BlockerUserId == b && p.BlockedUserId == a));

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

                var iAmInterested = senderInterestedGenders.Count == 0 || senderInterestedGenders.Contains(theirGender);
                var theyAreInterested = theirInterested.Count == 0 || theirInterested.Contains(senderGender);
                if (!iAmInterested && !theyAreInterested) continue;

                if (isBlockedPair(senderLocation.UserId, receiverLocation.UserId)) continue;

                var receiverReceiveToday = await db.Set<Connection>()
                    .CountAsync(c => c.ReceiverUserId == receiverLocation.UserId && c.InvitedAt.Date == today, ct);
                if (receiverReceiveToday >= receiveLimit) continue;

                // Skip if a live connection exists between this pair.
                // Terminal (Rejected, Withdrawn, Expired) and dead Accepted (decision phase over,
                // no chat started) allow re-pairing; everything else does not.
                var existingConn = await db.Set<Connection>()
                    .AnyAsync(c =>
                        ((c.SenderUserId == senderLocation.UserId && c.ReceiverUserId == receiverLocation.UserId)
                        || (c.SenderUserId == receiverLocation.UserId && c.ReceiverUserId == senderLocation.UserId))
                        && c.InvitationStatus != InvitationStatus.Rejected
                        && c.InvitationStatus != InvitationStatus.Withdrawn
                        && c.InvitationStatus != InvitationStatus.Expired
                        && !(c.InvitationStatus == InvitationStatus.Accepted
                             && c.ConnectedAt == null
                             && c.SenderConnectionStatus != ParticipantConnectionStatus.Pending
                             && c.ReceiverConnectionStatus != ParticipantConnectionStatus.Pending), ct);

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

                var senderGenderStr = senderProfile.Gender ?? "MALE";
                var senderPlaceholderPhotoUrl = GetPlaceholder(placeholders, senderGenderStr);
                var senderMaskedName = MaskName(senderProfile.DisplayName);

                var receiverGenderStr = receiverProfile.Gender ?? "MALE";
                var receiverPlaceholderPhotoUrl = GetPlaceholder(placeholders, receiverGenderStr);

                await realTime.SendToUserAsync(receiverLocation.UserId, HubEvents.ConnectionRequestReceived, new
                {
                    ConnectionId = connection.Id,
                    SenderMaskedName = senderMaskedName,
                    SenderAgeRange = senderProfile.AgeRange,
                    SenderGender = senderGenderStr,
                    SenderPlaceholderPhotoUrl = senderPlaceholderPhotoUrl,
                    Message = message,
                    InitiatedVia = "System",
                    ExpiresAt = connection.ExpiresAt
                }, ct);

                await realTime.SendToUserAsync(senderLocation.UserId, HubEvents.ConnectionRequestSent, new
                {
                    ConnectionId = connection.Id,
                    ReceiverMaskedName = MaskName(receiverProfile.DisplayName),
                    ReceiverPlaceholderPhotoUrl = receiverPlaceholderPhotoUrl,
                    ExpiresAt = connection.ExpiresAt
                }, ct);

                await firebase.SendToUserAsync(receiverLocation.UserId,
                    title: "New Connection Request",
                    body: $"{senderMaskedName} wants to connect with you!",
                    data: new Dictionary<string, string>
                    {
                        ["type"]         = "ConnectionRequestReceived",
                        ["connectionId"] = connection.Id.ToString(),
                        ["message"]      = message
                    },
                    ct: ct);
                await firebase.SendToUserAsync(senderLocation.UserId,
                    title: "We Found a Match!",
                    body: "We found someone nearby for you!",
                    data: new Dictionary<string, string>
                    {
                        ["type"]         = "ConnectionRequestSent",
                        ["connectionId"] = connection.Id.ToString()
                    },
                    ct: ct);

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

    private static string? GetPlaceholder(List<PlaceholderPhoto> placeholders, string gender)
    {
        var match = placeholders.FirstOrDefault(p =>
            string.Equals(p.Gender, gender, StringComparison.OrdinalIgnoreCase));
        return (match ?? placeholders.FirstOrDefault())?.PhotoUrl;
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
