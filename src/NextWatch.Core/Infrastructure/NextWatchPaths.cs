namespace NextWatch.Core.Infrastructure;

public static class NextWatchPaths
{
    public const string AppFolderName = "NextWatch";

    public static string GetDataDirectory(bool portable, string? portablePath)
    {
        if (portable)
        {
            var baseDir = string.IsNullOrWhiteSpace(portablePath)
                ? Path.Combine(AppContext.BaseDirectory, "data")
                : portablePath;
            Directory.CreateDirectory(baseDir);
            return baseDir;
        }

        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppFolderName);
        Directory.CreateDirectory(appData);
        return appData;
    }

    public static string GetDatabasePath(bool portable, string? portablePath) =>
        Path.Combine(GetDataDirectory(portable, portablePath), "data.db");

    public static string GetLogsDirectory(bool portable, string? portablePath) =>
        Path.Combine(GetDataDirectory(portable, portablePath), "logs");
}
