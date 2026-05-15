using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NextWatch.Core.Data.Migrations;
using NextWatch.Core.Infrastructure;

namespace NextWatch.Core.Data;

public sealed class DatabaseInitializer(
    IServiceScopeFactory scopeFactory,
    NextWatchRuntimeOptions runtime,
    ILogger<DatabaseInitializer> logger) : IHostedService
{
    private static readonly string InitialMigrationId = typeof(InitialCreate).GetCustomAttribute<MigrationAttribute>()?.Id
        ?? throw new InvalidOperationException("InitialCreate migration must define [Migration(\"...\")].");

    private const string EfProductVersion = "8.0.11";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NextWatchDbContext>();

        await TryBaselineLegacyEnsureCreatedDatabaseAsync(db, cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Database migrated");

        var settings = await db.Settings.OrderBy(s => s.Id).FirstAsync(cancellationToken);
        settings.PortableDataPath = runtime.PortableDataPath;
        settings.PortableDataDirectory = runtime.PortableDataDirectory;
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Databases created with <see cref="DatabaseFacade.EnsureCreatedAsync"/> have no
    /// <c>__EFMigrationsHistory</c> row. Stamp <see cref="InitialMigrationId"/> so
    /// <see cref="DatabaseFacade.MigrateAsync"/> becomes a no-op for already-matching schema.
    /// </summary>
    private async Task TryBaselineLegacyEnsureCreatedDatabaseAsync(NextWatchDbContext db, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(ct);
        try
        {
            if (await SqliteTableExistsAsync(connection, "__EFMigrationsHistory", ct))
                return;

            if (!await SqliteTableExistsAsync(connection, "Targets", ct))
                return;

            logger.LogInformation(
                "Legacy EnsureCreated database detected; stamping migration {MigrationId}",
                InitialMigrationId);

            await using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                    """
                    CREATE TABLE "__EFMigrationsHistory" (
                        "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                        "ProductVersion" TEXT NOT NULL
                    );
                    """;
                await cmd.ExecuteNonQueryAsync(ct);
            }

            await using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                    """
                    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                    VALUES (@id, @ver)
                    """;
                AddParameter(cmd, "@id", InitialMigrationId);
                AddParameter(cmd, "@ver", EfProductVersion);
                await cmd.ExecuteNonQueryAsync(ct);
            }
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    private static void AddParameter(DbCommand cmd, string name, string value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }

    private static async Task<bool> SqliteTableExistsAsync(DbConnection connection, string tableName, CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@name
            """;
        var p = cmd.CreateParameter();
        p.ParameterName = "@name";
        p.Value = tableName;
        cmd.Parameters.Add(p);
        var scalar = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt64(scalar) > 0;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
