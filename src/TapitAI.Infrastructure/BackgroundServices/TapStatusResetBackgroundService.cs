using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TapitAI.Application.DTOs.Dating;
using TapitAI.Domain.Constants;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Enums;
using TapitAI.Domain.Interfaces.Services;
using TapitAI.Infrastructure.Data;

namespace TapitAI.Infrastructure.BackgroundServices;

public class TapStatusResetBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<TapStatusResetBackgroundService> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("[TapStatusReset] Background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(Interval, stoppingToken);
            await ResetExpiredTapOutsAsync(stoppingToken);
        }
    }

    private async Task ResetExpiredTapOutsAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var realTime = scope.ServiceProvider.GetRequiredService<IRealTimeService>();
            var firebase = scope.ServiceProvider.GetRequiredService<IFirebaseService>();

            var expired = await db.Set<TapStatus>()
                .Where(ts => ts.Status == TapStatusEnum.TapOut
                          && ts.AutoTapInAt.HasValue
                          && ts.AutoTapInAt.Value <= DateTime.UtcNow)
                .ToListAsync(ct);

            if (expired.Count == 0) return;

            foreach (var status in expired)
                status.TapIn();

            // Persist before notifying so clients that re-fetch see the new state immediately
            await db.SaveChangesAsync(ct);

            logger.LogInformation("[TapStatusReset] Auto-tapped in {Count} user(s).", expired.Count);

            foreach (var status in expired)
            {
                var dto = new TapStatusDto
                {
                    UserId = status.UserId,
                    Status = TapStatusEnum.TapIn.ToString(),
                    AutoTapInAt = null,
                    TapOutReason = null
                };

                await realTime.SendToUserAsync(status.UserId, HubEvents.TapStatusChanged, dto, ct);

                await firebase.SendToUserAsync(
                    status.UserId,
                    title: "You're back!",
                    body: "Your Tap Out has expired — you're now visible to others.",
                    ct: ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[TapStatusReset] Error resetting expired tap-outs.");
        }
    }
}
