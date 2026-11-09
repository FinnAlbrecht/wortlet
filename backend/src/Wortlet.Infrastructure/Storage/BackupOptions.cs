namespace Wortlet.Infrastructure.Storage;

/// <summary>Automatische Sicherungen der SQLite-Datenbank (Abschnitt „Backup“).</summary>
public sealed class BackupOptions
{
    public const string SectionName = "Backup";

    public bool Enabled { get; set; } = true;

    /// <summary>Abstand zwischen zwei Sicherungen.</summary>
    public double IntervalHours { get; set; } = 24;

    /// <summary>So viele Sicherungen bleiben liegen, ältere werden gelöscht.</summary>
    public int KeepCount { get; set; } = 14;

    /// <summary>Zielordner; leer = Unterordner „backups“ im Datenordner. Am besten ein anderes Laufwerk/Volume.</summary>
    public string? Directory { get; set; }
}
