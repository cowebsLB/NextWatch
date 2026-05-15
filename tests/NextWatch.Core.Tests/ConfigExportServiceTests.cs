using Microsoft.EntityFrameworkCore;
using NextWatch.Core.Data;
using NextWatch.Core.Domain;
using NextWatch.Core.Domain.Entities;
using NextWatch.Core.Services;
using Xunit;

namespace NextWatch.Core.Tests;

public class ConfigExportServiceTests
{
    [Fact]
    public async Task ExportAsync_StripsSecretParametersFromChecks()
    {
        await using var db = CreateDb();
        var target = new MonitorTarget { Name = "Router", Host = "192.168.1.1" };
        db.Targets.Add(target);
        db.Checks.Add(new CheckDefinition
        {
            TargetId = target.Id,
            Type = CheckType.Snmp,
            ParametersJson = """{"community":"public-ro","password":"x","secret":"y","port":161}"""
        });
        await db.SaveChangesAsync();

        var json = await new ConfigExportService().ExportAsync(db);

        Assert.DoesNotContain("public-ro", json, StringComparison.Ordinal);
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("161", json, StringComparison.Ordinal);
    }

    private static NextWatchDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<NextWatchDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var db = new NextWatchDbContext(options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();
        return db;
    }
}
