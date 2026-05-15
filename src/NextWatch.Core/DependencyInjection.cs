using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NextWatch.Core.Alerts;
using NextWatch.Core.Checks;
using NextWatch.Core.Data;
using NextWatch.Core.Infrastructure;
using NextWatch.Core.Scheduling;
using NextWatch.Core.Services;
using Serilog;

namespace NextWatch.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddNextWatchCore(this IServiceCollection services, bool portable, string? portablePath, Action<IServiceCollection>? configureAlerts = null)
    {
        var dbPath = NextWatchPaths.GetDatabasePath(portable, portablePath);
        var logsPath = NextWatchPaths.GetLogsDirectory(portable, portablePath);
        Directory.CreateDirectory(logsPath);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(Path.Combine(logsPath, "nextwatch-.log"), rollingInterval: RollingInterval.Day)
            .CreateLogger();

        services.AddLogging(b => b.AddSerilog(dispose: true));

        services.AddDbContext<NextWatchDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));

        services.AddHttpClient("NextWatchChecks", c => c.Timeout = TimeSpan.FromSeconds(30));
        services.AddHttpClient();

        services.AddSingleton<ICheckStatusNotifier, InProcessCheckStatusNotifier>();
        services.AddSingleton<ConfigExportService>();
        services.AddSingleton<DiscoveryService>();
        services.AddSingleton<UpdateCheckerService>();
        services.AddSingleton<ReportExportService>();
        services.AddSingleton<DiagnosticsExportService>();

        services.AddSingleton<ICheckExecutor, PingCheckExecutor>();
        services.AddSingleton<ICheckExecutor, HttpCheckExecutor>();
        services.AddSingleton<ICheckExecutor, TcpCheckExecutor>();
        services.AddSingleton<ICheckExecutor, SslCheckExecutor>();
        services.AddSingleton<ICheckExecutor, DnsCheckExecutor>();
        services.AddSingleton<ICheckExecutor, SnmpCheckExecutor>();
        services.AddSingleton<ICheckExecutor, BandwidthCheckExecutor>();

        services.AddSingleton<WebhookAlertSink>();
        configureAlerts?.Invoke(services);
        if (!services.Any(d => d.ServiceType == typeof(IAlertSink)))
            services.AddSingleton<IAlertSink, NullAlertSink>();
        services.AddSingleton<IAlertEngine, AlertEngine>();

        services.AddHostedService<DatabaseInitializer>();
        services.AddHostedService<CheckSchedulerService>();
        services.AddHostedService<RetentionService>();
        services.AddHostedService<AlertRepeatService>();

        return services;
    }
}
