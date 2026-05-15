using System.Diagnostics;
using System.Runtime.Versioning;
using NextWatch.Core.Domain;
using NextWatch.Core.Domain.Entities;
using NextWatch.Core.Snmp;

namespace NextWatch.Core.Checks;

public sealed class BandwidthCheckExecutor : ICheckExecutor
{
    private static readonly Dictionary<string, (long inOctets, long outOctets, DateTime at)> SnmpCache = new();
    private static readonly object CacheLock = new();

    public CheckType Type => CheckType.Bandwidth;

    public async Task<CheckExecutionResult> ExecuteAsync(MonitorTarget target, CheckDefinition check, CancellationToken cancellationToken)
    {
        var p = CheckParameters.Parse<BandwidthCheckParams>(check.ParametersJson) ?? new BandwidthCheckParams();
        var sw = Stopwatch.StartNew();
        try
        {
            if (p.UseSnmp)
            {
                var snmp = CheckParameters.Parse<SnmpCheckParams>(check.ParametersJson);
                var community = snmp?.Community ?? "public";
                var counters = await SnmpClient.GetInterfaceCountersAsync(target.Host, community, snmp?.Port ?? 161, p.SnmpIfIndex, cancellationToken);
                sw.Stop();
                if (counters is null)
                    return new CheckExecutionResult(CheckStatus.Down, sw.Elapsed.TotalMilliseconds, "SNMP counters unavailable");

                var key = $"{check.Id}:{p.SnmpIfIndex}";
                lock (CacheLock)
                {
                    if (SnmpCache.TryGetValue(key, out var prev))
                    {
                        var seconds = (DateTime.UtcNow - prev.at).TotalSeconds;
                        if (seconds > 0)
                        {
                            var inBps = (counters.Value.inOctets - prev.inOctets) * 8 / seconds;
                            var outBps = (counters.Value.outOctets - prev.outOctets) * 8 / seconds;
                            SnmpCache[key] = (counters.Value.inOctets, counters.Value.outOctets, DateTime.UtcNow);
                            return new CheckExecutionResult(CheckStatus.Ok, sw.Elapsed.TotalMilliseconds,
                                $"In: {FormatBps(inBps)} Out: {FormatBps(outBps)}");
                        }
                    }
                    SnmpCache[key] = (counters.Value.inOctets, counters.Value.outOctets, DateTime.UtcNow);
                }
                return new CheckExecutionResult(CheckStatus.Ok, sw.Elapsed.TotalMilliseconds, "Collecting baseline");
            }

            if (!OperatingSystem.IsWindows())
            {
                sw.Stop();
                return new CheckExecutionResult(CheckStatus.Down, sw.Elapsed.TotalMilliseconds,
                    "Bandwidth via performance counters requires Windows");
            }

            return await CollectViaPerfCountersAsync(p, sw, cancellationToken);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new CheckExecutionResult(CheckStatus.Down, sw.Elapsed.TotalMilliseconds, ex.Message);
        }
    }

    [SupportedOSPlatform("windows")]
    private static async Task<CheckExecutionResult> CollectViaPerfCountersAsync(
        BandwidthCheckParams p,
        Stopwatch sw,
        CancellationToken cancellationToken)
    {
        var cat = new PerformanceCounterCategory("Network Interface");
        var instance = string.IsNullOrEmpty(p.InterfaceName)
            ? cat.GetInstanceNames().FirstOrDefault(n => n != "MS TCP Loopback interface" && !n.Contains("isatap", StringComparison.OrdinalIgnoreCase))
            : p.InterfaceName;
        if (instance is null)
            return new CheckExecutionResult(CheckStatus.Down, sw.Elapsed.TotalMilliseconds, "No NIC found");

        using var sent = new PerformanceCounter("Network Interface", "Bytes Sent/sec", instance);
        using var received = new PerformanceCounter("Network Interface", "Bytes Received/sec", instance);
        sent.NextValue();
        received.NextValue();
        await Task.Delay(500, cancellationToken);
        var sentBps = sent.NextValue() * 8;
        var recvBps = received.NextValue() * 8;
        sw.Stop();
        return new CheckExecutionResult(CheckStatus.Ok, sw.Elapsed.TotalMilliseconds,
            $"In: {FormatBps(recvBps)} Out: {FormatBps(sentBps)}");
    }

    private static string FormatBps(double bps) =>
        bps >= 1_000_000 ? $"{bps / 1_000_000:F1} Mbps" :
        bps >= 1_000 ? $"{bps / 1_000:F1} Kbps" : $"{bps:F0} bps";
}
