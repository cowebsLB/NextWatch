using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NextWatch.Core.Alerts;
using NextWatch.Core.Checks;
using NextWatch.Core.Data;
using NextWatch.Core.Domain;
using NextWatch.Core.Domain.Entities;

namespace NextWatch.Core.Scheduling;

public sealed class CheckSchedulerService(
    IServiceScopeFactory scopeFactory,
    IEnumerable<ICheckExecutor> executors,
    ICheckStatusNotifier notifier,
    IAlertEngine alertEngine,
    ILogger<CheckSchedulerService> logger) : BackgroundService
{
    private const int MaxParallel = 20;
    private readonly Dictionary<CheckType, ICheckExecutor> _executorMap = executors.ToDictionary(e => e.Type);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Check scheduler started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunDueChecksAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Scheduler loop error");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task RunDueChecksAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NextWatchDbContext>();
        var settings = await db.Settings.AsNoTracking().OrderBy(s => s.Id).FirstAsync(ct);
        if (settings.MonitoringPaused)
            return;

        var now = DateTime.UtcNow;
        var due = await db.Checks
            .Include(c => c.Target)
            .Where(c => c.IsEnabled && c.Target!.IsEnabled)
            .Where(c => c.NextRunUtc == null || c.NextRunUtc <= now)
            .OrderBy(c => c.NextRunUtc)
            .Take(MaxParallel)
            .ToListAsync(ct);

        if (due.Count == 0)
            return;

        await Parallel.ForEachAsync(due, new ParallelOptions { MaxDegreeOfParallelism = MaxParallel, CancellationToken = ct },
            async (check, token) => await ExecuteOneCheckAsync(check.Id, token));
    }

    private async Task ExecuteOneCheckAsync(Guid checkId, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NextWatchDbContext>();
        var check = await db.Checks.Include(c => c.Target).FirstOrDefaultAsync(c => c.Id == checkId, ct);
        if (check?.Target is null || !check.IsEnabled)
            return;

        if (check.Target.MuteUntilUtc is { } mute && mute > DateTime.UtcNow)
        {
            ScheduleNext(check, jitter: true);
            await db.SaveChangesAsync(ct);
            return;
        }

        if (!_executorMap.TryGetValue(check.Type, out var executor))
        {
            logger.LogWarning("No executor for check type {Type}", check.Type);
            return;
        }

        var previousStatus = check.LastStatus;
        var raw = await executor.ExecuteAsync(check.Target, check, ct);
        var status = ApplyHysteresis(check, raw.Status);

        var result = new CheckResult
        {
            CheckId = check.Id,
            Status = status,
            LatencyMs = raw.LatencyMs,
            Message = raw.Message,
            TimestampUtc = DateTime.UtcNow
        };
        db.Results.Add(result);
        check.LastStatus = status;
        ScheduleNext(check, jitter: true);

        if (status == CheckStatus.Ok)
        {
            var openForCheck = await db.AlertEvents.Where(e => e.CheckId == check.Id && e.AcknowledgedAtUtc == null)
                .ToListAsync(ct);
            foreach (var e in openForCheck)
                e.AcknowledgedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Check {CheckType} on target '{TargetName}' ({Host}): {Status} ({LatencyMs:F0} ms) — {Message}",
            check.Type,
            check.Target.Name,
            check.Target.Host,
            status,
            raw.LatencyMs,
            raw.Message);

        notifier.Publish(new CheckStatusChangedEventArgs
        {
            CheckId = check.Id,
            TargetId = check.TargetId,
            Status = status,
            Message = raw.Message,
            TimestampUtc = result.TimestampUtc
        });

        if (AlertIncidentTriggers.ShouldOpenNewIncident(previousStatus, status))
            await alertEngine.ProcessStatusChangeAsync(db, check, status, raw.Message, ct);
    }

    public static CheckStatus ApplyHysteresis(CheckDefinition check, CheckStatus raw)
    {
        if (raw is CheckStatus.Ok or CheckStatus.Unknown)
        {
            check.ConsecutiveFailures = 0;
            check.ConsecutiveSuccesses++;
            if (check.LastStatus is CheckStatus.Down or CheckStatus.Warn && check.ConsecutiveSuccesses < 2)
                return check.LastStatus;
            return CheckStatus.Ok;
        }

        check.ConsecutiveSuccesses = 0;
        check.ConsecutiveFailures++;
        if (raw == CheckStatus.Warn)
            return check.ConsecutiveFailures >= check.DownThreshold ? CheckStatus.Down : CheckStatus.Warn;
        return check.ConsecutiveFailures >= check.DownThreshold ? CheckStatus.Down : check.LastStatus == CheckStatus.Unknown ? CheckStatus.Down : check.LastStatus;
    }

    private static void ScheduleNext(CheckDefinition check, bool jitter)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(15, check.IntervalSeconds));
        if (jitter)
        {
            var jitterPct = Random.Shared.NextDouble() * 0.2 - 0.1;
            interval = TimeSpan.FromSeconds(interval.TotalSeconds * (1 + jitterPct));
        }
        check.NextRunUtc = DateTime.UtcNow.Add(interval);
    }
}
