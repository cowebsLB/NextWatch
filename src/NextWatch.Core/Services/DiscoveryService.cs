using System.Net;
using System.Net.NetworkInformation;

namespace NextWatch.Core.Services;

public sealed record DiscoveredHost(string Address, string? Hostname, bool IsReachable);

public sealed class DiscoveryService
{
    public async Task<IReadOnlyList<DiscoveredHost>> ScanSubnetAsync(string cidr, int maxHosts = 254, CancellationToken ct = default)
    {
        var (network, prefix) = ParseCidr(cidr);
        var hosts = new List<DiscoveredHost>();
        var tasks = new List<Task<DiscoveredHost>>();
        var count = Math.Min(maxHosts, (int)Math.Pow(2, 32 - prefix) - 2);
        for (var i = 1; i <= count; i++)
        {
            var ip = IncrementIp(network, i);
            tasks.Add(ProbeHostAsync(ip, ct));
        }

        var results = await Task.WhenAll(tasks);
        return results.Where(r => r.IsReachable).ToList();
    }

    private static async Task<DiscoveredHost> ProbeHostAsync(IPAddress ip, CancellationToken ct)
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
