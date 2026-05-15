using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NextWatch.Core.Data;
using NextWatch.Core.Domain;
using NextWatch.Core.Domain.Entities;

namespace NextWatch.Core.Alerts;

public sealed class AlertEngine(IAlertSink sink, ILogger<AlertEngine> logger) : IAlertEngine
{
    public async Task ProcessStatusChangeAsync(NextWatchDbContext db, CheckDefinition check, CheckStatus status, string message, CancellationToken ct)
    {
        var settings = await db.Settings.AsNoTracking().FirstAsync(ct);
        if (IsMuted(settings))
        {
            logger.LogDebug("Skipping alert: muted");
            return;
        }

        if (status is not (CheckStatus.Down or CheckStatus.Warn))
        {
            logger.LogDebug("Skipping alert: status {Status} does not notify", status);
            return;
        }

        var rules = await db.AlertRules.Where(r => r.CheckId == check.Id || r.CheckId == null).ToListAsync(ct);
        var rule = rules.FirstOrDefault() ?? new AlertRule();
        var target = await db.Targets.AsNoTracking().FirstAsync(t => t.Id == check.TargetId, ct);

        var evt = new AlertEvent
        {
            CheckId = check.Id,
            Status = status,
            Message = message,
            FiredAtUtc = DateTime.UtcNow
        };
        db.AlertEvents.Add(evt);
        await db.SaveChangesAsync(ct);

        await sink.NotifyAsync(new AlertNotification
        {
            Title = $"NextWatch: {target.Name} is {status}",
            Body = message,
            PlaySound = rule.SoundEnabled,
            WebhookUrl = ResolveWebhookUrl(rule, settings)
        }, ct);
    }

    public async Task ProcessRepeatsAsync(NextWatchDbContext db, CancellationToken ct)
    {
        var settings = await db.Settings.AsNoTracking().FirstAsync(ct);
        if (IsMuted(settings))
        {
            logger.LogDebug("Skipping repeat alerts: muted");
            return;
        }

        var cutoff = DateTime.UtcNow;
        var open = await db.AlertEvents
            .Where(e => e.AcknowledgedAtUtc == null)
            .ToListAsync(ct);

        foreach (var evt in open)
        {
            var rule = await db.AlertRules.FirstOrDefaultAsync(r => r.CheckId == evt.CheckId, ct);
            var repeatMin = rule?.RepeatMinutes ?? 15;
            var lastRepeat = evt.FiredAtUtc.AddMinutes(repeatMin * evt.RepeatCount);
            if (lastRepeat > cutoff)
                continue;

            evt.RepeatCount++;
            var check = await db.Checks.Include(c => c.Target).FirstAsync(c => c.Id == evt.CheckId, ct);
            await sink.NotifyAsync(new AlertNotification
            {
                Title = $"NextWatch: {check.Target!.Name} still {evt.Status}",
                Body = evt.Message,
                PlaySound = rule?.SoundEnabled ?? false,
                WebhookUrl = ResolveWebhookUrl(rule, settings)
            }, ct);
        }

        await db.SaveChangesAsync(ct);
    }

    internal static string? ResolveWebhookUrl(AlertRule? rule, AppSettings settings) =>
        rule is { WebhookEnabled: true } ? rule.WebhookUrl ?? settings.DefaultWebhookUrl : null;

    private static bool IsMuted(AppSettings settings) =>
        settings.AlertsMutedUntilRestart ||
        (settings.AlertsMutedUntilUtc is { } until && until > DateTime.UtcNow);
}

public sealed class WebhookAlertSink(IHttpClientFactory httpClientFactory, ILogger<WebhookAlertSink> logger) : IAlertSink
{
    public async Task NotifyAsync(AlertNotification notification, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(notification.WebhookUrl))
        {
            try
            {
                var client = httpClientFactory.CreateClient();
                await client.PostAsJsonAsync(notification.WebhookUrl, new
                {
                    text = $"{notification.Title}\n{notification.Body}"
                }, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Webhook failed");
            }
        }
    }
}