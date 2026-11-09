using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Wortlet.Infrastructure.Persistence;

namespace Wortlet.IntegrationTests;

/// <summary>Ältere Installationen haben die Tabellen ohne Migrationsverlauf (EnsureCreated). Sie müssen beim Update erhalten bleiben.</summary>
public class MigrationTests(WortletFactory factory) : IClassFixture<WortletFactory>
{
    [Fact]
    public async Task Database_from_before_migrations_is_taken_over_and_upgraded()
    {
        // Datenbank im Stand „InitialCreate“ anlegen und dann den Verlauf löschen – so sah es vor den Migrationen aus.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SqliteAppDbContext>();
            var migrator = db.GetService<IMigrator>();
            var initial = db.Database.GetMigrations().First(m => m.EndsWith("_InitialCreate"));
            await migrator.MigrateAsync("0"); // alles zurück
            await migrator.MigrateAsync(initial);
            await db.Database.ExecuteSqlRawAsync("DROP TABLE \"__EFMigrationsHistory\"");
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO AspNetUsers (Id, CreatedAt, AccessFailedCount, EmailConfirmed, LockoutEnabled, PhoneNumberConfirmed, TwoFactorEnabled, Email) " +
                "VALUES ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '2026-01-01 00:00:00', 0, 0, 0, 0, 0, 'alt@example.test')");
        }

        await factory.Services.InitializeDatabaseAsync();

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SqliteAppDbContext>();
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            Assert.Equal(db.Database.GetMigrations(), await db.Database.GetAppliedMigrationsAsync());
            Assert.Equal(1, await db.Users.CountAsync(u => u.Email == "alt@example.test")); // Daten sind noch da
            Assert.Equal(0, await db.Groups.CountAsync()); // neue Tabellen gibt es
        }
        SqliteConnection.ClearAllPools();
    }
}
