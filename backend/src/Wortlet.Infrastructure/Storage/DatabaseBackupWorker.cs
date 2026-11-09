using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wortlet.Application.Common;

namespace Wortlet.Infrastructure.Storage;

/// <summary>Erstellt im Hintergrund regelmässig Sicherungen (nur bei SQLite; PostgreSQL sichert der Anbieter).</summary>
public sealed class DatabaseBackupWorker(
    SqliteBackupService backups,
    IOptions<BackupOptions> options,
    IClock clock,
    IConfiguration configuration,
    ILogger<DatabaseBackupWorker> logger) : BackgroundService
{
    private static readonly TimeSpan RetryAfterError = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled || DependencyInjection.IsPostgres(configuration)) return;

        var interval = TimeSpan.FromHours(Math.Max(0.01, options.Value.IntervalHours));
        logger.LogInformation("Automatische Sicherung aktiv: alle {Hours} h nach {Directory}, {Keep} werden behalten.",
            interval.TotalHours, backups.BackupDirectory, options.Value.KeepCount);

        while (!stoppingToken.IsCancellationRequested)
        {
            var wait = interval;
            try
            {
                var last = backups.LastBackupUtc();
                var due = last is null ? TimeSpan.Zero : last.Value + interval - clock.UtcNow;
                if (due <= TimeSpan.Zero)
                {
                    var file = await backups.CreateBackupAsync(ct: stoppingToken);
                    logger.LogInformation("Sicherung erstellt: {File}", file);
                }
                else
                {
                    wait = due;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Sicherung fehlgeschlagen, neuer Versuch in {Minutes} min.", RetryAfterError.TotalMinutes);
                wait = RetryAfterError;
            }

            try { await Task.Delay(wait, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }
}
