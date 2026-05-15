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
        var settings = await db.Settings.AsNoTracking().OrderBy(s => s.Id).FirstAsync(ct);
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
        var rule = ResolveAlertRule(rules, check.Id);
        var target = await db.Targets.AsNoTracking().FirstAsync(t => t.Id == check.TargetId, ct);

        var supersedeUtc = DateTime.UtcNow;
        foreach (var prior in await db.AlertEvents.Where(e => e.CheckId == check.Id && e.AcknowledgedAtUtc == null)
                     .ToListAsync(ct))
            prior.AcknowledgedAtUtc = supersedeUtc;

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
            ToastEnabled = rule.ToastEnabled,
            PlaySound = rule.SoundEnabled,
            WebhookUrl = ResolveWebhookUrl(rule, settings)
        }, ct);
    }

    public async Task ProcessRepeatsAsync(NextWatchDbContext db, CancellationToken ct)
    {
        var settings = await db.Settings.AsNoTracking().OrderBy(s => s.Id).FirstAsync(ct);
        if (IsMuted(settings))
        {
            logger.LogDebug("Skipping repeat alerts: muted");
            return;
        }

        var nowUtc = DateTime.UtcNow;
        var open = await db.AlertEvents
            .Where(e => e.AcknowledgedAtUtc == null)
            .ToListAsync(ct);

        var allRules = await db.AlertRules.ToListAsync(ct);

        foreach (var evt in open)
        {
            var rule = ResolveAlertRule(allRules, evt.CheckId);
            var repeatMin = rule.RepeatMinutes;
            if (!AlertRepeatSchedule.IsRepeatDue(evt.FiredAtUtc, repeatMin, evt.RepeatCount, nowUtc))
                continue;

            evt.RepeatCount++;
            var check = await db.Checks.Include(c => c.Target).FirstAsync(c => c.Id == evt.CheckId, ct);
            await sink.NotifyAsync(new AlertNotification
            {
                Title = $"NextWatch: {check.Target!.Name} still {evt.Status}",
                Body = evt.Message,
                ToastEnabled = rule.ToastEnabled,
                PlaySound = rule.SoundEnabled,
                WebhookUrl = ResolveWebhookUrl(rule, settings)
            }, ct);
        }

        await db.SaveChangesAsync(ct);
    }

    private static AlertRule ResolveAlertRule(IReadOnlyList<AlertRule> rules, Guid checkId) =>
        rules.FirstOrDefault(r => r.CheckId == checkId)
        ?? rules.FirstOrDefault(r => r.CheckId == null)
        ?? new AlertRule();

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