using System.Diagnostics;
using System.Net.Sockets;
using NextWatch.Core.Domain;
using NextWatch.Core.Domain.Entities;

namespace NextWatch.Core.Checks;

public sealed class TcpCheckExecutor : ICheckExecutor
{
    public CheckType Type => CheckType.Tcp;

    public async Task<CheckExecutionResult> ExecuteAsync(MonitorTarget target, CheckDefinition check, CancellationToken cancellationToken)
    {
        var p = CheckParameters.Parse<TcpCheckParams>(check.ParametersJson) ?? new TcpCheckParams();
        var sw = Stopwatch.StartNew();
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(target.Host, p.Port, cancellationToken);
            sw.Stop();
            return new CheckExecutionResult(CheckStatus.Ok, sw.Elapsed.TotalMilliseconds, $"Port {p.Port} open");
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new CheckExecutionResult(CheckStatus.Down, sw.Elapsed.TotalMilliseconds, ex.Message);
        }
    }
}
