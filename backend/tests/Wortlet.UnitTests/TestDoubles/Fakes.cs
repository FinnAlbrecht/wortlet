using Wortlet.Application.Common;
using Wortlet.Domain.Common;
using Wortlet.Domain.Folders;
using Wortlet.Domain.Languages;

namespace Wortlet.UnitTests.TestDoubles;

internal sealed class FakeClock : IClock
{
    public DateTime UtcNow { get; set; } = TestData.Now;
}

internal sealed class FakeCurrentUser(Guid id) : ICurrentUser
{
    public Guid UserId { get; } = id;
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int Saves { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        Saves++;
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryLanguages : ILanguageRepository
{
    public List<Language> Items { get; } = [];

    public Task<Language?> GetAsync(Guid ownerId, Guid id, CancellationToken ct = default) =>
        Task.FromResult(Items.FirstOrDefault(l => l.OwnerId == ownerId && l.Id == id));

    public Task<IReadOnlyList<Language>> ListAsync(Guid ownerId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Language>>(Items.Where(l => l.OwnerId == ownerId).ToList());

    public void Add(Language language) => Items.Add(language);

    public void Remove(Language language) => Items.Remove(language);
}

internal sealed class InMemoryFolders : IFolderRepository
{
    public List<Folder> Items { get; } = [];

    public Task<Folder?> GetAsync(Guid ownerId, Guid id, CancellationToken ct = default) =>
        Task.FromResult(Items.FirstOrDefault(f => f.OwnerId == ownerId && f.Id == id));

    public Task<IReadOnlyList<Folder>> ListWithContentAsync(Guid ownerId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Folder>>(Items.Where(f => f.OwnerId == ownerId).ToList());

    public Task<IReadOnlyList<FolderSummary>> ListSummariesAsync(Guid ownerId, Guid? languageId = null, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<FolderSummary>>(Items
            .Where(f => f.OwnerId == ownerId && (languageId == null || f.LanguageId == languageId))
            .Select(f => new FolderSummary(f.Id, f.LanguageId, f.Name, f.Description, f.IsSeed, f.Chapters.Count, f.WordCount, f.WeakWords.Count, f.CreatedAt))
            .ToList());

    public Task<int> CountInLanguageAsync(Guid ownerId, Guid languageId, CancellationToken ct = default) =>
        Task.FromResult(Items.Count(f => f.OwnerId == ownerId && f.LanguageId == languageId));

    public void Add(Folder folder) => Items.Add(folder);

    public void Remove(Folder folder) => Items.Remove(folder);
}
