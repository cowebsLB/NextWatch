using System.Diagnostics;
using NextWatch.Core.Domain;
using NextWatch.Core.Domain.Entities;
using NextWatch.Core.Snmp;

namespace NextWatch.Core.Checks;

public sealed class SnmpCheckExecutor : ICheckExecutor
{
    public CheckType Type => CheckType.Snmp;

    public async Task<CheckExecutionResult> ExecuteAsync(MonitorTarget target, CheckDefinition check, CancellationToken cancellationToken)
    {
        var p = CheckParameters.Parse<SnmpCheckParams>(check.ParametersJson) ?? new SnmpCheckParams();
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await SnmpClient.QuerySysUpTimeAsync(target.Host, p.Community, p.Port, cancellationToken);
            sw.Stop();
            return result
                ? new CheckExecutionResult(CheckStatus.Ok, sw.Elapsed.TotalMilliseconds, "SNMP responding")
                : new CheckExecutionResult(CheckStatus.Down, sw.Elapsed.TotalMilliseconds, "SNMP timeout");
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new CheckExecutionResult(CheckStatus.Down, sw.Elapsed.TotalMilliseconds, ex.Message);
        }
    }
}
