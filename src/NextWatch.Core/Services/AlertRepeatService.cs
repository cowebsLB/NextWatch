using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NextWatch.Core.Alerts;
using NextWatch.Core.Data;

namespace NextWatch.Core.Services;

public sealed class AlertRepeatService(IServiceScopeFactory scopeFactory, ILogger<AlertRepeatService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<NextWatchDbContext>();
                var engine = scope.ServiceProvider.GetRequiredService<IAlertEngine>();
                await engine.ProcessRepeatsAsync(db, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Alert repeat loop failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
