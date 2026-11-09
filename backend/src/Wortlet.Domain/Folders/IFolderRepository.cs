namespace Wortlet.Domain.Folders;

/// <summary>Kennzahlen eines Ordners für Übersichten, ohne den ganzen Inhalt zu laden.</summary>
public sealed record FolderSummary(
    Guid Id,
    Guid LanguageId,
    string Name,
    string Description,
    bool IsSeed,
    int ChapterCount,
    int WordCount,
    int WeakCount,
    DateTime CreatedAt);

public interface IFolderRepository
{
    /// <summary>Lädt den Ordner vollständig (Kapitel, Wörter, Fehlerwörter).</summary>
    Task<Folder?> GetAsync(Guid ownerId, Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Folder>> ListWithContentAsync(Guid ownerId, CancellationToken ct = default);
    Task<IReadOnlyList<FolderSummary>> ListSummariesAsync(Guid ownerId, Guid? languageId = null, CancellationToken ct = default);
    Task<int> CountInLanguageAsync(Guid ownerId, Guid languageId, CancellationToken ct = default);
    void Add(Folder folder);
    void Remove(Folder folder);
}
