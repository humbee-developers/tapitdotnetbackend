using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Domain.Constants;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Enums;
using TapitAI.Domain.Interfaces.Services;
using TapitAI.Infrastructure.Data;

namespace TapitAI.Infrastructure.Services.Discovery;

public class DiscoveryService(AppDbContext db, IAdminSettingService settings, ILogger<DiscoveryService> logger) : IDiscoveryService
{
    public async Task<IReadOnlyList<NearbyUserResult>> GetNearbyUsersAsync(
        string requestingUserId, double radiusMiles, CancellationToken ct = default)
    {
        var myLocation = await db.Set<UserLocation>()
            .AsNoTracking()
            .Where(ul => ul.UserId == requestingUserId && ul.IsLatest)
            .FirstOrDefaultAsync(ct);

        if (myLocation is null)
        {
            logger.LogDebug("[Discovery] Skipped: requesting user {UserId} has no location record.", requestingUserId);
            return Array.Empty<NearbyUserResult>();
        }

        var myProfile = await db.Set<UserDatingProfile>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == requestingUserId, ct);

        if (myProfile is null)
        {
            logger.LogDebug("[Discovery] Skipped: requesting user {UserId} has no dating profile.", requestingUserId);
            return Array.Empty<NearbyUserResult>();
        }

        var connectionLimit = await settings.GetIntAsync(AdminSettingKeys.ConnectionsPerDayLimit, 3, ct);
        var minUsersThreshold = await settings.GetIntAsync(AdminSettingKeys.DiscoveryMinUsersThreshold, 5, ct);
        var staleMinutes = await settings.GetIntAsync(AdminSettingKeys.LocationStaleMinutes, 30, ct);
        var today = DateTime.UtcNow.Date;
        var radiusMeters = radiusMiles * 1609.34;

        var myGenderPreference = new HashSet<string>(myProfile.GenderPreference, StringComparer.OrdinalIgnoreCase);

        // Raw PostGIS query for nearby users — {4} = stale window in minutes (integer, no injection risk)
        var nearbyRaw = await db.Set<UserLocation>()
            .FromSqlRaw(@"
                SELECT ul.* FROM ""UserLocations"" ul
                WHERE ul.""IsLatest"" = true
                AND ul.""UserId"" != {0}
                AND ul.""CreatedAt"" >= NOW() - ({4} * INTERVAL '1 minute')
                AND ST_DWithin(
                    ul.""Location""::geography,
                    ST_SetSRID(ST_MakePoint({1}, {2}), 4326)::geography,
                    {3}
                )
                ORDER BY ST_Distance(ul.""Location""::geography, ST_SetSRID(ST_MakePoint({1}, {2}), 4326)::geography)",
                requestingUserId, myLocation.Location.X, myLocation.Location.Y, radiusMeters, staleMinutes)
            .AsNoTracking()
            .ToListAsync(ct);

        logger.LogDebug("[Discovery] Found {Count} users within {Radius} mi of {UserId}.", nearbyRaw.Count, radiusMiles, requestingUserId);

        // Not enough nearby — fall back to global (no distance constraint, no previously-connected filter)
        if (nearbyRaw.Count < minUsersThreshold)
        {
            var nearbyIds = nearbyRaw.Select(l => l.UserId).ToHashSet();
            var globalRaw = await db.Set<UserLocation>()
                .FromSqlRaw(@"
                    SELECT ul.* FROM ""UserLocations"" ul
                    WHERE ul.""IsLatest"" = true
                    AND ul.""UserId"" != {0}
                    AND ul.""CreatedAt"" >= NOW() - ({3} * INTERVAL '1 minute')
                    ORDER BY ST_Distance(ul.""Location""::geography, ST_SetSRID(ST_MakePoint({1}, {2}), 4326)::geography)",
                    requestingUserId, myLocation.Location.X, myLocation.Location.Y, staleMinutes)
                .AsNoTracking()
                .ToListAsync(ct);

            var extra = globalRaw.Where(l => !nearbyIds.Contains(l.UserId)).ToList();
            nearbyRaw.AddRange(extra);
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("[Discovery] Expanded to global pool: {Total} total users for {UserId}.", nearbyRaw.Count, requestingUserId);
        }

        var nearbyUserIds = nearbyRaw.Select(l => l.UserId).ToList();

