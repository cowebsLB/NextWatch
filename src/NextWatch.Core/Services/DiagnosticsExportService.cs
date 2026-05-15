using System.IO.Compression;
using NextWatch.Core.Infrastructure;

namespace NextWatch.Core.Services;

public sealed class DiagnosticsExportService(NextWatchRuntimeOptions runtime)
{
    public Task ExportZipAsync(string outputPath, CancellationToken ct = default) =>
        ExportZipAsync(outputPath, runtime.PortableDataPath, runtime.PortableDataDirectory, ct);

    public async Task ExportZipAsync(string outputPath, bool portable, string? portablePath, CancellationToken ct = default)
    {
        var dataDir = NextWatchPaths.GetDataDirectory(portable, portablePath);
        var logsDir = NextWatchPaths.GetLogsDirectory(portable, portablePath);
        if (File.Exists(outputPath))
            File.Delete(outputPath);

        using var zip = ZipFile.Open(outputPath, ZipArchiveMode.Create);
        AddDirectory(zip, dataDir, "data");
        if (Directory.Exists(logsDir))
            AddDirectory(zip, logsDir, "logs");
    }

    private static void AddDirectory(ZipArchive zip, string dir, string prefix)
    {
        if (!Directory.Exists(dir)) return;
        foreach (var file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
        {
            var entryName = Path.Combine(prefix, Path.GetRelativePath(dir, file)).Replace('\\', '/');
            zip.CreateEntryFromFile(file, entryName);
        }
    }
}
