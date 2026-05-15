using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NextWatch.Core.Infrastructure;

namespace NextWatch.Core.Data;

public sealed class DatabaseInitializer(
    IServiceScopeFactory scopeFactory,
    NextWatchRuntimeOptions runtime,
    ILogger<DatabaseInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NextWatchDbContext>();
        var pending = await db.Database.GetPendingMigrationsAsync(cancellationToken);
        if (pending.Any())
        {
            await db.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Database migrated");
        }
        else
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
            logger.LogInformation("Database ensured (run dotnet ef migrations add when ASP.NET 8 runtime is available)");
        }

        var settings = await db.Settings.FirstAsync(cancellationToken);
        settings.PortableDataPath = runtime.PortableDataPath;
        settings.PortableDataDirectory = runtime.PortableDataDirectory;
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
