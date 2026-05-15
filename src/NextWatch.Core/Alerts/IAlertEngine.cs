using NextWatch.Core.Data;
using NextWatch.Core.Domain;
using NextWatch.Core.Domain.Entities;

namespace NextWatch.Core.Alerts;

public interface IAlertEngine
{
    Task ProcessStatusChangeAsync(NextWatchDbContext db, CheckDefinition check, CheckStatus status, string message, CancellationToken ct);
    Task ProcessRepeatsAsync(NextWatchDbContext db, CancellationToken ct);
}

public sealed class AlertNotification
{
    public required string Title { get; init; }
    public required string Body { get; init; }
    /// <summary>Tray balloon / toast (webhooks still honor <see cref="WebhookUrl"/>).</summary>
    public bool ToastEnabled { get; init; } = true;
    public bool PlaySound { get; init; }
    public string? WebhookUrl { get; init; }
}

public interface IAlertSink
{
    Task NotifyAsync(AlertNotification notification, CancellationToken ct);
}
