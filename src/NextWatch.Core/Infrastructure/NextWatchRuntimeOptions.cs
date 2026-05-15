namespace NextWatch.Core.Infrastructure;

/// <summary>
/// Resolved at startup from command-line / host configuration (source of truth for data paths).
/// </summary>
public sealed class NextWatchRuntimeOptions
{
    public bool PortableDataPath { get; init; }
    public string? PortableDataDirectory { get; init; }
}
