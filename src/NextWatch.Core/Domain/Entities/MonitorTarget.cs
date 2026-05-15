namespace NextWatch.Core.Domain.Entities;

public sealed class MonitorTarget
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string? Tag { get; set; }
    public DateTime? MuteUntilUtc { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<CheckDefinition> Checks { get; set; } = new List<CheckDefinition>();
}
