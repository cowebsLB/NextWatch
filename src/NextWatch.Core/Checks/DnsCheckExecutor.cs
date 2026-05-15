using System.Diagnostics;
using System.Net;
using NextWatch.Core.Domain;
using NextWatch.Core.Domain.Entities;

namespace NextWatch.Core.Checks;

public sealed class DnsCheckExecutor : ICheckExecutor
{
    public CheckType Type => CheckType.Dns;

    public async Task<CheckExecutionResult> ExecuteAsync(MonitorTarget target, CheckDefinition check, CancellationToken cancellationToken)
    {
        var p = CheckParameters.Parse<DnsCheckParams>(check.ParametersJson) ?? new DnsCheckParams { Hostname = target.Host };
        var sw = Stopwatch.StartNew();
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(p.Hostname);
            sw.Stop();
            if (addresses.Length == 0)
                return new CheckExecutionResult(CheckStatus.Down, sw.Elapsed.TotalMilliseconds, "No addresses");
            if (!string.IsNullOrEmpty(p.ExpectedAddress))
            {
                var expected = IPAddress.Parse(p.ExpectedAddress);
                if (!addresses.Contains(expected))
                    return new CheckExecutionResult(CheckStatus.Warn, sw.Elapsed.TotalMilliseconds,
                        $"Resolved {string.Join(", ", addresses.Select(a => a.ToString()))}");
            }
            return new CheckExecutionResult(CheckStatus.Ok, sw.Elapsed.TotalMilliseconds,
                string.Join(", ", addresses.Select(a => a.ToString())));
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new CheckExecutionResult(CheckStatus.Down, sw.Elapsed.TotalMilliseconds, ex.Message);
        }
    }
}
