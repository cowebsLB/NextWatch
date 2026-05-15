using NextWatch.Core.Domain;

namespace NextWatch.Core.Domain.Entities;

public sealed class CheckDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TargetId { get; set; }
    public MonitorTarget? Target { get; set; }
    public Domain.CheckType Type { get; set; }
    public string? ParametersJson { get; set; }
    public int IntervalSeconds { get; set; } = 60;
    public bool IsEnabled { get; set; } = true;
    public int WarnThreshold { get; set; }
    public int DownThreshold { get; set; } = 3;
    public DateTime? NextRunUtc { get; set; }
    public CheckStatus LastStatus { get; set; } = CheckStatus.Unknown;
    public int ConsecutiveFailures { get; set; }
    public int ConsecutiveSuccesses { get; set; }

    public ICollection<CheckResult> Results { get; set; } = new List<CheckResult>();
    public ICollection<AlertRule> AlertRules { get; set; } = new List<AlertRule>();
}
