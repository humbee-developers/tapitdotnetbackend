using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TapitAI.Infrastructure.BackgroundServices;

public class SampleBackgroundService(ILogger<SampleBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Background service running at: {time}", DateTimeOffset.UtcNow);

            // TODO: Replace with actual background work (e.g. email queue, cleanup, etc.)
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }

        logger.LogInformation("Background service stopped.");
    }
}
