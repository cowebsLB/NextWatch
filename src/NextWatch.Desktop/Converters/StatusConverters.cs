using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using NextWatch.Core.Domain;

namespace NextWatch.Desktop.Converters;

internal static class StatusPalette
{
    public static (Brush Fg, Brush Bg, Brush Border, string Label) Get(object? status)
    {
        var name = status switch
        {
            CheckStatus s => s.ToString(),
            string s => s,
            _ => "Unknown"
        };

        Brush Get(string key) => (Brush)Application.Current.Resources[key];

        return name switch
        {
            "Ok"   => (Get("Brush.Ok"),      Get("Brush.OkSoft"),      Get("Brush.Ok"),      "Ok"),
            "Warn" => (Get("Brush.Warn"),    Get("Brush.WarnSoft"),    Get("Brush.Warn"),    "Warn"),
            "Down" => (Get("Brush.Down"),    Get("Brush.DownSoft"),    Get("Brush.Down"),    "Down"),
            _      => (Get("Brush.Unknown"), Get("Brush.UnknownSoft"), Get("Brush.Unknown"), "Unknown"),
        };
    }
}

public sealed class StatusToForegroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => StatusPalette.Get(value).Fg;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

public sealed class StatusToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => StatusPalette.Get(value).Bg;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

public sealed class StatusToBorderConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => StatusPalette.Get(value).Border;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

public sealed class LogLevelToForegroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        Brush Get(string key) => (Brush)Application.Current.Resources[key];
        return (value as string) switch
        {
            "ERR" or "FTL" => Get("Brush.Down"),
            "WRN"          => Get("Brush.Warn"),
            "INF"          => Get("Brush.Accent"),
            "DBG" or "VRB" => Get("Brush.TextDim"),
            _              => Get("Brush.TextMuted"),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
