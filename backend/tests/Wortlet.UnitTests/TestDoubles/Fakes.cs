using Wortlet.Application.Common;
using Wortlet.Domain.Common;
using Wortlet.Domain.Folders;
using Wortlet.Domain.Groups;
using Wortlet.Domain.Languages;
using Wortlet.Domain.Sets;
using Wortlet.Domain.Statistics;

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

    public Task<Folder?> GetCopyOfShareAsync(Guid ownerId, Guid shareId, CancellationToken ct = default) =>
        Task.FromResult(Items.FirstOrDefault(f => f.OwnerId == ownerId && f.SourceShareId == shareId));

    public Task<IReadOnlyDictionary<Guid, Guid>> ListCopiedSharesAsync(Guid ownerId, IReadOnlyCollection<Guid> shareIds, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, Guid>>(Items
            .Where(f => f.OwnerId == ownerId && f.SourceShareId is Guid s && shareIds.Contains(s))
            .ToDictionary(f => f.SourceShareId!.Value, f => f.Id));

    public void Add(Folder folder) => Items.Add(folder);

    public void Remove(Folder folder) => Items.Remove(folder);
}

internal sealed class InMemoryStatistics : IStatisticsRepository
{
    public List<WordStat> WordStats { get; } = [];
    public List<PracticeDay> Days { get; } = [];
    public List<QuizResult> Results { get; } = [];

    public Task<WordStat?> GetWordStatAsync(Guid ownerId, Guid folderId, string wordKey, CancellationToken ct = default) =>
        Task.FromResult(WordStats.FirstOrDefault(s => s.OwnerId == ownerId && s.FolderId == folderId && s.WordKey == wordKey));

    public Task<PracticeDay?> GetDayAsync(Guid ownerId, DateOnly day, CancellationToken ct = default) =>
        Task.FromResult(Days.FirstOrDefault(d => d.OwnerId == ownerId && d.Day == day));

    public void Add(WordStat stat) => WordStats.Add(stat);
    public void Add(PracticeDay day) => Days.Add(day);
    public void Add(QuizResult result) => Results.Add(result);

    public Task<IReadOnlyList<PracticeDay>> ListDaysAsync(Guid ownerId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<PracticeDay>>(Days.Where(d => d.OwnerId == ownerId).OrderBy(d => d.Day).ToList());

    public Task<IReadOnlyList<PracticeDay>> ListDaysAsync(IReadOnlyCollection<Guid> ownerIds, DateOnly from, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<PracticeDay>>(Days.Where(d => ownerIds.Contains(d.OwnerId) && d.Day >= from).ToList());

    public Task<IReadOnlyList<QuizResult>> ListResultsAsync(Guid ownerId, int take, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<QuizResult>>(Results.Where(r => r.OwnerId == ownerId).OrderByDescending(r => r.FinishedAt).Take(take).ToList());

    public Task<StatTotals> TotalsAsync(Guid ownerId, CancellationToken ct = default)
    {
        var grades = Results.Where(r => r.OwnerId == ownerId).Select(r => r.Grade).ToList();
        return Task.FromResult(new StatTotals(WordStats.Count(s => s.OwnerId == ownerId), grades.Count, grades.Count > 0 ? grades.Average() : null));
    }

    public Task<IReadOnlyList<HardWord>> HardestAsync(Guid ownerId, Guid? languageId, int take, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<HardWord>>(WordStats
            .Where(s => s.OwnerId == ownerId && s.Wrong > 0)
            .Select(s => new HardWord(s.FolderId, s.WordKey, s.Attempts, s.Wrong))
            .Take(take * 2)
            .ToList());
}

internal sealed class InMemoryWordSets : IWordSetRepository
{
    public List<WordSet> Items { get; } = [];

    public Task<WordSet?> GetAsync(Guid ownerId, Guid id, CancellationToken ct = default) =>
        Task.FromResult(Items.FirstOrDefault(s => s.OwnerId == ownerId && s.Id == id));

    public Task<IReadOnlyList<WordSetSummary>> ListAsync(Guid ownerId, Guid languageId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<WordSetSummary>>(Items
            .Where(s => s.OwnerId == ownerId && s.LanguageId == languageId)
            .Select(s => new WordSetSummary(s.Id, s.LanguageId, s.Name, s.Items.Count, s.CreatedAt))
            .ToList());

    public void Add(WordSet set) => Items.Add(set);

    public void Remove(WordSet set) => Items.Remove(set);
}

internal sealed class InMemoryGroups : IGroupRepository
{
    public List<Group> Items { get; } = [];

    public Task<Group?> GetAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Items.FirstOrDefault(g => g.Id == id));

    public Task<Group?> GetByCodeAsync(string joinCode, CancellationToken ct = default) =>
        Task.FromResult(Items.FirstOrDefault(g => g.JoinCode == joinCode));

    public Task<IReadOnlyList<Group>> ListForUserAsync(Guid userId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Group>>(Items.Where(g => g.IsMember(userId)).ToList());

    public Task<IReadOnlyList<Group>> ListRequestedByUserAsync(Guid userId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Group>>(Items.Where(g => g.HasRequested(userId)).ToList());

    public Task<IReadOnlyList<Group>> ListSearchableAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Group>>(Items.Where(g => g.IsSearchable).ToList());

    public Task<bool> CodeExistsAsync(string joinCode, CancellationToken ct = default) => Task.FromResult(Items.Any(g => g.JoinCode == joinCode));

    public void Add(Group group) => Items.Add(group);

    public void Remove(Group group) => Items.Remove(group);
}