        // Users who blocked me, or whom I blocked — exclude from map entirely (both directions)
        var blockRelations = await db.Set<UserBlock>()
            .AsNoTracking()
            .Where(b =>
                (b.BlockerUserId == requestingUserId && nearbyUserIds.Contains(b.BlockedUserId)) ||
                (b.BlockedUserId == requestingUserId && nearbyUserIds.Contains(b.BlockerUserId)))
            .Select(b => b.BlockerUserId == requestingUserId ? b.BlockedUserId : b.BlockerUserId)
            .ToHashSetAsync(ct);

        var profiles = await db.Set<UserDatingProfile>()
            .AsNoTracking()
            .Where(p => nearbyUserIds.Contains(p.UserId))
            .ToListAsync(ct);

        // Users with no TapStatus record are treated as TapIn (the domain default).
        // Only exclude users who explicitly have a TapOut record.
        var tappedOutUserIds = await db.Set<TapStatus>()
            .AsNoTracking()
            .Where(ts => nearbyUserIds.Contains(ts.UserId) && ts.Status == TapStatusEnum.TapOut)
            .Select(ts => ts.UserId)
            .ToHashSetAsync(ct);

        // Exclude users who have logged out (no active device token).
        var loggedInUserIds = await db.Set<UserDevice>()
            .AsNoTracking()
            .Where(d => nearbyUserIds.Contains(d.UserId) && d.IsActive)
            .Select(d => d.UserId)
            .ToHashSetAsync(ct);

        var placeholders = await db.Set<PlaceholderPhoto>()
            .AsNoTracking()
            .Where(pp => pp.IsActive)
            .ToListAsync(ct);

        var existingConnections = await db.Set<Connection>()
            .AsNoTracking()
            .Where(c =>
                (c.SenderUserId == requestingUserId || c.ReceiverUserId == requestingUserId)
                && nearbyUserIds.Contains(c.SenderUserId == requestingUserId ? c.ReceiverUserId : c.SenderUserId))
            .ToListAsync(ct);

        var myConnectionsToday = await db.Set<Connection>()
            .AsNoTracking()
            .CountAsync(c =>
                (c.SenderUserId == requestingUserId || c.ReceiverUserId == requestingUserId)
                && c.ConnectedAt.HasValue && c.ConnectedAt.Value.Date == today, ct);

        // A connection is "active" (blocking) while:
        //   - invitation is still pending, OR
        //   - in the decision phase and neither participant has passed or expired.
        // If anyone passed or the timer expired, the connection is dead and both users are free.
        var iHaveActiveConnection = await db.Set<Connection>()
            .AsNoTracking()
            .AnyAsync(c =>
                (c.SenderUserId == requestingUserId || c.ReceiverUserId == requestingUserId)
                && (c.InvitationStatus == InvitationStatus.Pending
                    || (c.InvitationStatus == InvitationStatus.Accepted
                        && c.ConnectedAt == null
                        && c.SenderConnectionStatus != ParticipantConnectionStatus.Pass
                        && c.ReceiverConnectionStatus != ParticipantConnectionStatus.Pass
                        && c.SenderConnectionStatus != ParticipantConnectionStatus.Expired
                        && c.ReceiverConnectionStatus != ParticipantConnectionStatus.Expired)), ct);

        // Nearby users who are already a party to any active connection — they cannot be connected to.
        var nearbyUsersWithActive = await db.Set<Connection>()
            .AsNoTracking()
            .Where(c =>
                (c.InvitationStatus == InvitationStatus.Pending
                    || (c.InvitationStatus == InvitationStatus.Accepted
                        && c.ConnectedAt == null
                        && c.SenderConnectionStatus != ParticipantConnectionStatus.Pass
                        && c.ReceiverConnectionStatus != ParticipantConnectionStatus.Pass
                        && c.SenderConnectionStatus != ParticipantConnectionStatus.Expired
                        && c.ReceiverConnectionStatus != ParticipantConnectionStatus.Expired))
                && (nearbyUserIds.Contains(c.SenderUserId) || nearbyUserIds.Contains(c.ReceiverUserId)))
            .Select(c => new { c.SenderUserId, c.ReceiverUserId })
            .ToListAsync(ct);

        var nearbyActiveUserIds = nearbyUsersWithActive
            .SelectMany(c => new[] { c.SenderUserId, c.ReceiverUserId })
            .Where(nearbyUserIds.Contains)
            .ToHashSet();

        var results = new List<NearbyUserResult>();

