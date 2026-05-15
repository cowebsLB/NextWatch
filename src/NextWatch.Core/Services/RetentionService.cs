using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NextWatch.Core.Data;

namespace NextWatch.Core.Services;

public sealed class RetentionService(IServiceScopeFactory scopeFactory, ILogger<RetentionService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PruneAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Retention prune failed");
            }

            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }

    private async Task PruneAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NextWatchDbContext>();
        var settings = await db.Settings.AsNoTracking().FirstAsync(ct);
        var cutoff = DateTime.UtcNow.AddDays(-settings.RetentionDays);
        var old = await db.Results.Where(r => r.TimestampUtc < cutoff).ExecuteDeleteAsync(ct);
        if (old > 0)
            logger.LogInformation("Pruned {Count} check results older than {Days} days", old, settings.RetentionDays);
    }
}
