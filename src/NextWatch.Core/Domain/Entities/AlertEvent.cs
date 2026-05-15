namespace NextWatch.Core.Domain.Entities;

public sealed class AlertEvent
{
    public long Id { get; set; }
    public Guid CheckId { get; set; }
    public CheckStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime FiredAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedAtUtc { get; set; }
    public int RepeatCount { get; set; }
}