        foreach (var location in nearbyRaw)
        {
            var profile = profiles.FirstOrDefault(p => p.UserId == location.UserId);
            if (profile is null)
            {
                logger.LogDebug("[Discovery] Filtered {UserId}: no dating profile.", location.UserId);
                continue;
            }

            if (tappedOutUserIds.Contains(location.UserId))
            {
                logger.LogDebug("[Discovery] Filtered {UserId}: tapped out.", location.UserId);
                continue;
            }

            if (!loggedInUserIds.Contains(location.UserId))
            {
                logger.LogDebug("[Discovery] Filtered {UserId}: logged out (no active device).", location.UserId);
                continue;
            }

            if (blockRelations.Contains(location.UserId))
            {
                logger.LogDebug("[Discovery] Filtered {UserId}: blocked.", location.UserId);
                continue;
            }

            var theirGender = profile.Gender;
            var theirGenderPreference = new HashSet<string>(profile.GenderPreference, StringComparer.OrdinalIgnoreCase);
            var myGender = myProfile.Gender;

            // Empty GenderPreference means "open to everyone" — skip gender filter in that case.
            // OR logic: show if either side is interested in the other's gender.
            // Strict mutual matching is enforced at connection-request time, not at discovery.
            var iAmInterested = myGenderPreference.Count == 0 || myGenderPreference.Contains(theirGender);
            var theyAreInterested = theirGenderPreference.Count == 0 || theirGenderPreference.Contains(myGender);

            if (!iAmInterested && !theyAreInterested)
            {
                logger.LogDebug("[Discovery] Filtered {UserId}: gender mismatch (me={MyGender} pref={MyPref}, them={TheirGender} pref={TheirPref}).",
                    location.UserId, myGender, string.Join(",", myGenderPreference), theirGender, string.Join(",", theirGenderPreference));
                continue;
            }

            var distanceMeters = CalculateDistance(
                myLocation.Location.Y, myLocation.Location.X,
                location.Location.Y, location.Location.X);
            var distanceMiles = distanceMeters / 1609.34;

            var existingConn = existingConnections.FirstOrDefault(c =>
                c.SenderUserId == location.UserId || c.ReceiverUserId == location.UserId);

            // A dead Accepted connection (decision phase expired/passed with no chat) does not block.
            var deadAccepted = existingConn is { InvitationStatus: InvitationStatus.Accepted }
                && existingConn.ConnectedAt == null
                && existingConn.SenderConnectionStatus != ParticipantConnectionStatus.Pending
                && existingConn.ReceiverConnectionStatus != ParticipantConnectionStatus.Pending;

            var existingConnBlocks = existingConn is not null
                && existingConn.InvitationStatus != InvitationStatus.Rejected
                && existingConn.InvitationStatus != InvitationStatus.Withdrawn
                && existingConn.InvitationStatus != InvitationStatus.Expired
                && !deadAccepted;

            var canSend = !iHaveActiveConnection
                && !nearbyActiveUserIds.Contains(location.UserId)
                && myConnectionsToday < connectionLimit
                && !existingConnBlocks;

            string? cannotConnectReason = canSend ? null
                : iHaveActiveConnection                     ? "YOU_HAVE_ACTIVE_CONNECTION"
                : nearbyActiveUserIds.Contains(location.UserId) ? "THEY_HAVE_ACTIVE_CONNECTION"
                : myConnectionsToday >= connectionLimit     ? "DAILY_LIMIT_REACHED"
                : existingConnBlocks                        ? "ALREADY_CONNECTED"
                : null;

            var placeholder = GetPlaceholder(placeholders, theirGender, location.UserId);

            results.Add(new NearbyUserResult(
                location.UserId,
                MaskName(profile.DisplayName),
                profile.AgeRange,
                theirGender,
                placeholder,
                distanceMiles,
                canSend,
                cannotConnectReason,
                existingConn?.Id,
                existingConn?.InvitationStatus.ToString()
            ));
        }

        logger.LogDebug("[Discovery] Returning {Count} result(s) for user {UserId}.", results.Count, requestingUserId);
        return results.AsReadOnly();
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

    private static string GetPlaceholder(List<PlaceholderPhoto> photos, string gender, string userId)
    {
        var matches = photos.Where(p => p.Gender.Equals(gender, StringComparison.OrdinalIgnoreCase)).ToList();
        if (matches.Count == 0) matches = photos;
        if (matches.Count == 0) return string.Empty;
        var hash = userId.Aggregate(0u, (acc, c) => acc * 31 + c);
        return matches[(int)(hash % (uint)matches.Count)].PhotoUrl;
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
