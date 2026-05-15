using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using NextWatch.Core.Checks;
using NextWatch.Core.Data;
using NextWatch.Core.Domain;
using NextWatch.Core.Domain.Entities;
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

    public ObservableCollection<TargetRowViewModel> Targets { get; } = [];
    public ObservableCollection<string> Tags { get; } = [];
    public ObservableCollection<AlertEvent> RecentAlerts { get; } = [];

    [ObservableProperty] private string _filterTag = string.Empty;
    [ObservableProperty] private string _windowTitle = "NextWatch";
    [ObservableProperty] private string _aggregateStatus = "Unknown";
    [ObservableProperty] private string _newTargetName = string.Empty;
    [ObservableProperty] private string _newTargetHost = "127.0.0.1";
    [ObservableProperty] private string _newTargetTag = string.Empty;
    [ObservableProperty] private string _discoveryCidr = "192.168.1.0/24";
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _lanViewerEnabled;
    [ObservableProperty] private int _lanViewerPort = 5080;

    public event Action<string>? TrayStatusChanged;

    public MainViewModel(
        IServiceScopeFactory scopeFactory,
        ICheckStatusNotifier notifier,
        ConfigExportService configExport,
        DiscoveryService discovery,
        UpdateCheckerService updateChecker,
        ReportExportService reportExport,
        DiagnosticsExportService diagnostics)
    {
        _scopeFactory = scopeFactory;
        _notifier = notifier;
        _configExport = configExport;
        _discovery = discovery;
        _updateChecker = updateChecker;
        _reportExport = reportExport;
        _diagnostics = diagnostics;
        _notifier.StatusChanged += OnStatusChanged;
    }

    public async Task InitializeAsync()
    {
        var v = Assembly.GetExecutingAssembly().GetName().Version;
        WindowTitle = v is null ? "NextWatch" : $"NextWatch {v.Major}.{v.Minor}.{v.Build}";
        await RefreshAsync();
        var settings = await GetSettingsAsync();
        LanViewerEnabled = settings.LanViewerEnabled;
        LanViewerPort = settings.LanViewerPort;
    }

    public async Task<AppSettings> GetSettingsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NextWatchDbContext>();
        return await db.Settings.AsNoTracking().FirstAsync();
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

        AggregateStatus = Targets.Any(t => t.Status == CheckStatus.Down) ? "Down"
            : Targets.Any(t => t.Status == CheckStatus.Warn) ? "Warn"
            : Targets.Count > 0 ? "Ok" : "Unknown";
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
                Url = NewTargetHost.StartsWith("http") ? NewTargetHost : $"http://{NewTargetHost}"
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
        var settings = await db.Settings.FirstAsync();
        settings.OnboardingCompleted = true;
        await db.SaveChangesAsync();
    }

    [RelayCommand]
    private async Task TogglePauseAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NextWatchDbContext>();
        var settings = await db.Settings.FirstAsync();
        settings.MonitoringPaused = !settings.MonitoringPaused;
        await db.SaveChangesAsync();
        StatusMessage = settings.MonitoringPaused ? "Monitoring paused" : "Monitoring resumed";
    }

    [RelayCommand]
    public async Task MuteAlertsAsync(TimeSpan duration)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NextWatchDbContext>();
        var settings = await db.Settings.FirstAsync();
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
        StatusMessage = "Scanning subnet...";
        var found = await _discovery.ScanSubnetAsync(DiscoveryCidr, 64);
        StatusMessage = $"Found {found.Count} hosts";
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
        var settings = await db.Settings.FirstAsync();
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