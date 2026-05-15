using System.Net.Http.Json;
using System.Reflection;

namespace NextWatch.Core.Services;

public sealed record ReleaseInfo(string Version, string HtmlUrl, string? DownloadUrl);

public sealed class UpdateCheckerService(IHttpClientFactory httpClientFactory)
{
    private const string Repo = "cowebsLB/NextWatch";

    public Version CurrentVersion =>
        Version.Parse(Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.1.0");

    public async Task<ReleaseInfo?> GetLatestReleaseAsync(CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("NextWatch/0.1");
        var url = $"https://api.github.com/repos/{Repo}/releases/latest";
        var doc = await client.GetFromJsonAsync<GithubRelease>(url, ct);
        if (doc?.TagName is null)
            return null;
        var version = doc.TagName.TrimStart('v');
        var asset = doc.Assets?.FirstOrDefault(a => a.Name?.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) == true);
        return new ReleaseInfo(version, doc.HtmlUrl ?? $"https://github.com/{Repo}/releases", asset?.BrowserDownloadUrl);
    }

    public bool IsNewer(string remoteVersion) =>
        Version.TryParse(remoteVersion, out var remote) && remote > CurrentVersion;

    private sealed class GithubRelease
    {
        public string? TagName { get; set; }
        public string? HtmlUrl { get; set; }
        public List<GithubAsset>? Assets { get; set; }
    }

    private sealed class GithubAsset
    {
        public string? Name { get; set; }
        public string? BrowserDownloadUrl { get; set; }
    }
}
