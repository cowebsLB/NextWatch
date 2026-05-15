using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace NextWatch.Core.Services;

public sealed record DiscoveredHost(string Address, string? Hostname, bool IsReachable);

/// <summary>IPv4 subnet derived from a connected adapter (for Discovery UI).</summary>
public sealed record DetectedIpv4Network(string Cidr, string InterfaceName)
{
    public string DisplayLabel => $"{InterfaceName} — {Cidr}";
}

public sealed class DiscoveryService(ILogger<DiscoveryService> logger)
{
    /// <summary>
    /// Enumerates IPv4 subnets for interfaces that are up (Ethernet, Wi‑Fi, VPN, etc.).
    /// Uses each address's prefix length from the OS (typically /24 on home LANs).
    /// Link‑local (169.254.x.x) is skipped.
    /// </summary>
    public static IReadOnlyList<DetectedIpv4Network> GetDetectedIpv4Networks()
    {
        var byCidr = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;

            var label = string.IsNullOrWhiteSpace(ni.Description) ? ni.Name : ni.Description;

            foreach (var ua in ni.GetIPProperties().UnicastAddresses)
            {
                if (ua.Address.AddressFamily != AddressFamily.InterNetwork)
                    continue;
                var prefix = ua.PrefixLength;
                if (prefix is < 8 or > 30)
                    continue;
                if (IsApipa(ua.Address))
                    continue;
                var cidr = ToNetworkCidr(ua.Address, prefix);
                byCidr.TryAdd(cidr, label);
            }
        }

        return byCidr.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => new DetectedIpv4Network(kv.Key, kv.Value))
            .ToList();
    }

    public static IReadOnlyList<string> GetConnectedIpv4Networks() =>
        GetDetectedIpv4Networks().Select(x => x.Cidr).ToList();

    /// <summary>
    /// Ping‑scans each connected IPv4 subnet (see <see cref="GetConnectedIpv4Networks"/>); merges unique IPs.
    /// </summary>
    public async Task<IReadOnlyList<DiscoveredHost>> ScanConnectedNetworksAsync(int maxHostsPerSubnet = 64,
        CancellationToken ct = default)
    {
        var nets = GetConnectedIpv4Networks();
        if (nets.Count == 0)
        {
            logger.LogWarning("Discovery multi-subnet scan: no connected IPv4 networks detected (up interfaces)");
            return [];
        }

        logger.LogInformation(
            "Discovery scan all connected IPv4 subnets started ({NetworkCount} networks, up to {MaxHosts} hosts each)",
            nets.Count,
            maxHostsPerSubnet);

        var merged = new Dictionary<string, DiscoveredHost>(StringComparer.OrdinalIgnoreCase);
        foreach (var cidr in nets)
        {
            foreach (var host in await ScanSubnetAsync(cidr, maxHostsPerSubnet, ct))
                merged[host.Address] = host;
        }

        var ordered = merged.Values
            .OrderBy(h =>
            {
                var b = IPAddress.Parse(h.Address).GetAddressBytes();
                return (b[0], b[1], b[2], b[3]);
            })
            .ToList();

        logger.LogInformation(
            "Discovery scan all connected IPv4 subnets completed; merged unique reachable hosts: {Count}",
            ordered.Count);

        return ordered;
    }

    private static bool IsApipa(IPAddress ip)
    {
        var b = ip.GetAddressBytes();
        return b.Length == 4 && b[0] == 169 && b[1] == 254;
    }

    private static string ToNetworkCidr(IPAddress ip, int prefixLength)
    {
        var bytes = ip.GetAddressBytes();
        if (bytes.Length != 4)
            throw new InvalidOperationException("Expected IPv4");

        var addr = (uint)bytes[0] << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3];
        var mask = prefixLength == 0 ? 0u : 0xFFFFFFFFu << (32 - prefixLength);
        var network = addr & mask;
        var nb = new[]
        {
            (byte)(network >> 24),
            (byte)(network >> 16),
            (byte)(network >> 8),
            (byte)network
        };
        return $"{new IPAddress(nb)}/{prefixLength}";
    }

    public async Task<IReadOnlyList<DiscoveredHost>> ScanSubnetAsync(string cidr, int maxHosts = 254, CancellationToken ct = default)
    {
        logger.LogInformation("Discovery subnet scan started for {Cidr} (max {MaxHosts} host probes)", cidr, maxHosts);

        var (network, prefix) = ParseCidr(cidr);
        var tasks = new List<Task<DiscoveredHost>>();
        var count = Math.Min(maxHosts, (int)Math.Pow(2, 32 - prefix) - 2);
        for (var i = 1; i <= count; i++)
        {
            var ip = IncrementIp(network, i);
            tasks.Add(ProbeHostAsync(ip, ct));
        }

        var results = await Task.WhenAll(tasks);
        var reachable = results.Where(r => r.IsReachable).ToList();

        foreach (var h in reachable)
            logger.LogInformation("Discovery found reachable host {Ip} ({Hostname})", h.Address, h.Hostname ?? "—");

        logger.LogInformation(
            "Discovery subnet scan completed for {Cidr}; reachable hosts: {Count}",
            cidr,
            reachable.Count);

        return reachable;
    }

    private async Task<DiscoveredHost> ProbeHostAsync(IPAddress ip, CancellationToken ct)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(ip, 1000);
            string? hostname = null;
            if (reply.Status == IPStatus.Success)
            {
                try
                {
                    var entry = await Dns.GetHostEntryAsync(ip);
                    hostname = entry.HostName;
                }
                catch
                {
                    // ignore reverse DNS failures
                }
            }

            return new DiscoveredHost(ip.ToString(), hostname, reply.Status == IPStatus.Success);
        }
        catch
        {
            return new DiscoveredHost(ip.ToString(), null, false);
        }
    }

    private static (uint network, int prefix) ParseCidr(string cidr)
    {
        var parts = cidr.Split('/');
        if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var ip))
            throw new ArgumentException("Invalid CIDR", nameof(cidr));
        var prefix = int.Parse(parts[1]);
        var bytes = ip.GetAddressBytes();
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        var value = BitConverter.ToUInt32(bytes, 0);
        var mask = uint.MaxValue << (32 - prefix);
        return (value & mask, prefix);
    }

    private static IPAddress IncrementIp(uint network, int offset)
    {
        var value = network + (uint)offset;
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return new IPAddress(bytes);
    }
}
