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

        var todayStart = DateTime.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1);
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

        if (locations.Count == 0) return;

        var locationUserIds = locations.Select(l => l.UserId).ToHashSet();

        // Load profiles only for users who have fresh locations (not the full profiled pool)
        var profiles = await db.Set<UserDatingProfile>()
            .Where(p => locationUserIds.Contains(p.UserId))
            .ToListAsync(ct);

        var placeholders = await db.Set<PlaceholderPhoto>()
            .Where(pp => pp.IsActive)
            .ToListAsync(ct);

        // Users in an active (non-dead) connection cannot be matched
        var usersWithActiveConn = await db.Set<Connection>()
            .Where(c => c.InvitationStatus == InvitationStatus.Pending
                     || (c.InvitationStatus == InvitationStatus.Accepted
                         && c.ConnectedAt == null
                         && (c.SenderConnectionStatus == ParticipantConnectionStatus.Pending
                             || c.ReceiverConnectionStatus == ParticipantConnectionStatus.Pending)))
            .Select(c => new[] { c.SenderUserId, c.ReceiverUserId })
            .ToListAsync(ct);
        var activeConnectionUserIds = usersWithActiveConn.SelectMany(x => x).ToHashSet();

        // Block pairs: if A blocked B or B blocked A, skip this pair entirely
        var blockPairs = await db.Set<UserBlock>()
            .Where(b => locationUserIds.Contains(b.BlockerUserId) || locationUserIds.Contains(b.BlockedUserId))
            .Select(b => new { b.BlockerUserId, b.BlockedUserId })
            .ToListAsync(ct);
        var isBlockedPair = (string a, string b) =>
            blockPairs.Any(p =>
                (p.BlockerUserId == a && p.BlockedUserId == b) ||
                (p.BlockerUserId == b && p.BlockedUserId == a));

        // Pre-load today's sent counts per user — avoids one DB query per sender in the outer loop
        var sentTodayIds = await db.Set<Connection>()
            .Where(c => c.InvitedAt >= todayStart && c.InvitedAt < todayEnd
                     && locationUserIds.Contains(c.SenderUserId))
            .Select(c => c.SenderUserId)
            .ToListAsync(ct);
        var sentToday = sentTodayIds.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());

        // Pre-load today's received counts per user — avoids one DB query per candidate in the inner loop
        var receivedTodayIds = await db.Set<Connection>()
            .Where(c => c.InvitedAt >= todayStart && c.InvitedAt < todayEnd
                     && locationUserIds.Contains(c.ReceiverUserId))
            .Select(c => c.ReceiverUserId)
            .ToListAsync(ct);
        var receivedToday = receivedTodayIds.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());

        // Pre-load today's completed chat counts per user — avoids one DB query per sender in the outer loop
        var chatsTodayPairs = await db.Set<Connection>()
            .Where(c => c.ConnectedAt.HasValue
                     && c.ConnectedAt.Value >= todayStart && c.ConnectedAt.Value < todayEnd
                     && (locationUserIds.Contains(c.SenderUserId) || locationUserIds.Contains(c.ReceiverUserId)))
            .Select(c => new { c.SenderUserId, c.ReceiverUserId })
            .ToListAsync(ct);
        var chatsToday = new Dictionary<string, int>();
        foreach (var chat in chatsTodayPairs)
        {
            chatsToday[chat.SenderUserId] = chatsToday.GetValueOrDefault(chat.SenderUserId) + 1;
            chatsToday[chat.ReceiverUserId] = chatsToday.GetValueOrDefault(chat.ReceiverUserId) + 1;
        }

        // Pre-load all live connection pairs as a HashSet — avoids one DB query per candidate pair in the inner loop.
        // Excluded (allow re-pair): invitation expired naturally, invitation withdrawn, decision phase timed out for both.
        // Included (block re-pair): pending invitation, rejected invitation, someone actively passed, connected to chat.
        var livePairRows = await db.Set<Connection>()
            .Where(c =>
                (locationUserIds.Contains(c.SenderUserId) || locationUserIds.Contains(c.ReceiverUserId))
                && c.InvitationStatus != InvitationStatus.Withdrawn
                && c.InvitationStatus != InvitationStatus.Expired
                && !(c.InvitationStatus == InvitationStatus.Accepted
                     && c.ConnectedAt == null
                     && c.SenderConnectionStatus == ParticipantConnectionStatus.Expired
                     && c.ReceiverConnectionStatus == ParticipantConnectionStatus.Expired))
            .Select(c => new { c.SenderUserId, c.ReceiverUserId })
            .ToListAsync(ct);
        var livePairKeys = livePairRows
            .Select(p => PairKey(p.SenderUserId, p.ReceiverUserId))
            .ToHashSet();

        var alreadyPaired = new HashSet<string>();
        var connectionsCreated = 0;

        foreach (var senderLocation in locations.OrderBy(_ => Random.Shared.Next()))
        {
            if (alreadyPaired.Contains(senderLocation.UserId)) continue;
            if (activeConnectionUserIds.Contains(senderLocation.UserId)) continue;

            var senderProfile = profiles.FirstOrDefault(p => p.UserId == senderLocation.UserId);
            if (senderProfile is null) continue;

            if (sentToday.GetValueOrDefault(senderLocation.UserId) >= sendLimit) continue;
            if (chatsToday.GetValueOrDefault(senderLocation.UserId) >= connectionLimit) continue;

            var senderInterestedGenders = new HashSet<string>(senderProfile.GenderPreference, StringComparer.OrdinalIgnoreCase);
            var senderGender = senderProfile.Gender;

            var candidates = locations
                .Where(ul =>
                    ul.UserId != senderLocation.UserId
                    && !alreadyPaired.Contains(ul.UserId)
                    && !activeConnectionUserIds.Contains(ul.UserId)
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

                if (receivedToday.GetValueOrDefault(receiverLocation.UserId) >= receiveLimit) continue;

                if (livePairKeys.Contains(PairKey(senderLocation.UserId, receiverLocation.UserId))) continue;

                var message = PickupLineProvider.GetRandom();
                var connection = Connection.Create(
                    senderLocation.UserId, receiverLocation.UserId,
                    senderLocation.Location.Y, senderLocation.Location.X,
                    receiverLocation.Location.Y, receiverLocation.Location.X,
                    ConnectionInitiatedVia.System, message, expiryMinutes);

                // System connections skip the invitation phase — neither user "sent" this,
                // so both go straight to the decision phase (Connect or Pass).
                connection.Accept();

                db.Set<Connection>().Add(connection);
                await db.SaveChangesAsync(ct);

                alreadyPaired.Add(senderLocation.UserId);
                alreadyPaired.Add(receiverLocation.UserId);

                var senderGenderStr = senderProfile.Gender ?? "MALE";
                var senderPlaceholderPhotoUrl = GetPlaceholder(placeholders, senderGenderStr);
                var senderMaskedName = MaskName(senderProfile.DisplayName);

                var receiverGenderStr = receiverProfile.Gender ?? "MALE";
                var receiverPlaceholderPhotoUrl = GetPlaceholder(placeholders, receiverGenderStr);

                // Both users are immediately in the decision phase — notify with masked info.
                await realTime.SendToUserAsync(receiverLocation.UserId, HubEvents.ConnectionAccepted, new
                {
                    ConnectionId = connection.Id,
                    OtherUserMaskedName = senderMaskedName,
                    OtherUserAgeRange = senderProfile.AgeRange,
                    OtherUserGender = senderGenderStr,
                    OtherUserPlaceholderPhotoUrl = senderPlaceholderPhotoUrl,
                    Message = message,
                    InitiatedVia = "System",
                    ExpiresAt = connection.ExpiresAt
                }, ct);

                await realTime.SendToUserAsync(senderLocation.UserId, HubEvents.ConnectionAccepted, new
                {
                    ConnectionId = connection.Id,
                    OtherUserMaskedName = MaskName(receiverProfile.DisplayName),
                    OtherUserAgeRange = receiverProfile.AgeRange,
                    OtherUserGender = receiverGenderStr,
                    OtherUserPlaceholderPhotoUrl = receiverPlaceholderPhotoUrl,
                    Message = message,
                    InitiatedVia = "System",
                    ExpiresAt = connection.ExpiresAt
                }, ct);

                await firebase.SendToUserAsync(receiverLocation.UserId,
                    title: "We Found a Match!",
                    body: $"{senderMaskedName} is nearby — connect or pass?",
                    data: new Dictionary<string, string>
                    {
                        ["type"]         = "SystemConnectionMatched",
                        ["connectionId"] = connection.Id.ToString(),
                        ["message"]      = message
                    },
                    ct: ct);
                await firebase.SendToUserAsync(senderLocation.UserId,
                    title: "We Found a Match!",
                    body: "Someone nearby is ready — connect or pass?",
                    data: new Dictionary<string, string>
                    {
                        ["type"]         = "SystemConnectionMatched",
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

    private static string PairKey(string a, string b) =>
        string.Compare(a, b, StringComparison.Ordinal) < 0 ? $"{a}:{b}" : $"{b}:{a}";

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
