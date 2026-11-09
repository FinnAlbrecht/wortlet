using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Wortlet.Application.Common;
using Wortlet.Infrastructure.Persistence;

namespace Wortlet.Infrastructure.Storage;

/// <summary>
/// Kopiert die SQLite-Datenbank über die Online-Backup-Schnittstelle von SQLite.
/// Das geht auch, während die App läuft, und ergibt immer eine in sich stimmige Datei.
/// </summary>
public sealed class SqliteBackupService(
    IServiceScopeFactory scopes,
    DataDirectory dataDirectory,
    IOptions<BackupOptions> options,
    IClock clock)
{
    public const string FilePrefix = "wortlet-";

    public string BackupDirectory
    {
        get
        {
            var dir = options.Value.Directory;
            if (string.IsNullOrWhiteSpace(dir)) return Path.Combine(dataDirectory.Path, "backups");
            return Path.IsPathRooted(dir) ? dir : Path.Combine(dataDirectory.Path, dir);
        }
    }

    /// <summary>Erstellt eine Sicherung und räumt alte auf. Gibt den Pfad der neuen Datei zurück.</summary>
    public async Task<string> CreateBackupAsync(string? label = null, CancellationToken ct = default)
    {
        string connectionString;
        await using (var scope = scopes.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SqliteAppDbContext>();
            connectionString = db.Database.GetConnectionString()
                ?? throw new InvalidOperationException("Keine SQLite-Verbindung konfiguriert.");
        }

        Directory.CreateDirectory(BackupDirectory);
        var name = FilePrefix + clock.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)
            + (string.IsNullOrWhiteSpace(label) ? "" : "-" + label) + ".db";
        var target = Path.Combine(BackupDirectory, name);
        var temp = target + ".tmp";

        await using (var source = new SqliteConnection(connectionString))
        await using (var destination = new SqliteConnection(
            new SqliteConnectionStringBuilder { DataSource = temp, Pooling = false }.ToString()))
        {
            await source.OpenAsync(ct);
            await destination.OpenAsync(ct);
            source.BackupDatabase(destination);

            // Die Kopie übernimmt sonst den WAL-Modus und legt beim Öffnen -wal/-shm-Dateien an.
            await using var journal = destination.CreateCommand();
            journal.CommandText = "PRAGMA journal_mode=DELETE;";
            await journal.ExecuteNonQueryAsync(ct);
        }

        // Erst umbenennen, wenn die Kopie fertig ist – so liegt nie eine halbe Sicherung im Ordner.
        File.Move(temp, target, overwrite: true);
        Prune();
        return target;
    }

    /// <summary>Zeitpunkt der neuesten Sicherung oder null, wenn es noch keine gibt.</summary>
    public DateTime? LastBackupUtc() =>
        ListBackups().Select(f => (DateTime?)File.GetLastWriteTimeUtc(f)).FirstOrDefault();

    /// <summary>Alle Sicherungen, neueste zuerst.</summary>
    public IReadOnlyList<string> ListBackups() =>
        Directory.Exists(BackupDirectory)
            ? Directory.GetFiles(BackupDirectory, FilePrefix + "*.db")
                .Where(f => f.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(Path.GetFileName, StringComparer.Ordinal)
                .ToList()
            : [];

    private void Prune()
    {
        var keep = Math.Max(1, options.Value.KeepCount);
        foreach (var old in ListBackups().Skip(keep))
        {
            try { File.Delete(old); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }
}
