using Serilog.Events;

namespace NextWatch.Core.Infrastructure.Logging;

public sealed record UiLogEntry(DateTime TimestampUtc, LogEventLevel Level, string SourceContext, string Message);
