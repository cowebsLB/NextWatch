using System.Diagnostics;
using System.Net.Http;
using NextWatch.Core.Domain;
using NextWatch.Core.Domain.Entities;

namespace NextWatch.Core.Checks;

public sealed class HttpCheckExecutor(IHttpClientFactory httpClientFactory) : ICheckExecutor
{
    public CheckType Type => CheckType.Http;

    public async Task<CheckExecutionResult> ExecuteAsync(MonitorTarget target, CheckDefinition check, CancellationToken cancellationToken)
    {
        var p = CheckParameters.Parse<HttpCheckParams>(check.ParametersJson) ?? new HttpCheckParams
        {
            Url = target.Host.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? target.Host : $"http://{target.Host}"
        };

        var client = httpClientFactory.CreateClient("NextWatchChecks");
        var sw = Stopwatch.StartNew();
        try
        {
            using var response = await client.GetAsync(p.Url, cancellationToken);
            sw.Stop();
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var status = (int)response.StatusCode;
            if (!HttpExpectedStatuses.Accepts(p, status))
                return new CheckExecutionResult(CheckStatus.Down, sw.Elapsed.TotalMilliseconds, $"HTTP {status} (outside expected set)");
            if (!string.IsNullOrEmpty(p.Keyword) && !body.Contains(p.Keyword, StringComparison.OrdinalIgnoreCase))
                return new CheckExecutionResult(CheckStatus.Warn, sw.Elapsed.TotalMilliseconds, "Keyword not found");
            return new CheckExecutionResult(CheckStatus.Ok, sw.Elapsed.TotalMilliseconds, $"HTTP {status}");
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new CheckExecutionResult(CheckStatus.Down, sw.Elapsed.TotalMilliseconds, ex.Message);
        }
    }
}
