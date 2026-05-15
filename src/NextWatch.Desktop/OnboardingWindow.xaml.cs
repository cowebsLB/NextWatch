using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NextWatch.Desktop.ViewModels;

namespace NextWatch.Desktop;

public partial class OnboardingWindow : Window
{
    private readonly MainViewModel _vm;

    public OnboardingWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        await _vm.CompleteOnboardingCommand.ExecuteAsync(HostBox.Text.Trim());
        DialogResult = true;
        Close();
    }

    private async void Skip_Click(object sender, RoutedEventArgs e)
    {
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NextWatch.Core.Data.NextWatchDbContext>();
        var settings = await db.Settings.OrderBy(s => s.Id).FirstAsync();
        settings.OnboardingCompleted = true;
        await db.SaveChangesAsync();
        DialogResult = true;
        Close();
    }
}
