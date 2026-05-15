using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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
            using var request = new HttpRequestMessage(HttpMethod.Get, p.Url);
            if (!string.IsNullOrWhiteSpace(p.Username))
            {
                var raw = $"{p.Username}:{p.Password ?? ""}";
                var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);
            }

            using var response = await client.SendAsync(request, cancellationToken);
            sw.Stop();
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var status = (int)response.StatusCode;
            if (!HttpExpectedStatuses.Accepts(p, status))
            {
                var detail = $"HTTP {status} (outside expected set)";
                if (status is 401 or 403)
                    detail += string.IsNullOrWhiteSpace(p.Username)
                        ? " — many gateways return this without credentials; add 401/403 to ExpectedStatuses if “reachable” is enough"
                        : " — Basic auth rejected or insufficient; verify Username/Password or loosen ExpectedStatuses";
                return new CheckExecutionResult(CheckStatus.Down, sw.Elapsed.TotalMilliseconds, detail);
            }
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
