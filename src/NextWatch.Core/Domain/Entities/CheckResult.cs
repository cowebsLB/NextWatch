namespace NextWatch.Core.Domain.Entities;

public sealed class CheckResult
{
    public long Id { get; set; }
    public Guid CheckId { get; set; }
    public CheckDefinition? Check { get; set; }
    public CheckStatus Status { get; set; }
    public double? LatencyMs { get; set; }
    public string? Message { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
