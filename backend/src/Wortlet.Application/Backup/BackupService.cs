using Wortlet.Application.Common;
using Wortlet.Domain.Common;
using Wortlet.Domain.Folders;
using Wortlet.Domain.Languages;

namespace Wortlet.Application.Backup;

public sealed class BackupService(
    ILanguageRepository languages,
    IFolderRepository folders,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
{
    public const int MaxLanguages = 100;
    public const int MaxFolders = 1000;

    public async Task<BackupDocument> ExportAsync(CancellationToken ct = default)
    {
        var userId = currentUser.UserId;
        var langs = await languages.ListAsync(userId, ct);
        var all = await folders.ListWithContentAsync(userId, ct);
        return new BackupDocument(
            BackupDocument.CurrentVersion,
            BackupDocument.AppName,
            clock.UtcNow,
            langs.OrderBy(l => l.CreatedAt).Select(l => new BackupLanguage(
                l.Name,
                all.Where(f => f.LanguageId == l.Id).OrderBy(f => f.CreatedAt).Select(ToBackup).ToList())).ToList());
    }

    /// <summary>Ersetzt ALLE Sprachen, Ordner und Fehlerwörter des Benutzers durch den Inhalt der Datei.</summary>
    public async Task<ImportResultDto> ImportAsync(BackupDocument? document, CancellationToken ct = default)
    {
        var (newLanguages, newFolders) = Build(document);

        var userId = currentUser.UserId;
        foreach (var folder in await folders.ListWithContentAsync(userId, ct))
            folders.Remove(folder);
        foreach (var language in await languages.ListAsync(userId, ct))
            languages.Remove(language);

        foreach (var language in newLanguages)
            languages.Add(language);
        foreach (var folder in newFolders)
            folders.Add(folder);

        await unitOfWork.SaveChangesAsync(ct);
        return new ImportResultDto(
            newLanguages.Count,
            newFolders.Count,
            newFolders.Sum(f => f.WordCount),
            newFolders.Sum(f => f.WeakWords.Count));
    }

    /// <summary>Prüft die Datei vollständig, bevor etwas gelöscht wird.</summary>
    internal (List<Language> Languages, List<Folder> Folders) Build(BackupDocument? document)
    {
        if (document is null || document.Languages is null)
            throw new ValidationException("Die Datei ist keine gültige Sicherung des Vokabeltrainers.");
        if (document.Version != BackupDocument.CurrentVersion)
            throw new ValidationException($"Diese Sicherung hat die Version {document.Version}. Unterstützt wird Version {BackupDocument.CurrentVersion}.");
        if (document.Languages.Count > MaxLanguages)
            throw new ValidationException($"Die Sicherung enthält zu viele Sprachen (höchstens {MaxLanguages}).");
        if (document.Languages.Sum(l => l.Folders?.Count ?? 0) > MaxFolders)
            throw new ValidationException($"Die Sicherung enthält zu viele Ordner (höchstens {MaxFolders}).");

        var userId = currentUser.UserId;
        var now = clock.UtcNow;
        var langs = new List<Language>();
        var list = new List<Folder>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var order = 0;

        foreach (var bl in document.Languages)
        {
            var language = Wrap($"Sprache „{bl?.Name}“", () => Language.Create(userId, bl?.Name ?? string.Empty, now.AddMilliseconds(order++)));
            if (!names.Add(language.Name))
                throw new ValidationException($"Die Sprache „{language.Name}“ kommt in der Datei doppelt vor.");
            langs.Add(language);

            foreach (var bf in bl!.Folders ?? [])
            {
                var folder = Wrap($"Ordner „{bf?.Name}“", () =>
                {
                    var chapters = (bf!.Chapters ?? [])
                        .Select(c => new ChapterDraft(
                            c?.Name ?? string.Empty,
                            c?.Pages ?? string.Empty,
                            c?.IsNew ?? false,
                            (c?.Words ?? []).Select(w => new WordDraft(w?.De ?? string.Empty, w?.Answers ?? [], w?.Hint)).ToList()))
                        .ToList();
                    var created = Folder.Create(
                        userId, language.Id, bf.Name, chapters,
                        bf.CreatedAt ?? now.AddMilliseconds(order++),
                        bf.Description, bf.OldLabel, bf.NewLabel, bf.IsSeed);
                    created.RestoreWeakWords(bf.WeakWords ?? [], now);
                    return created;
                });
                list.Add(folder);
            }
        }
        return (langs, list);
    }

    private static T Wrap<T>(string context, Func<T> build)
    {
        try { return build(); }
        catch (DomainException ex) { throw new ValidationException($"{context}: {ex.Message}"); }
        catch (NullReferenceException) { throw new ValidationException($"{context}: Die Angaben sind unvollständig."); }
    }

    private static BackupFolder ToBackup(Folder f) => new(
        f.Name,
        f.Description,
        f.OldLabel,
        f.NewLabel,
        f.IsSeed,
        f.CreatedAt,
        f.Chapters.Select(c => new BackupChapter(
            c.Name,
            c.Pages,
            c.IsNew,
            c.Words.Select(w => new BackupWord(w.Key, w.Answers, w.Hint)).ToList())).ToList(),
        f.WeakWordList().Select(w => w.Key).ToList());
}
