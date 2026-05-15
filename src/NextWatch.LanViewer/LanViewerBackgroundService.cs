using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NextWatch.Core.Data;

namespace NextWatch.LanViewer;

public sealed class LanViewerBackgroundService(
    IServiceProvider provider,
    LanViewerHost host,
    ILogger<LanViewerBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<NextWatchDbContext>();
                var settings = await db.Settings.AsNoTracking().FirstAsync(stoppingToken);
                if (settings.LanViewerEnabled)
                {
                    await host.StartAsync(provider, settings.LanViewerPort, settings.LanSharedSecretHash, stoppingToken);
                }
                else
                {
                    await host.StopAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "LAN viewer service error");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
