namespace NextWatch.Core.Domain.Entities;

public sealed class AppSettings
{
    public int Id { get; set; } = 1;
    public int RetentionDays { get; set; } = 30;
    public string Theme { get; set; } = "Dark";
    public bool LanViewerEnabled { get; set; }
    public int LanViewerPort { get; set; } = 5080;
    public string? LanSharedSecretHash { get; set; }
    public string? LastSeenReleaseVersion { get; set; }
    public bool PortableDataPath { get; set; }
    public string? PortableDataDirectory { get; set; }
    public bool StartWithWindows { get; set; }
    public bool MonitoringPaused { get; set; }
    public bool AlertsMutedUntilRestart { get; set; }
    public DateTime? AlertsMutedUntilUtc { get; set; }
    public string? DefaultWebhookUrl { get; set; }
    public bool OnboardingCompleted { get; set; }
}
