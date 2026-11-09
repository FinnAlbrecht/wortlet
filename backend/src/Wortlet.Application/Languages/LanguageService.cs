using Wortlet.Application.Common;
using Wortlet.Domain.Common;
using Wortlet.Domain.Folders;
using Wortlet.Domain.Languages;

namespace Wortlet.Application.Languages;

public sealed class LanguageService(
    ILanguageRepository languages,
    IFolderRepository folders,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
{
    public async Task<IReadOnlyList<LanguageDto>> ListAsync(CancellationToken ct = default)
    {
        var userId = currentUser.UserId;
        var all = await languages.ListAsync(userId, ct);
        var summaries = await folders.ListSummariesAsync(userId, null, ct);
        return all
            .OrderBy(l => l.CreatedAt)
            .Select(l => ToDto(l, summaries))
            .ToList();
    }

    public async Task<LanguageDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var language = await Load(id, ct);
        var summaries = await folders.ListSummariesAsync(currentUser.UserId, id, ct);
        return ToDto(language, summaries);
    }

    /// <summary>Wie im Original: Gibt es die Sprache schon (gleicher Name), wird sie zurückgegeben.</summary>
    public async Task<LanguageDto> CreateAsync(SaveLanguageRequest request, CancellationToken ct = default)
    {
        var userId = currentUser.UserId;
        var name = request.Name.Trim();
        var existing = (await languages.ListAsync(userId, ct))
            .FirstOrDefault(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
            return await GetAsync(existing.Id, ct);

        var language = Language.Create(userId, name, clock.UtcNow);
        languages.Add(language);
        await unitOfWork.SaveChangesAsync(ct);
        return new LanguageDto(language.Id, language.Name, 0, 0, 0, language.CreatedAt);
    }

    public async Task<LanguageDto> RenameAsync(Guid id, SaveLanguageRequest request, CancellationToken ct = default)
    {
        var language = await Load(id, ct);
        var name = request.Name.Trim();
        var clash = (await languages.ListAsync(currentUser.UserId, ct))
            .Any(l => l.Id != id && string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase));
        if (clash)
            throw new ConflictException($"Es gibt schon eine Sprache mit dem Namen „{name}“.");

        language.Rename(name);
        await unitOfWork.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var language = await Load(id, ct);
        var count = await folders.CountInLanguageAsync(currentUser.UserId, id, ct);
        try
        {
            language.EnsureCanBeDeleted(count);
        }
        catch (DomainException ex)
        {
            throw new ConflictException(ex.Message);
        }
        languages.Remove(language);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<Language> Load(Guid id, CancellationToken ct) =>
        await languages.GetAsync(currentUser.UserId, id, ct)
        ?? throw new NotFoundException("Diese Sprache gibt es nicht.");

    private static LanguageDto ToDto(Language l, IReadOnlyList<FolderSummary> summaries)
    {
        var mine = summaries.Where(s => s.LanguageId == l.Id).ToList();
        return new LanguageDto(l.Id, l.Name, mine.Count, mine.Sum(s => s.WordCount), mine.Sum(s => s.WeakCount), l.CreatedAt);
    }
}
