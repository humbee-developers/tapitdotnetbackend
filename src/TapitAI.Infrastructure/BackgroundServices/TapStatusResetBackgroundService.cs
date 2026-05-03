using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TapitAI.Domain.Constants;
using TapitAI.Domain.Entities;
using TapitAI.Domain.Enums;
using TapitAI.Domain.Interfaces.Services;
using TapitAI.Infrastructure.Data;

namespace TapitAI.Infrastructure.BackgroundServices;

public class TapStatusResetBackgroundService(IServiceScopeFactory scopeFactory, ILogger<TapStatusResetBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
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

            var expiredTapOuts = await db.Set<TapStatus>()
                .Where(ts => ts.Status == TapStatusEnum.TapOut
                          && ts.AutoTapInAt.HasValue
                          && ts.AutoTapInAt.Value <= DateTime.UtcNow)
                .ToListAsync(ct);

            foreach (var status in expiredTapOuts)
            {
                status.TapIn();
                await realTime.SendToUserAsync(status.UserId, HubEvents.TapStatusChanged, new
                {
                    UserId = status.UserId,
                    Status = TapStatusEnum.TapIn.ToString(),
                    AutoTapInAt = (DateTime?)null
                }, ct);

                await firebase.SendToUserAsync(status.UserId, "You're back!",
                    "Your TapOut has expired — you're now TapIn and visible to others.", ct: ct);
            }

            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in TapStatusResetBackgroundService");
        }
    }
}
