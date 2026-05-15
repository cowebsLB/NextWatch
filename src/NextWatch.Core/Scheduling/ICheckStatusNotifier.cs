using NextWatch.Core.Domain;
using NextWatch.Core.Domain.Entities;

namespace NextWatch.Core.Scheduling;

public sealed class CheckStatusChangedEventArgs : EventArgs
{
    public required Guid CheckId { get; init; }
    public required Guid TargetId { get; init; }
    public required CheckStatus Status { get; init; }
    public required string Message { get; init; }
    public DateTime TimestampUtc { get; init; }
}

public interface ICheckStatusNotifier
{
    event EventHandler<CheckStatusChangedEventArgs>? StatusChanged;
    void Publish(CheckStatusChangedEventArgs args);
}

public sealed class InProcessCheckStatusNotifier : ICheckStatusNotifier
{
    public event EventHandler<CheckStatusChangedEventArgs>? StatusChanged;

    public void Publish(CheckStatusChangedEventArgs args) =>
        StatusChanged?.Invoke(this, args);
}
