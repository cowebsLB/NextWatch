using System.Diagnostics;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using NextWatch.Core.Domain;
using NextWatch.Core.Domain.Entities;

namespace NextWatch.Core.Checks;

public sealed class SslCheckExecutor : ICheckExecutor
{
    public CheckType Type => CheckType.Ssl;

    public async Task<CheckExecutionResult> ExecuteAsync(MonitorTarget target, CheckDefinition check, CancellationToken cancellationToken)
    {
        var p = CheckParameters.Parse<SslCheckParams>(check.ParametersJson) ?? new SslCheckParams { Host = target.Host };
        var sw = Stopwatch.StartNew();
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(p.Host, p.Port, cancellationToken);
            using var ssl = new SslStream(client.GetStream(), false, (_, _, _, _) => true);
            await ssl.AuthenticateAsClientAsync(new System.Net.Security.SslClientAuthenticationOptions
            {
                TargetHost = p.Host
            }, cancellationToken);
            var cert = ssl.RemoteCertificate as X509Certificate2 ?? new X509Certificate2(ssl.RemoteCertificate!);
            sw.Stop();
            var days = (cert.NotAfter.ToUniversalTime() - DateTime.UtcNow).TotalDays;
            if (days < 0)
                return new CheckExecutionResult(CheckStatus.Down, sw.Elapsed.TotalMilliseconds, $"Expired {cert.NotAfter:yyyy-MM-dd}");
            if (days < p.WarnDaysBeforeExpiry)
                return new CheckExecutionResult(CheckStatus.Warn, sw.Elapsed.TotalMilliseconds, $"Expires in {days:F0} days");
            return new CheckExecutionResult(CheckStatus.Ok, sw.Elapsed.TotalMilliseconds, $"Valid until {cert.NotAfter:yyyy-MM-dd}");
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new CheckExecutionResult(CheckStatus.Down, sw.Elapsed.TotalMilliseconds, ex.Message);
        }
    }
}
