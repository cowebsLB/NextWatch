using System.Globalization;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace NextWatch.Core.Infrastructure.Logging;

public sealed class InMemoryUiLogSink : ILogEventSink
{
    private readonly InMemoryUiLogBuffer _buffer;
    private readonly MessageTemplateTextFormatter _messageFormatter = new(
        "{Message:lj}{NewLine}{Exception}",
        CultureInfo.InvariantCulture);

    public InMemoryUiLogSink(InMemoryUiLogBuffer buffer) =>
        _buffer = buffer;

    public void Emit(LogEvent logEvent)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        _messageFormatter.Format(logEvent, writer);
        var message = writer.ToString().TrimEnd();

        var source = "";
        if (logEvent.Properties.TryGetValue("SourceContext", out var sc)
            && sc is ScalarValue { Value: string ctx })
            source = ctx;

        _buffer.Append(new UiLogEntry(logEvent.Timestamp.UtcDateTime, logEvent.Level, source, message));
    }
}
