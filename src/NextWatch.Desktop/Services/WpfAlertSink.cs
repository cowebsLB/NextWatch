using NextWatch.Core.Alerts;

namespace NextWatch.Desktop.Services;

public sealed class WpfAlertSink(WebhookAlertSink webhook, TrayIconService tray) : IAlertSink
{
    public async Task NotifyAsync(AlertNotification notification, CancellationToken ct)
    {
        tray.ShowBalloon(notification.Title, notification.Body);
        await webhook.NotifyAsync(notification, ct);
    }
}
