using System.Diagnostics;
using System.Net.NetworkInformation;
using NextWatch.Core.Domain;
using NextWatch.Core.Domain.Entities;

namespace NextWatch.Core.Checks;

public sealed class PingCheckExecutor : ICheckExecutor
{
    public CheckType Type => CheckType.Ping;

    public async Task<CheckExecutionResult> ExecuteAsync(MonitorTarget target, CheckDefinition check, CancellationToken cancellationToken)
    {
        using var ping = new Ping();
        var sw = Stopwatch.StartNew();
        try
        {
            var reply = await ping.SendPingAsync(target.Host, 5000);
            sw.Stop();
            if (reply.Status == IPStatus.Success)
                return new CheckExecutionResult(CheckStatus.Ok, sw.Elapsed.TotalMilliseconds,
                    $"ICMP RTT {reply.RoundtripTime} ms · check duration {sw.Elapsed.TotalMilliseconds:F0} ms");
            return new CheckExecutionResult(CheckStatus.Down, sw.Elapsed.TotalMilliseconds, reply.Status.ToString());
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new CheckExecutionResult(CheckStatus.Down, sw.Elapsed.TotalMilliseconds, ex.Message);
        }
    }
}
