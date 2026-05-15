using System.Windows;
using NextWatch.Desktop.ViewModels;

namespace NextWatch.Desktop;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
