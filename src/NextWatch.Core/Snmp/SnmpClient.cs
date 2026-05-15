using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;

namespace NextWatch.Core.Snmp;

public static class SnmpClient
{
    private static readonly Variable SysUpTime = new(new ObjectIdentifier("1.3.6.1.2.1.1.3.0"));
    private static readonly Variable SysName = new(new ObjectIdentifier("1.3.6.1.2.1.1.5.0"));
    private static readonly Variable IfOperStatus = new(new ObjectIdentifier("1.3.6.1.2.1.2.2.1.8.1"));
    private static readonly Variable IfInOctets = new(new ObjectIdentifier("1.3.6.1.2.1.2.2.1.10.1"));
    private static readonly Variable IfOutOctets = new(new ObjectIdentifier("1.3.6.1.2.1.2.2.1.16.1"));

    public static async Task<bool> QuerySysUpTimeAsync(string host, string community, int port, CancellationToken ct)
    {
        var endpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(await ResolveHostAsync(host)), port);
        var result = await Messenger.GetAsync(VersionCode.V2, endpoint, new OctetString(community), new List<Variable> { SysUpTime }, ct);
        return result.Count > 0;
    }

    public static async Task<Dictionary<string, string>> GetDeviceInfoAsync(string host, string community, int port, CancellationToken ct)
    {
        var endpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(await ResolveHostAsync(host)), port);
        var vars = new List<Variable> { SysName, IfOperStatus };
        var result = await Messenger.GetAsync(VersionCode.V2, endpoint, new OctetString(community), vars, ct);
        return result.ToDictionary(v => v.Id.ToString(), v => v.Data.ToString());
    }

    public static async Task<(long inOctets, long outOctets)?> GetInterfaceCountersAsync(string host, string community, int port, int ifIndex, CancellationToken ct)
    {
        var inOid = new Variable(new ObjectIdentifier($"1.3.6.1.2.1.2.2.1.10.{ifIndex}"));
        var outOid = new Variable(new ObjectIdentifier($"1.3.6.1.2.1.2.2.1.16.{ifIndex}"));
        var endpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(await ResolveHostAsync(host)), port);
        var result = await Messenger.GetAsync(VersionCode.V2, endpoint, new OctetString(community), new List<Variable> { inOid, outOid }, ct);
        if (result.Count < 2) return null;
        return (long.Parse(result[0].Data.ToString()), long.Parse(result[1].Data.ToString()));
    }

    private static async Task<string> ResolveHostAsync(string host)
    {
        if (System.Net.IPAddress.TryParse(host, out var ip))
            return ip.ToString();
        var addresses = await System.Net.Dns.GetHostAddressesAsync(host);
        return addresses.First(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToString();
    }
}

public static class SnmpTemplates
{
    public static IReadOnlyList<string> Names => ["Generic", "Ubiquiti", "MikroTik"];
}
