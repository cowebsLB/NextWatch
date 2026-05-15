namespace NextWatch.Core.Infrastructure.Logging;

/// <summary>
/// Ring buffer of recent log lines for the desktop Logs tab. Thread-safe.
/// </summary>
public sealed class InMemoryUiLogBuffer
{
    private readonly object _lock = new();
    private readonly List<UiLogEntry> _entries = [];
    private const int MaxEntries = 8000;

    public event Action<UiLogEntry>? EntryAppended;
    public event Action? Cleared;

    public void Append(UiLogEntry entry)
    {
        lock (_lock)
        {
            _entries.Add(entry);
            var overflow = _entries.Count - MaxEntries;
            if (overflow > 0)
                _entries.RemoveRange(0, overflow);
        }

        EntryAppended?.Invoke(entry);
    }

    public void Clear()
    {
        lock (_lock)
            _entries.Clear();

        Cleared?.Invoke();
    }

    public IReadOnlyList<UiLogEntry> Snapshot()
    {
        lock (_lock)
            return _entries.ToArray();
    }
}
