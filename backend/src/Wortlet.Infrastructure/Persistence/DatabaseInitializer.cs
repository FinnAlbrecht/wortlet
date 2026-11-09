using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Wortlet.Infrastructure.Storage;

namespace Wortlet.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>Spielt beim Start alle Migrationen ein (legt die Datenbank bei Bedarf an).</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (db.Database.GetMigrations().Any())
        {
            await BackupBeforeMigrationAsync(scope.ServiceProvider, db, ct);
            await db.Database.MigrateAsync(ct);
        }
        else
        {
            await db.Database.EnsureCreatedAsync(ct); // Notlösung, solange noch keine Migration erzeugt wurde
        }
    }

    /// <summary>Ändert ein Update die Tabellen einer bestehenden SQLite-Datenbank, wird vorher gesichert.</summary>
    private static async Task BackupBeforeMigrationAsync(IServiceProvider sp, AppDbContext db, CancellationToken ct)
    {
        if (db is not SqliteAppDbContext || !sp.GetRequiredService<IOptions<BackupOptions>>().Value.Enabled) return;
        if (!(await db.Database.GetAppliedMigrationsAsync(ct)).Any()) return; // neue, leere Datenbank
        if (!(await db.Database.GetPendingMigrationsAsync(ct)).Any()) return;

        await sp.GetRequiredService<SqliteBackupService>().CreateBackupAsync("vor-update", ct);
    }
}
