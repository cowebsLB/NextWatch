using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using NextWatch.Core.Checks;
using NextWatch.Core.Data;
using NextWatch.Core.Domain;
using NextWatch.Core.Domain.Entities;
using NextWatch.Core.Infrastructure;
using NextWatch.Core.Infrastructure.Logging;
using NextWatch.Core.Scheduling;
using NextWatch.Core.Services;

namespace NextWatch.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICheckStatusNotifier _notifier;
    private readonly ConfigExportService _configExport;
    private readonly DiscoveryService _discovery;
    private readonly UpdateCheckerService _updateChecker;
    private readonly ReportExportService _reportExport;
    private readonly DiagnosticsExportService _diagnostics;
    private readonly InMemoryUiLogBuffer _logBuffer;
    private readonly NextWatchRuntimeOptions _runtime;
    private readonly ILogger<MainViewModel> _logger;
    private bool _logsWired;

    public ObservableCollection<TargetRowViewModel> Targets { get; } = [];
    public ObservableCollection<string> Tags { get; } = [];
    public ObservableCollection<AlertEvent> RecentAlerts { get; } = [];
    public ObservableCollection<UiLogLineVm> LogLines { get; } = [];
    public ObservableCollection<DetectedIpv4Network> DetectedNetworks { get; } = [];

    [ObservableProperty] private string _filterTag = string.Empty;
    [ObservableProperty] private string _windowTitle = "NextWatch";
    [ObservableProperty] private string _aggregateStatus = "Unknown";
    [ObservableProperty] private string _newTargetName = string.Empty;
    [ObservableProperty] private string _newTargetHost = "127.0.0.1";
    [ObservableProperty] private string _newTargetTag = string.Empty;
    [ObservableProperty] private string _discoveryCidr = "";
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _lanViewerEnabled;
    [ObservableProperty] private int _lanViewerPort = 5080;
    [ObservableProperty] private string _logFolderPath = "";

    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _okCount;
    [ObservableProperty] private int _warnCount;
    [ObservableProperty] private int _downCount;
    [ObservableProperty] private int _unknownCount;
    [ObservableProperty] private string _versionLabel = "v0.0.0";

    public event Action<string>? TrayStatusChanged;

    public MainViewModel(
        IServiceScopeFactory scopeFactory,
        ICheckStatusNotifier notifier,
        ConfigExportService configExport,
        DiscoveryService discovery,
        UpdateCheckerService updateChecker,
        ReportExportService reportExport,
        DiagnosticsExportService diagnostics,
        InMemoryUiLogBuffer logBuffer,
        NextWatchRuntimeOptions runtime,
        ILogger<MainViewModel> logger)
    {
        _scopeFactory = scopeFactory;
        _notifier = notifier;
        _configExport = configExport;
        _discovery = discovery;
        _updateChecker = updateChecker;
        _reportExport = reportExport;
        _diagnostics = diagnostics;
        _logBuffer = logBuffer;
        _runtime = runtime;
        _logger = logger;
        _notifier.StatusChanged += OnStatusChanged;
    }

    private void RefreshDetectedNetworksList()
    {
        DetectedNetworks.Clear();
        foreach (var n in DiscoveryService.GetDetectedIpv4Networks())
            DetectedNetworks.Add(n);
    }

    [RelayCommand]
    private void RefreshDetectedNetworks() => RefreshDetectedNetworksList();

    [RelayCommand]
    private void ApplyDetectedNetwork(string? cidr)
    {
        if (string.IsNullOrWhiteSpace(cidr))
            return;
        DiscoveryCidr = cidr.Trim();
        _logger.LogInformation("Discovery CIDR set from detected network list: {Cidr}", DiscoveryCidr);
    }

    public async Task InitializeAsync()
    {
        LogFolderPath = NextWatchPaths.GetLogsDirectory(_runtime.PortableDataPath, _runtime.PortableDataDirectory);
        EnsureLogsWired();
        RefreshDetectedNetworksList();

        var v = Assembly.GetExecutingAssembly().GetName().Version;
        WindowTitle = v is null ? "NextWatch" : $"NextWatch {v.Major}.{v.Minor}.{v.Build}";
        VersionLabel = v is null ? "v0.0.0" : $"v{v.Major}.{v.Minor}.{v.Build}";
        await RefreshAsync();
        var settings = await GetSettingsAsync();
        LanViewerEnabled = settings.LanViewerEnabled;
        LanViewerPort = settings.LanViewerPort;
    }

    private void EnsureLogsWired()
    {
        if (_logsWired)
            return;
        _logsWired = true;

        foreach (var e in _logBuffer.Snapshot())
            LogLines.Add(UiLogLineVm.From(e));

        _logBuffer.EntryAppended += entry =>
        {
            App.Current.Dispatcher.BeginInvoke(() =>
            {
                LogLines.Add(UiLogLineVm.From(entry));
                while (LogLines.Count > 8000)
                    LogLines.RemoveAt(0);
            });
        };

        _logBuffer.Cleared += () => App.Current.Dispatcher.BeginInvoke(LogLines.Clear);
    }

    public async Task<AppSettings> GetSettingsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NextWatchDbContext>();
        return await db.Settings.AsNoTracking().OrderBy(s => s.Id).FirstAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NextWatchDbContext>();
        var targets = await db.Targets.Include(t => t.Checks).AsNoTracking().ToListAsync();
        Targets.Clear();
        Tags.Clear();
        foreach (var tag in targets.Select(t => t.Tag).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().OrderBy(t => t))
            Tags.Add(tag!);

        foreach (var t in targets.Where(t => string.IsNullOrEmpty(FilterTag) || t.Tag == FilterTag))
        {
            var worst = t.Checks.DefaultIfEmpty().Max(c => c?.LastStatus ?? CheckStatus.Unknown);
            Targets.Add(new TargetRowViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Host = t.Host,
                Tag = t.Tag,
                Status = worst,
                CheckCount = t.Checks.Count
            });
        }

        var alerts = await db.AlertEvents.OrderByDescending(a => a.FiredAtUtc).Take(50).AsNoTracking().ToListAsync();
        RecentAlerts.Clear();
        foreach (var a in alerts)
            RecentAlerts.Add(a);

        TotalCount = Targets.Count;
        OkCount = Targets.Count(t => t.Status == CheckStatus.Ok);
        WarnCount = Targets.Count(t => t.Status == CheckStatus.Warn);
        DownCount = Targets.Count(t => t.Status == CheckStatus.Down);
        UnknownCount = Targets.Count(t => t.Status == CheckStatus.Unknown);

        AggregateStatus = DownCount > 0 ? "Down"
            : WarnCount > 0 ? "Warn"
            : TotalCount > 0 ? "Ok" : "Unknown";
        TrayStatusChanged?.Invoke(AggregateStatus);
    }

    [RelayCommand]
    private async Task AddTargetAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTargetName) || string.IsNullOrWhiteSpace(NewTargetHost))
            return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NextWatchDbContext>();
        var target = new MonitorTarget
        {
            Name = NewTargetName.Trim(),
            Host = NewTargetHost.Trim(),
            Tag = string.IsNullOrWhiteSpace(NewTargetTag) ? null : NewTargetTag.Trim()
        };
        db.Targets.Add(target);
        db.Checks.Add(new CheckDefinition
        {
            TargetId = target.Id,
            Type = CheckType.Ping,
            IntervalSeconds = 60,
            NextRunUtc = DateTime.UtcNow
        });
        db.Checks.Add(new CheckDefinition
        {
            TargetId = target.Id,
            Type = CheckType.Http,
            ParametersJson = CheckParameters.Serialize(new HttpCheckParams
            {
                Url = NewTargetHost.StartsWith("http") ? NewTargetHost : $"http://{NewTargetHost}",
                // SOHO routers often answer HTTP with 401/403 until logged in; still proves the service is up.
                ExpectedStatuses = "200-399,401,403"
            }),
            IntervalSeconds = 120,
            NextRunUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        NewTargetName = string.Empty;
        StatusMessage = $"Added {target.Name}";
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task CompleteOnboardingAsync(string host)
    {
        NewTargetHost = host;
        NewTargetName = host;
        await AddTargetAsync();
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NextWatchDbContext>();
        var settings = await db.Settings.OrderBy(s => s.Id).FirstAsync();
        settings.OnboardingCompleted = true;
        await db.SaveChangesAsync();
    }

    [RelayCommand]
    private async Task TogglePauseAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NextWatchDbContext>();
        var settings = await db.Settings.OrderBy(s => s.Id).FirstAsync();
        settings.MonitoringPaused = !settings.MonitoringPaused;
        await db.SaveChangesAsync();
        StatusMessage = settings.MonitoringPaused ? "Monitoring paused" : "Monitoring resumed";
    }

    [RelayCommand]
    public async Task MuteAlertsAsync(TimeSpan duration)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NextWatchDbContext>();
        var settings = await db.Settings.OrderBy(s => s.Id).FirstAsync();
        settings.AlertsMutedUntilUtc = DateTime.UtcNow.Add(duration);
        await db.SaveChangesAsync();
        StatusMessage = $"Alerts muted until {settings.AlertsMutedUntilUtc:u}";
    }

    [RelayCommand]
    private async Task ExportConfigAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NextWatchDbContext>();
        var json = await _configExport.ExportAsync(db);
        var dlg = new SaveFileDialog { Filter = "JSON|*.json", FileName = "nextwatch-config.json" };
        if (dlg.ShowDialog() == true)
        {
            await File.WriteAllTextAsync(dlg.FileName, json);
            StatusMessage = "Config exported";
        }
    }

    [RelayCommand]
    private async Task ImportConfigAsync()
    {
        var dlg = new OpenFileDialog { Filter = "JSON|*.json" };
        if (dlg.ShowDialog() != true) return;
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NextWatchDbContext>();
        await _configExport.ImportAsync(db, await File.ReadAllTextAsync(dlg.FileName));
        StatusMessage = "Config imported";
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task RunDiscoveryAsync()
    {
        if (string.IsNullOrWhiteSpace(DiscoveryCidr))
            _logger.LogInformation("Discovery requested from UI: all connected IPv4 subnets (CIDR field empty)");
        else
            _logger.LogInformation("Discovery requested from UI: manual CIDR {Cidr}", DiscoveryCidr.Trim());

        IReadOnlyList<DiscoveredHost> found;
        if (string.IsNullOrWhiteSpace(DiscoveryCidr))
        {
            StatusMessage = "Scanning connected IPv4 subnets (Wi‑Fi, Ethernet, VPN)…";
            found = await _discovery.ScanConnectedNetworksAsync(64);
            StatusMessage = $"Found {found.Count} reachable host(s) on local IPv4 networks";
        }
        else
        {
            StatusMessage = "Scanning subnet…";
            found = await _discovery.ScanSubnetAsync(DiscoveryCidr.Trim(), 64);
            StatusMessage = $"Found {found.Count} reachable host(s)";
        }
        if (found.FirstOrDefault() is { } first)
        {
            NewTargetHost = first.Address;
            NewTargetName = first.Hostname ?? first.Address;
        }
    }

    [RelayCommand]
    private async Task SaveLanSettingsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NextWatchDbContext>();
        var settings = await db.Settings.OrderBy(s => s.Id).FirstAsync();
        settings.LanViewerEnabled = LanViewerEnabled;
        settings.LanViewerPort = LanViewerPort;
        await db.SaveChangesAsync();
        StatusMessage = LanViewerEnabled ? "LAN viewer enabled (trusted network only)" : "LAN viewer disabled";
    }

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NextWatchDbContext>();
        var csv = await _reportExport.ExportCsvAsync(db);
        var dlg = new SaveFileDialog { Filter = "CSV|*.csv", FileName = "nextwatch-results.csv" };
        if (dlg.ShowDialog() == true)
        {
            await File.WriteAllTextAsync(dlg.FileName, csv);
            StatusMessage = "CSV exported";
        }
    }

    [RelayCommand]
    private async Task ExportHtmlAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NextWatchDbContext>();
        var html = await _reportExport.ExportHtmlSnapshotAsync(db);
        var dlg = new SaveFileDialog { Filter = "HTML|*.html", FileName = "nextwatch-snapshot.html" };
        if (dlg.ShowDialog() == true)
        {
            await File.WriteAllTextAsync(dlg.FileName, html);
            StatusMessage = "HTML snapshot exported";
        }
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        var latest = await _updateChecker.GetLatestReleaseAsync();
        if (latest is null)
        {
            StatusMessage = "Could not check for updates";
            return;
        }
        if (_updateChecker.IsNewer(latest.Version))
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(latest.HtmlUrl) { UseShellExecute = true });
        else
            StatusMessage = "You are on the latest version";
    }

    [RelayCommand]
    private async Task ExportDiagnosticsAsync()
    {
        var settings = await GetSettingsAsync();
        var dlg = new SaveFileDialog { Filter = "ZIP|*.zip", FileName = "nextwatch-diagnostics.zip" };
        if (dlg.ShowDialog() != true) return;
        await _diagnostics.ExportZipAsync(dlg.FileName, settings.PortableDataPath, settings.PortableDataDirectory);
        StatusMessage = "Diagnostics exported";
    }

    [RelayCommand]
    private void ClearLogs()
    {
        _logBuffer.Clear();
    }

    [RelayCommand]
    private void CopyLogsToClipboard()
    {
        var sb = new StringBuilder(Math.Max(256, LogLines.Count * 64));
        foreach (var row in LogLines)
        {
            sb.Append(row.LocalTime).Append('\t').Append(row.Level).Append('\t').Append(row.Source).Append('\t')
                .AppendLine(row.Message.Replace('\r', ' ').Replace('\n', ' '));
        }

        Clipboard.SetText(sb.ToString());
        StatusMessage = $"Copied {LogLines.Count} log lines";
    }

    [RelayCommand]
    private void OpenLogsFolder()
    {
        if (Directory.Exists(LogFolderPath))
            Process.Start(new ProcessStartInfo { FileName = LogFolderPath, UseShellExecute = true });
        else
            StatusMessage = "Logs folder not found";
    }

    private async void OnStatusChanged(object? sender, CheckStatusChangedEventArgs e) =>
        await App.Current.Dispatcher.InvokeAsync(async () => await RefreshAsync());
}

public sealed class TargetRowViewModel
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Host { get; init; } = string.Empty;
    public string? Tag { get; init; }
    public CheckStatus Status { get; init; }
    public int CheckCount { get; init; }
}