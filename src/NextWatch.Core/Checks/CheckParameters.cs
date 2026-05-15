using System.Text.Json;

namespace NextWatch.Core.Checks;

public static class CheckParameters
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static T? Parse<T>(string? json) where T : class =>
        string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<T>(json, Options);

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);
}

public sealed class HttpCheckParams
{
    public string Url { get; set; } = "http://localhost";
    public int ExpectedStatusCode { get; set; } = 200;
    public string? Keyword { get; set; }
    public bool ValidateCertificate { get; set; } = true;
}

public sealed class TcpCheckParams
{
    public int Port { get; set; } = 80;
}

public sealed class SslCheckParams
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 443;
    public int WarnDaysBeforeExpiry { get; set; } = 14;
}

public sealed class SnmpCheckParams
{
    public string Community { get; set; } = "public";
    public string Template { get; set; } = "Generic";
    public int Port { get; set; } = 161;
}

public sealed class DnsCheckParams
{
    public string Hostname { get; set; } = "example.com";
    public string? ExpectedAddress { get; set; }
}

public sealed class BandwidthCheckParams
{
    public string InterfaceName { get; set; } = string.Empty;
    public bool UseSnmp { get; set; }
    public int SnmpIfIndex { get; set; } = 1;
}
