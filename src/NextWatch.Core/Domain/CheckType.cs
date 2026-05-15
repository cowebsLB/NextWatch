namespace NextWatch.Core.Domain;

public enum CheckType
{
    Ping = 0,
    Http = 1,
    Tcp = 2,
    Ssl = 3,
    Snmp = 4,
    Dns = 5,
    Bandwidth = 6
}
