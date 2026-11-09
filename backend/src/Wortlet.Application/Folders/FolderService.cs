using Wortlet.Application.Common;
using Wortlet.Domain.Common;
using Wortlet.Domain.Folders;
using Wortlet.Domain.Languages;
using Wortlet.Domain.Vocabulary;

namespace Wortlet.Application.Folders;

public sealed class FolderService(
    IFolderRepository folders,
    ILanguageRepository languages,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
{
    public async Task<IReadOnlyList<FolderSummaryDto>> ListAsync(Guid languageId, CancellationToken ct = default)
    {
        await LoadLanguage(languageId, ct);
        var list = await folders.ListSummariesAsync(currentUser.UserId, languageId, ct);
        return list.OrderBy(f => f.CreatedAt).Select(f => f.ToDto()).ToList();
    }

    public async Task<FolderDetailDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var folder = await LoadFolder(id, ct);
        var language = await LoadLanguage(folder.LanguageId, ct);
        return folder.ToDetail(language);
    }

    public async Task<FolderDetailDto> CreateAsync(CreateFolderRequest request, CancellationToken ct = default)
    {
        var language = await LoadLanguage(request.LanguageId, ct);
        var chapters = ParseOrThrow(request.Text);
        var folder = Run(() => Folder.Create(currentUser.UserId, language.Id, request.Name, chapters, clock.UtcNow));
        folders.Add(folder);
        await unitOfWork.SaveChangesAsync(ct);
        return folder.ToDetail(language);
    }

    public async Task<FolderDetailDto> UpdateAsync(Guid id, UpdateFolderRequest request, CancellationToken ct = default)
    {
        var folder = await LoadFolder(id, ct);
        var language = await LoadLanguage(folder.LanguageId, ct);
        var chapters = ParseOrThrow(request.Text);
        Run(() =>
        {
            folder.Rename(request.Name, clock.UtcNow);
            folder.ReplaceContent(chapters, clock.UtcNow);
            return folder;
        });
        await unitOfWork.SaveChangesAsync(ct);
        return folder.ToDetail(language);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var folder = await LoadFolder(id, ct);
        folders.Remove(folder);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private static IReadOnlyList<ChapterDraft> ParseOrThrow(string text)
    {
        var parsed = VocabularyText.Parse(text);
        if (parsed.WordCount == 0)
            throw new ValidationException("Es wurde kein Wort erkannt. Schreibe eine Zeile pro Wort: Deutsch = Fremdsprache.",
                new Dictionary<string, string[]> { ["text"] = ["Kein Wort erkannt."] });
        return parsed.Chapters;
    }

    private static T Run<T>(Func<T> action)
    {
        try { return action(); }
        catch (DomainException ex) { throw new ValidationException(ex.Message); }
    }

    private async Task<Folder> LoadFolder(Guid id, CancellationToken ct) =>
        await folders.GetAsync(currentUser.UserId, id, ct)
        ?? throw new NotFoundException("Diesen Ordner gibt es nicht.");

    private async Task<Language> LoadLanguage(Guid id, CancellationToken ct) =>
        await languages.GetAsync(currentUser.UserId, id, ct)
        ?? throw new NotFoundException("Diese Sprache gibt es nicht.");
}
