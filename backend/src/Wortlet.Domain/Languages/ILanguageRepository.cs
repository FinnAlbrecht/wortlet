namespace Wortlet.Domain.Languages;

public interface ILanguageRepository
{
    Task<Language?> GetAsync(Guid ownerId, Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Language>> ListAsync(Guid ownerId, CancellationToken ct = default);
    void Add(Language language);
    void Remove(Language language);
}
