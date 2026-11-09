using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Wortlet.Infrastructure.Storage;

namespace Wortlet.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    private const string InitialMigrationSuffix = "_InitialCreate";

    /// <summary>Spielt beim Start alle Migrationen ein (legt die Datenbank bei Bedarf an).</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var applied = (await db.Database.GetAppliedMigrationsAsync(ct)).ToList();
        if (applied.Count == 0 && await HasTablesFromBeforeMigrationsAsync(db, ct))
            await MarkInitialMigrationAsAppliedAsync(db, ct);

        var existing = applied.Count > 0 || (await db.Database.GetAppliedMigrationsAsync(ct)).Any();
        if (existing && (await db.Database.GetPendingMigrationsAsync(ct)).Any())
            await BackupBeforeMigrationAsync(scope.ServiceProvider, db, ct);

        await db.Database.MigrateAsync(ct);
    }

    /// <summary>
    /// Ältere Versionen legten die Tabellen ohne Migrationen an (EnsureCreated).
    /// Erkennbar daran, dass es die Tabelle „Folders“ gibt, aber keinen Migrationsverlauf.
    /// </summary>
    private static async Task<bool> HasTablesFromBeforeMigrationsAsync(AppDbContext db, CancellationToken ct)
    {
        if (!await db.Database.CanConnectAsync(ct)) return false;
        var sql = db is SqliteAppDbContext
            ? "SELECT COUNT(*) AS \"Value\" FROM sqlite_master WHERE type = 'table' AND name = 'Folders'"
            : "SELECT COUNT(*)::int AS \"Value\" FROM information_schema.tables WHERE table_name = 'Folders'";
        return await db.Database.SqlQueryRaw<int>(sql).SingleAsync(ct) > 0;
    }

    /// <summary>Trägt „InitialCreate“ als eingespielt ein – die Tabellen dieses Stands gibt es ja schon.</summary>
    private static async Task MarkInitialMigrationAsAppliedAsync(AppDbContext db, CancellationToken ct)
    {
        var initial = db.Database.GetMigrations().First(m => m.EndsWith(InitialMigrationSuffix, StringComparison.Ordinal));
        var history = db.GetService<IHistoryRepository>();
        await db.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript(), ct);
        await db.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(initial, ProductInfo.GetVersion())), ct);
    }

    /// <summary>Ändert ein Update die Tabellen einer bestehenden SQLite-Datenbank, wird vorher gesichert.</summary>
    private static async Task BackupBeforeMigrationAsync(IServiceProvider sp, AppDbContext db, CancellationToken ct)
    {
        if (db is not SqliteAppDbContext || !sp.GetRequiredService<IOptions<BackupOptions>>().Value.Enabled) return;
        await sp.GetRequiredService<SqliteBackupService>().CreateBackupAsync("vor-update", ct);
    }
}
