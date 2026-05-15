namespace NextWatch.Core.Alerts;

public sealed class NullAlertSink : IAlertSink
{
    public Task NotifyAsync(AlertNotification notification, CancellationToken ct) => Task.CompletedTask;
}
