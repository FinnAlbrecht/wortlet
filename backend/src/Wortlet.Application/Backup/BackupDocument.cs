namespace Wortlet.Application.Backup;

/// <summary>Format der Datei „Daten exportieren“. Version 1.</summary>
public sealed record BackupDocument(
    int Version,
    string App,
    DateTime ExportedAt,
    IReadOnlyList<BackupLanguage> Languages)
{
    public const int CurrentVersion = 1;
    public const string AppName = "Wortlet";
}

public sealed record BackupLanguage(string Name, IReadOnlyList<BackupFolder> Folders);

public sealed record BackupFolder(
    string Name,
    string? Description,
    string? OldLabel,
    string? NewLabel,
    bool IsSeed,
    DateTime? CreatedAt,
    IReadOnlyList<BackupChapter> Chapters,
    IReadOnlyList<string>? WeakWords);

public sealed record BackupChapter(string Name, string? Pages, bool IsNew, IReadOnlyList<BackupWord> Words);

public sealed record BackupWord(string De, IReadOnlyList<string> Answers, string? Hint);

public sealed record ImportResultDto(int Languages, int Folders, int Words, int WeakWords);
