using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NextWatch.Core.Data;

public sealed class DatabaseInitializer(IServiceScopeFactory scopeFactory, ILogger<DatabaseInitializer> logger) : IHostedService
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
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
