using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NextWatch.Core.Data;

/// <summary>
/// Used by <c>dotnet ef</c> design-time tools only (no DI from the WPF host).
/// </summary>
public sealed class NextWatchDesignTimeDbContextFactory : IDesignTimeDbContextFactory<NextWatchDbContext>
{
    public NextWatchDbContext CreateDbContext(string[] args)
    {
        var dbPath = Path.Combine(Path.GetTempPath(), "nextwatch-ef-design.db");
        var options = new DbContextOptionsBuilder<NextWatchDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;
        return new NextWatchDbContext(options);
    }
}
