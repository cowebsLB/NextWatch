using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NextWatch.Core;
using NextWatch.Core.Alerts;
using NextWatch.Desktop.Services;
using NextWatch.Desktop.ViewModels;
using NextWatch.LanViewer;

namespace NextWatch.Desktop;

public partial class App : Application
{
    private IHost? _host;
    public static IServiceProvider Services { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var portable = e.Args.Contains("--portable", StringComparer.OrdinalIgnoreCase);
        var portablePath = portable ? Path.Combine(AppContext.BaseDirectory, "data") : null;

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddNextWatchCore(portable, portablePath, s =>
                    s.AddSingleton<IAlertSink, WpfAlertSink>());
                services.AddSingleton<LanViewerHost>();
                services.AddHostedService<LanViewerBackgroundService>();
                services.AddSingleton<TrayIconService>();
                services.AddSingleton<MainViewModel>();
            })
            .Build();

        await _host.StartAsync();
        Services = _host.Services;

        var vm = Services.GetRequiredService<MainViewModel>();
        await vm.InitializeAsync();

        var settings = await vm.GetSettingsAsync();
        if (!settings.OnboardingCompleted)
        {
            var onboarding = new OnboardingWindow(vm);
            if (onboarding.ShowDialog() != true)
            {
                Shutdown();
                return;
            }
        }

        var main = new MainWindow(vm);
        MainWindow = main;
        Services.GetRequiredService<TrayIconService>().Initialize(main, vm);

        if (!e.Args.Contains("--minimized", StringComparer.OrdinalIgnoreCase))
            main.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        Services.GetService<TrayIconService>()?.Dispose();
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        base.OnExit(e);
    }
}
