using System.Drawing;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using NextWatch.Desktop.ViewModels;

namespace NextWatch.Desktop.Services;

public sealed class TrayIconService : IDisposable
{
    private TaskbarIcon? _icon;
    private MainWindow? _window;

    public void Initialize(MainWindow window, MainViewModel vm)
    {
        _window = window;
        _icon = new TaskbarIcon
        {
            ToolTipText = "NextWatch",
            Icon = SystemIcons.Application
        };

        _icon.ContextMenu = new System.Windows.Controls.ContextMenu();
        AddMenu("Open dashboard", () => ShowWindow());
        AddMenu("Pause monitoring", () => vm.TogglePauseCommand.Execute(null));
        AddMenu("Mute alerts 1h", () => vm.MuteAlertsCommand.Execute(TimeSpan.FromHours(1)));
        AddMenu("Export config", () => vm.ExportConfigCommand.Execute(null));
        AddMenu("Exit", () => { _icon.Dispose(); Application.Current.Shutdown(); });

        _icon.TrayMouseDoubleClick += (_, _) => ShowWindow();
        vm.TrayStatusChanged += status =>
        {
            var title = vm.WindowTitle;
            _icon!.ToolTipText = $"{title} — {status}";
        };
    }

    public void ShowBalloon(string title, string message) =>
        _icon?.ShowBalloonTip(title, message, BalloonIcon.Info);

    private void ShowWindow()
    {
        if (_window is null) return;
        _window.Show();
        _window.WindowState = WindowState.Normal;
        _window.Activate();
    }

    private void AddMenu(string header, Action action)
    {
        var item = new System.Windows.Controls.MenuItem { Header = header };
        item.Click += (_, _) => action();
        _icon!.ContextMenu!.Items.Add(item);
    }

    public void Dispose() => _icon?.Dispose();
}
