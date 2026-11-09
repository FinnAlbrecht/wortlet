using System.ComponentModel.DataAnnotations;
using Wortlet.Application.Common;
using ValidationException = Wortlet.Application.Common.ValidationException;
using Wortlet.Domain.Common;
using Wortlet.Domain.Folders;
using Wortlet.Domain.Languages;
using Wortlet.Domain.Sets;
using Wortlet.Domain.Vocabulary;

namespace Wortlet.Application.Sets;

public sealed record SetSummaryDto(Guid Id, Guid LanguageId, string Name, int WordCount, DateTime CreatedAt);

/// <summary>Ein Wort im Set – mit dem Ordner, aus dem es stammt (dorthin gehen die Antworten).</summary>
public sealed record SetWordDto(
    Guid FolderId, string FolderName, string Key, string German, IReadOnlyList<string> Answers, string Solution, string? Hint, bool IsWeak);

public sealed record SetDetailDto(Guid Id, Guid LanguageId, string LanguageName, string Name, IReadOnlyList<SetWordDto> Words);

public sealed record SetItemDto(
    [Required] Guid FolderId,
    [Required, StringLength(Word.MaxKeyLength)] string WordKey);

public sealed record CreateSetRequest(
    [Required(ErrorMessage = "Die Sprache fehlt.")] Guid LanguageId,
    [Required(ErrorMessage = "Bitte gib einen Namen ein.")]
    [StringLength(WordSet.MaxNameLength, ErrorMessage = "Der Name ist zu lang.")] string Name,
    [Required, MaxLength(WordSet.MaxItems, ErrorMessage = "Zu viele Wörter.")] IReadOnlyList<SetItemDto> Items);

public sealed record UpdateSetRequest(
    [Required(ErrorMessage = "Bitte gib einen Namen ein.")]
    [StringLength(WordSet.MaxNameLength, ErrorMessage = "Der Name ist zu lang.")] string Name,
    [Required, MaxLength(WordSet.MaxItems, ErrorMessage = "Zu viele Wörter.")] IReadOnlyList<SetItemDto> Items);

public sealed class SetService(
    IWordSetRepository sets,
    IFolderRepository folders,
    ILanguageRepository languages,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
{
    public async Task<IReadOnlyList<SetSummaryDto>> ListAsync(Guid languageId, CancellationToken ct = default)
    {
        await LoadLanguage(languageId, ct);
        var list = await sets.ListAsync(currentUser.UserId, languageId, ct);
        return list.OrderBy(s => s.CreatedAt).Select(s => new SetSummaryDto(s.Id, s.LanguageId, s.Name, s.ItemCount, s.CreatedAt)).ToList();
    }

    public async Task<SetDetailDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var set = await Load(id, ct);
        var language = await LoadLanguage(set.LanguageId, ct);
        var inLanguage = await FoldersOf(language.Id, ct);
        return new SetDetailDto(set.Id, set.LanguageId, language.Name, set.Name, Resolve(set, inLanguage));
    }

    public async Task<SetDetailDto> CreateAsync(CreateSetRequest request, CancellationToken ct = default)
    {
        var language = await LoadLanguage(request.LanguageId, ct);
        var inLanguage = await FoldersOf(language.Id, ct);
        var items = Validate(request.Items, inLanguage);
        var set = Run(() => WordSet.Create(currentUser.UserId, language.Id, request.Name, items, clock.UtcNow));
        sets.Add(set);
        await unitOfWork.SaveChangesAsync(ct);
        return new SetDetailDto(set.Id, set.LanguageId, language.Name, set.Name, Resolve(set, inLanguage));
    }

    public async Task<SetDetailDto> UpdateAsync(Guid id, UpdateSetRequest request, CancellationToken ct = default)
    {
        var set = await Load(id, ct);
        var language = await LoadLanguage(set.LanguageId, ct);
        var inLanguage = await FoldersOf(language.Id, ct);
        var items = Validate(request.Items, inLanguage);
        Run(() => { set.Update(request.Name, items, clock.UtcNow); return set; });
        await unitOfWork.SaveChangesAsync(ct);
        return new SetDetailDto(set.Id, set.LanguageId, language.Name, set.Name, Resolve(set, inLanguage));
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var set = await Load(id, ct);
        sets.Remove(set);
        await unitOfWork.SaveChangesAsync(ct);
    }

    /// <summary>Die Wörter eines Sets, soweit es sie in den Ordnern noch gibt (in der Reihenfolge des Sets).</summary>
    public static IReadOnlyList<SetWordDto> Resolve(WordSet set, IReadOnlyList<Folder> folderList)
    {
        var byId = folderList.ToDictionary(f => f.Id);
        var result = new List<SetWordDto>();
        foreach (var item in set.Items)
        {
            if (!byId.TryGetValue(item.FolderId, out var folder)) continue;
            if (folder.FindWord(item.WordKey) is not { } w) continue;
            result.Add(new SetWordDto(folder.Id, folder.Name, w.Key, AnswerEvaluator.BaseKey(w.Key), w.Answers,
                AnswerEvaluator.Solution(w.Answers), w.Hint, folder.IsWeak(w.Key)));
        }
        return result;
    }

    private static List<SetItemRef> Validate(IReadOnlyList<SetItemDto>? items, IReadOnlyList<Folder> inLanguage)
    {
        var byId = inLanguage.ToDictionary(f => f.Id);
        var result = new List<SetItemRef>();
        foreach (var item in items ?? [])
        {
            if (!byId.TryGetValue(item.FolderId, out var folder))
                throw new ValidationException("Ein Wort stammt aus einem Ordner, den es in dieser Sprache nicht gibt.");
            if (folder.FindWord(item.WordKey) is null)
                throw new ValidationException($"Das Wort „{item.WordKey}“ gibt es im Ordner „{folder.Name}“ nicht (mehr).");
            result.Add(new SetItemRef(item.FolderId, item.WordKey));
        }
        if (result.Count == 0)
            throw new ValidationException("Wähle mindestens ein Wort aus.");
        return result;
    }

    private async Task<IReadOnlyList<Folder>> FoldersOf(Guid languageId, CancellationToken ct) =>
        (await folders.ListWithContentAsync(currentUser.UserId, ct)).Where(f => f.LanguageId == languageId).ToList();

    private static T Run<T>(Func<T> action)
    {
        try { return action(); }
        catch (DomainException ex) { throw new ValidationException(ex.Message); }
    }

    private async Task<WordSet> Load(Guid id, CancellationToken ct) =>
        await sets.GetAsync(currentUser.UserId, id, ct) ?? throw new NotFoundException("Dieses Set gibt es nicht.");

    private async Task<Language> LoadLanguage(Guid id, CancellationToken ct) =>
        await languages.GetAsync(currentUser.UserId, id, ct) ?? throw new NotFoundException("Diese Sprache gibt es nicht.");
}
