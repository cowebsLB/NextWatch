using NextWatch.Core.Domain;
using NextWatch.Core.Domain.Entities;

namespace NextWatch.Core.Checks;

public sealed record CheckExecutionResult(CheckStatus Status, double? LatencyMs, string Message);

public interface ICheckExecutor
{
    Domain.CheckType Type { get; }
    Task<CheckExecutionResult> ExecuteAsync(MonitorTarget target, CheckDefinition check, CancellationToken cancellationToken);
}
