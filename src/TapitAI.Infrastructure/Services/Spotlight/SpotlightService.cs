using Microsoft.EntityFrameworkCore;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Domain.Constants;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Enums;
using TapitAI.Domain.Interfaces.Services;
using TapitAI.Infrastructure.Data;

namespace TapitAI.Infrastructure.Services.Spotlight;

public class SpotlightService(AppDbContext db, IAdminSettingService settings) : ISpotlightService
{
    public async Task<SpotlightSession?> GenerateForUserAsync(string userId, CancellationToken ct = default)
    {
        var radiusMiles  = await settings.GetDoubleAsync(AdminSettingKeys.SpotlightRadiusMiles, 100, ct);
        var maxUsers     = await settings.GetIntAsync(AdminSettingKeys.SpotlightMaxUsers, 5, ct);
        var expiryMinutes = await settings.GetIntAsync(AdminSettingKeys.SpotlightExpiryMinutes, 60, ct);

        var watcherProfile = await db.Set<UserDatingProfile>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (watcherProfile is null) return null;

        var watcherLocation = await db.Set<UserLocation>()
            .AsNoTracking()
            .FirstOrDefaultAsync(ul => ul.UserId == userId && ul.IsLatest, ct);
        if (watcherLocation is null) return null;

        // Expire any existing active sessions first
        var oldSessions = await db.Set<SpotlightSession>()
            .Where(s => s.WatcherUserId == userId && s.IsActive)
            .ToListAsync(ct);
        oldSessions.ForEach(s => s.Expire());

        var tapInUserIds = await db.Set<TapStatus>()
            .AsNoTracking()
            .Where(ts => ts.Status == TapStatusEnum.TapIn)
            .Select(ts => ts.UserId)
            .ToListAsync(ct);

        var shownUserIds = await db.Set<SpotlightSessionFeed>()
            .AsNoTracking()
            .Where(sf => db.Set<SpotlightSession>()
                .Where(s => s.WatcherUserId == userId)
                .Select(s => s.Id)
                .Contains(sf.SpotlightSessionId))
            .Select(sf => sf.FeaturedUserId)
            .Distinct()
            .ToListAsync(ct);

        var blockedUserIds = await db.Set<UserBlock>()
            .AsNoTracking()
            .Where(b => b.BlockerUserId == userId || b.BlockedUserId == userId)
            .Select(b => b.BlockerUserId == userId ? b.BlockedUserId : b.BlockerUserId)
            .ToHashSetAsync(ct);

        var candidateUserIds = await db.Set<UserDatingProfile>()
            .AsNoTracking()
            .Where(p => tapInUserIds.Contains(p.UserId) && p.UserId != userId
                        && p.IsSpotlightVisible && !blockedUserIds.Contains(p.UserId))
            .Select(p => p.UserId)
            .ToListAsync(ct);

        var staleThreshold = DateTime.UtcNow.AddMinutes(-30);
        var allLocations = await db.Set<UserLocation>()
            .AsNoTracking()
            .Where(ul => candidateUserIds.Contains(ul.UserId) && ul.IsLatest && ul.CreatedAt >= staleThreshold)
            .ToListAsync(ct);

        var allProfiles = await db.Set<UserDatingProfile>()
            .AsNoTracking()
            .Where(p => candidateUserIds.Contains(p.UserId))
            .ToListAsync(ct);

        var radiusMeters = radiusMiles * 1609.34;
        var watcherInterestedGenders = new HashSet<string>(watcherProfile.GenderPreference, StringComparer.OrdinalIgnoreCase);
        var watcherGender = watcherProfile.Gender;

        var nearbyUnseen = allLocations
            .Where(ul => !shownUserIds.Contains(ul.UserId))
            .Select(ul => new
            {
                ul.UserId,
                Distance = CalculateDistance(
                    watcherLocation.Location.Y, watcherLocation.Location.X,
                    ul.Location.Y, ul.Location.X)
            })
            .Where(x => x.Distance <= radiusMeters)
            .OrderBy(x => x.Distance)
            .Take(maxUsers * 3)
            .ToList();

        // Fall back to global pool if not enough nearby
        if (nearbyUnseen.Count < maxUsers)
        {
            var excludeIds = shownUserIds.Concat(nearbyUnseen.Select(c => c.UserId)).ToHashSet();
            var global = allLocations
                .Where(ul => !excludeIds.Contains(ul.UserId))
                .Select(ul => new
                {
                    ul.UserId,
                    Distance = CalculateDistance(
                        watcherLocation.Location.Y, watcherLocation.Location.X,
                        ul.Location.Y, ul.Location.X)
                })
                .OrderBy(x => x.Distance)
                .Take(maxUsers - nearbyUnseen.Count)
                .ToList();
            nearbyUnseen.AddRange(global);
        }

        // Gender compatibility filter — OR logic, empty preference = open to everyone
        var compatible = nearbyUnseen
            .Where(c =>
            {
                var cp = allProfiles.FirstOrDefault(p => p.UserId == c.UserId);
                if (cp is null) return false;
                var iAmInterested = watcherInterestedGenders.Count == 0
                    || watcherInterestedGenders.Contains(cp.Gender);
                var theirInterested = new HashSet<string>(cp.GenderPreference, StringComparer.OrdinalIgnoreCase);
                var theyAreInterested = theirInterested.Count == 0
                    || theirInterested.Contains(watcherGender);
                return iAmInterested || theyAreInterested;
            })
            .Take(maxUsers)
            .ToList();

        if (compatible.Count == 0) return null;

        var session = SpotlightSession.Create(userId, expiryMinutes);
        db.Set<SpotlightSession>().Add(session);
        await db.SaveChangesAsync(ct);

        foreach (var candidate in compatible)
            db.Set<SpotlightSessionFeed>().Add(SpotlightSessionFeed.Create(session.Id, candidate.UserId));

        await db.SaveChangesAsync(ct);
        return session;
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
}
