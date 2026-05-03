using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TapitAI.Application.Common.Interfaces;
using TapitAI.Domain.Constants;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Enums;
using TapitAI.Infrastructure.Data;

namespace TapitAI.Infrastructure.BackgroundServices;

public class SpotlightGenerationBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<SpotlightGenerationBackgroundService> logger)
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
                var intervalMinutes = await settings.GetIntAsync(AdminSettingKeys.SpotlightGenerationIntervalMinutes, 60, stoppingToken);

                await GenerateSpotlightsAsync(scope.ServiceProvider, stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in SpotlightGenerationBackgroundService");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task GenerateSpotlightsAsync(IServiceProvider services, CancellationToken ct)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var settings = services.GetRequiredService<IAdminSettingService>();

        var radiusMiles = await settings.GetDoubleAsync(AdminSettingKeys.SpotlightRadiusMiles, 100, ct);
        var maxUsers = await settings.GetIntAsync(AdminSettingKeys.SpotlightMaxUsers, 5, ct);
        var expiryMinutes = await settings.GetIntAsync(AdminSettingKeys.SpotlightExpiryMinutes, 60, ct);

        var tapInUserIds = await db.Set<TapStatus>()
            .Where(ts => ts.Status == TapStatusEnum.TapIn)
            .Select(ts => ts.UserId)
            .ToListAsync(ct);

        var profiledUserIds = await db.Set<UserDatingProfile>()
            .Where(p => tapInUserIds.Contains(p.UserId))
            .Select(p => p.UserId)
            .ToListAsync(ct);

        var userLocations = await db.Set<UserLocation>()
            .Where(ul => profiledUserIds.Contains(ul.UserId) && ul.IsLatest)
            .ToListAsync(ct);

        var placeholders = await db.Set<PlaceholderPhoto>()
            .Where(pp => pp.IsActive).ToListAsync(ct);

        var profiles = await db.Set<UserDatingProfile>()
            .Include(p => p.InterestedGenders)
            .Include(p => p.SelfGenderOption)
            .Where(p => profiledUserIds.Contains(p.UserId))
            .ToListAsync(ct);

        int generated = 0;

        foreach (var watcherLocation in userLocations)
        {
            try
            {
                var watcherId = watcherLocation.UserId;
                var watcherProfile = profiles.FirstOrDefault(p => p.UserId == watcherId);
                if (watcherProfile is null) continue;

                var watcherInterestedGenders = watcherProfile.InterestedGenders
                    .Select(g => g.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var watcherGender = watcherProfile.SelfGenderOption?.Value ?? string.Empty;

                // Expire old active sessions
                var oldSessions = await db.Set<SpotlightSession>()
                    .Where(s => s.WatcherUserId == watcherId && s.IsActive)
                    .ToListAsync(ct);
                oldSessions.ForEach(s => s.Expire());

                // Users already shown to this watcher (permanent exclusion)
                var shownUserIds = await db.Set<SpotlightSessionFeed>()
                    .Where(sf => db.Set<SpotlightSession>()
                        .Where(s => s.WatcherUserId == watcherId)
                        .Select(s => s.Id)
                        .Contains(sf.SpotlightSessionId))
                    .Select(sf => sf.FeaturedUserId)
                    .Distinct()
                    .ToListAsync(ct);

                var radiusMeters = radiusMiles * 1609.34;
                var candidates = userLocations
                    .Where(ul =>
                        ul.UserId != watcherId
                        && !shownUserIds.Contains(ul.UserId))
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

                // If not enough, go global
                if (candidates.Count < maxUsers)
                {
                    var additionalIds = shownUserIds.Concat(candidates.Select(c => c.UserId)).ToHashSet();
                    var globalCandidates = userLocations
                        .Where(ul => ul.UserId != watcherId && !additionalIds.Contains(ul.UserId))
                        .Select(ul => new
                        {
                            ul.UserId,
                            Distance = CalculateDistance(
                                watcherLocation.Location.Y, watcherLocation.Location.X,
                                ul.Location.Y, ul.Location.X)
                        })
                        .OrderBy(x => x.Distance)
                        .Take(maxUsers - candidates.Count)
                        .ToList();
                    candidates.AddRange(globalCandidates);
                }

                // Filter by gender compatibility
                var compatible = candidates
                    .Where(c =>
                    {
                        var cp = profiles.FirstOrDefault(p => p.UserId == c.UserId);
                        if (cp is null) return false;
                        var theirGender = cp.SelfGenderOption?.Value ?? string.Empty;
                        var theirInterested = cp.InterestedGenders.Select(g => g.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
                        return watcherInterestedGenders.Contains(theirGender) && theirInterested.Contains(watcherGender);
                    })
                    .Take(maxUsers)
                    .ToList();

                if (!compatible.Any()) continue;

                var session = SpotlightSession.Create(watcherId, expiryMinutes);
                db.Set<SpotlightSession>().Add(session);
                await db.SaveChangesAsync(ct);

                foreach (var candidate in compatible)
                {
                    var feedItem = SpotlightSessionFeed.Create(session.Id, candidate.UserId);
                    db.Set<SpotlightSessionFeed>().Add(feedItem);
                }

                await db.SaveChangesAsync(ct);
                generated++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to generate spotlight for user {UserId}", watcherLocation.UserId);
            }
        }

        logger.LogInformation("Generated spotlight sessions for {Count} users", generated);
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
