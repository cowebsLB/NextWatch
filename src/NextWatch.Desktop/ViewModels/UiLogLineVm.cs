using NextWatch.Core.Infrastructure.Logging;
using Serilog.Events;

namespace NextWatch.Desktop.ViewModels;

public sealed class UiLogLineVm
{
    public string LocalTime { get; }
    public string Level { get; }
    public string Source { get; }
    public string Message { get; }

    private UiLogLineVm(string localTime, string level, string source, string message)
    {
        LocalTime = localTime;
        Level = level;
        Source = source;
        Message = message;
    }

    public static UiLogLineVm From(UiLogEntry e)
    {
        var lvl = e.Level switch
        {
            LogEventLevel.Verbose => "VRB",
            LogEventLevel.Debug => "DBG",
            LogEventLevel.Information => "INF",
            LogEventLevel.Warning => "WRN",
            LogEventLevel.Error => "ERR",
            LogEventLevel.Fatal => "FTL",
            _ => e.Level.ToString()
        };
        return new UiLogLineVm(
            e.TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff"),
            lvl,
            e.SourceContext,
            e.Message);
    }
}
