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

        // Users without a TapStatus record are TapIn by default — only exclude explicit TapOut
        var tappedOutUserIds = await db.Set<TapStatus>()
            .AsNoTracking()
            .Where(ts => ts.Status == TapStatusEnum.TapOut)
            .Select(ts => ts.UserId)
            .ToHashSetAsync(ct);

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
            .Where(p => p.UserId != userId
                        && p.IsSpotlightVisible
                        && !tappedOutUserIds.Contains(p.UserId)
                        && !blockedUserIds.Contains(p.UserId))
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

        var allWithDistance = allLocations
            .Select(ul => new
            {
                ul.UserId,
                Distance = CalculateDistance(
                    watcherLocation.Location.Y, watcherLocation.Location.X,
                    ul.Location.Y, ul.Location.X)
            })
            .OrderBy(x => x.Distance)
            .ToList();

        // Priority 1: nearby + unseen
        var pool = allWithDistance
            .Where(x => !shownUserIds.Contains(x.UserId) && x.Distance <= radiusMeters)
            .ToList();

        // Priority 2: not enough → add global unseen (no distance limit)
        if (pool.Count < maxUsers)
        {
            var seenInPool = pool.Select(x => x.UserId).ToHashSet();
            var globalUnseen = allWithDistance
                .Where(x => !shownUserIds.Contains(x.UserId) && !seenInPool.Contains(x.UserId))
                .ToList();
            pool.AddRange(globalUnseen);
        }

        // Priority 3: still not enough → add previously shown users (drop seen filter entirely)
        if (pool.Count < maxUsers)
        {
            var seenInPool = pool.Select(x => x.UserId).ToHashSet();
            var shown = allWithDistance
                .Where(x => !seenInPool.Contains(x.UserId))
                .ToList();
            pool.AddRange(shown);
        }

        // Gender compatibility filter — OR logic, empty preference = open to everyone
        var compatible = pool
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

        // Final fallback: still not enough — fill remaining slots with completely random users,
        // no filters at all (no gender, no distance, no tap-out, no block, no seen-before)
        if (compatible.Count < maxUsers)
        {
            var filledIds = compatible.Select(x => x.UserId).ToHashSet();
            var needed = maxUsers - compatible.Count;

            var randomUserIds = await db.Set<UserDatingProfile>()
                .AsNoTracking()
                .Where(p => p.UserId != userId && !filledIds.Contains(p.UserId))
                .Select(p => p.UserId)
                .ToListAsync(ct);

            var randomPick = randomUserIds
                .OrderBy(_ => Random.Shared.Next())
                .Take(needed)
                .Select(id => new { UserId = id, Distance = 0.0 });

            compatible.AddRange(randomPick);
        }

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
