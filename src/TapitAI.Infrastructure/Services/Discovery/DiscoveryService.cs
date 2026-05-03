using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Domain.Constants;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Enums;
using TapitAI.Domain.Interfaces.Services;
using TapitAI.Infrastructure.Data;

namespace TapitAI.Infrastructure.Services.Discovery;

public class DiscoveryService(AppDbContext db, IAdminSettingService settings) : IDiscoveryService
{
    public async Task<IReadOnlyList<NearbyUserResult>> GetNearbyUsersAsync(
        string requestingUserId, double radiusMiles, CancellationToken ct = default)
    {
        var myLocation = await db.Set<UserLocation>()
            .AsNoTracking()
            .Where(ul => ul.UserId == requestingUserId && ul.IsLatest)
            .FirstOrDefaultAsync(ct);

        if (myLocation is null) return Array.Empty<NearbyUserResult>();

        var myProfile = await db.Set<UserDatingProfile>()
            .AsNoTracking()
            .Include(p => p.InterestedGenders)
            .Include(p => p.SelfGenderOption)
            .FirstOrDefaultAsync(p => p.UserId == requestingUserId, ct);

        if (myProfile is null) return Array.Empty<NearbyUserResult>();

        var connectionLimit = await settings.GetIntAsync(AdminSettingKeys.ConnectionsPerDayLimit, 3, ct);
        var today = DateTime.UtcNow.Date;
        var radiusMeters = radiusMiles * 1609.34;

        var myInterestedGenderValues = myProfile.InterestedGenders.Select(g => g.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Raw PostGIS query for nearby users
        var nearbyRaw = await db.Set<UserLocation>()
            .FromSqlRaw(@"
                SELECT ul.* FROM ""UserLocations"" ul
                WHERE ul.""IsLatest"" = true
                AND ul.""UserId"" != {0}
                AND ST_DWithin(
                    ul.""Location""::geography,
                    ST_SetSRID(ST_MakePoint({1}, {2}), 4326)::geography,
                    {3}
                )
                ORDER BY ST_Distance(ul.""Location""::geography, ST_SetSRID(ST_MakePoint({1}, {2}), 4326)::geography)",
                requestingUserId, myLocation.Location.X, myLocation.Location.Y, radiusMeters)
            .AsNoTracking()
            .ToListAsync(ct);

        var nearbyUserIds = nearbyRaw.Select(l => l.UserId).ToList();

        var profiles = await db.Set<UserDatingProfile>()
            .AsNoTracking()
            .Include(p => p.AgeRangeOption)
            .Include(p => p.SelfGenderOption)
            .Include(p => p.InterestedGenders)
            .Where(p => nearbyUserIds.Contains(p.UserId))
            .ToListAsync(ct);

        var activeTapUserIds = await db.Set<TapStatus>()
            .AsNoTracking()
            .Where(ts => nearbyUserIds.Contains(ts.UserId) && ts.Status == TapStatusEnum.TapIn)
            .Select(ts => ts.UserId)
            .ToListAsync(ct);

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

        var hasPendingRequest = await db.Set<Connection>()
            .AsNoTracking()
            .AnyAsync(c =>
                (c.SenderUserId == requestingUserId || c.ReceiverUserId == requestingUserId)
                && c.InvitationStatus == InvitationStatus.Pending, ct);

        var results = new List<NearbyUserResult>();

        foreach (var location in nearbyRaw)
        {
            var profile = profiles.FirstOrDefault(p => p.UserId == location.UserId);
            if (profile is null) continue;

            if (!activeTapUserIds.Contains(location.UserId)) continue;

            var theirGender = profile.SelfGenderOption?.Value ?? string.Empty;
            var theirInterestedGenders = profile.InterestedGenders.Select(g => g.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var myGender = myProfile.SelfGenderOption?.Value ?? string.Empty;

            var genderMatch = myInterestedGenderValues.Contains(theirGender) &&
                              theirInterestedGenders.Contains(myGender);

            if (!genderMatch) continue;

            var distanceMeters = CalculateDistance(
                myLocation.Location.Y, myLocation.Location.X,
                location.Location.Y, location.Location.X);
            var distanceMiles = distanceMeters / 1609.34;

            var existingConn = existingConnections.FirstOrDefault(c =>
                c.SenderUserId == location.UserId || c.ReceiverUserId == location.UserId);

            var canSend = !hasPendingRequest
                && myConnectionsToday < connectionLimit
                && (existingConn is null || existingConn.InvitationStatus == InvitationStatus.Rejected
                    || existingConn.InvitationStatus == InvitationStatus.Withdrawn
                    || existingConn.InvitationStatus == InvitationStatus.Expired);

            var placeholder = GetPlaceholder(placeholders, theirGender);

            results.Add(new NearbyUserResult(
                location.UserId,
                MaskName(profile.DisplayName),
                profile.AgeRangeOption?.Value ?? string.Empty,
                theirGender,
                placeholder,
                distanceMiles,
                canSend,
                existingConn?.Id,
                existingConn?.InvitationStatus.ToString()
            ));
        }

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

    private static string GetPlaceholder(List<PlaceholderPhoto> photos, string gender)
    {
        var matches = photos.Where(p => p.Gender.Equals(gender, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!matches.Any()) matches = photos;
        if (!matches.Any()) return string.Empty;
        return matches[Random.Shared.Next(matches.Count)].PhotoUrl;
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
