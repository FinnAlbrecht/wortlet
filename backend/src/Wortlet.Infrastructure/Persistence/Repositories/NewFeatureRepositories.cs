using Microsoft.EntityFrameworkCore;
using Wortlet.Domain.Groups;
using Wortlet.Domain.Profiles;
using Wortlet.Domain.Sets;
using Wortlet.Domain.Statistics;

namespace Wortlet.Infrastructure.Persistence.Repositories;

internal sealed class ProfileRepository(AppDbContext db) : IProfileRepository
{
    public async Task<UserProfile?> GetAsync(Guid userId, CancellationToken ct = default) =>
        db.Profiles.Local.FirstOrDefault(p => p.Id == userId)
        ?? await db.Profiles.FirstOrDefaultAsync(p => p.Id == userId, ct);

    public async Task<IReadOnlyList<UserProfile>> ListAsync(IEnumerable<Guid> userIds, CancellationToken ct = default)
    {
        var ids = userIds.Distinct().ToList();
        return await db.Profiles.Where(p => ids.Contains(p.Id)).ToListAsync(ct);
    }

    public void Add(UserProfile profile) => db.Profiles.Add(profile);

    public Task<AvatarImage?> GetImageAsync(Guid userId, CancellationToken ct = default) =>
        db.AvatarImages.FirstOrDefaultAsync(i => i.Id == userId, ct);

    public void AddImage(AvatarImage image) => db.AvatarImages.Add(image);

    public void RemoveImage(AvatarImage image) => db.AvatarImages.Remove(image);
}

internal sealed class StatisticsRepository(AppDbContext db) : IStatisticsRepository
{
    // Local zuerst: Innerhalb einer Anfrage kann ein Eintrag schon angelegt, aber noch nicht gespeichert sein.
    public async Task<WordStat?> GetWordStatAsync(Guid ownerId, Guid folderId, string wordKey, CancellationToken ct = default) =>
        db.WordStats.Local.FirstOrDefault(s => s.OwnerId == ownerId && s.FolderId == folderId && s.WordKey == wordKey)
        ?? await db.WordStats.FirstOrDefaultAsync(s => s.OwnerId == ownerId && s.FolderId == folderId && s.WordKey == wordKey, ct);

    public async Task<PracticeDay?> GetDayAsync(Guid ownerId, DateOnly day, CancellationToken ct = default) =>
        db.PracticeDays.Local.FirstOrDefault(d => d.OwnerId == ownerId && d.Day == day)
        ?? await db.PracticeDays.FirstOrDefaultAsync(d => d.OwnerId == ownerId && d.Day == day, ct);

    public void Add(WordStat stat) => db.WordStats.Add(stat);
    public void Add(PracticeDay day) => db.PracticeDays.Add(day);
    public void Add(QuizResult result) => db.QuizResults.Add(result);

    public async Task<IReadOnlyList<PracticeDay>> ListDaysAsync(Guid ownerId, CancellationToken ct = default) =>
        await db.PracticeDays.Where(d => d.OwnerId == ownerId).OrderBy(d => d.Day).ToListAsync(ct);

    public async Task<IReadOnlyList<PracticeDay>> ListDaysAsync(IReadOnlyCollection<Guid> ownerIds, DateOnly from, CancellationToken ct = default)
    {
        var ids = ownerIds.ToList();
        return await db.PracticeDays.Where(d => ids.Contains(d.OwnerId) && d.Day >= from).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<QuizResult>> ListResultsAsync(Guid ownerId, int take, CancellationToken ct = default) =>
        await db.QuizResults.Where(r => r.OwnerId == ownerId).OrderByDescending(r => r.FinishedAt).Take(take).ToListAsync(ct);

    public async Task<StatTotals> TotalsAsync(Guid ownerId, CancellationToken ct = default)
    {
        var words = await db.WordStats.CountAsync(s => s.OwnerId == ownerId, ct);
        var grades = await db.QuizResults.Where(r => r.OwnerId == ownerId).Select(r => r.Grade).ToListAsync(ct);
        return new StatTotals(words, grades.Count, grades.Count > 0 ? grades.Average() : null);
    }

    public async Task<IReadOnlyList<HardWord>> HardestAsync(Guid ownerId, Guid? languageId, int take, CancellationToken ct = default)
    {
        var query = db.WordStats.Where(s => s.OwnerId == ownerId && s.Wrong > 0);
        if (languageId is Guid lid)
            query = query.Where(s => db.Folders.Any(f => f.Id == s.FolderId && f.LanguageId == lid));

        // Etwas mehr holen: Wörter, die inzwischen geändert wurden, fallen danach weg.
        var rows = await query
            .OrderByDescending(s => (double)s.Wrong / s.Attempts)
            .ThenByDescending(s => s.Wrong)
            .Take(take * 2)
            .Select(s => new HardWord(s.FolderId, s.WordKey, s.Attempts, s.Wrong))
            .ToListAsync(ct);
        return rows;
    }
}

internal sealed class WordSetRepository(AppDbContext db) : IWordSetRepository
{
    public Task<WordSet?> GetAsync(Guid ownerId, Guid id, CancellationToken ct = default) =>
        db.WordSets.FirstOrDefaultAsync(s => s.OwnerId == ownerId && s.Id == id, ct);

    public async Task<IReadOnlyList<WordSetSummary>> ListAsync(Guid ownerId, Guid languageId, CancellationToken ct = default) =>
        await db.WordSets
            .Where(s => s.OwnerId == ownerId && s.LanguageId == languageId)
            .OrderBy(s => s.CreatedAt)
            .Select(s => new WordSetSummary(s.Id, s.LanguageId, s.Name, s.Items.Count(), s.CreatedAt))
            .ToListAsync(ct);

    public void Add(WordSet set) => db.WordSets.Add(set);

    public void Remove(WordSet set) => db.WordSets.Remove(set);
}

internal sealed class GroupRepository(AppDbContext db) : IGroupRepository
{
    public Task<Group?> GetAsync(Guid id, CancellationToken ct = default) =>
        db.Groups.FirstOrDefaultAsync(g => g.Id == id, ct);

    public Task<Group?> GetByCodeAsync(string joinCode, CancellationToken ct = default) =>
        db.Groups.FirstOrDefaultAsync(g => g.JoinCode == joinCode, ct);

    public async Task<IReadOnlyList<Group>> ListForUserAsync(Guid userId, CancellationToken ct = default) =>
        await db.Groups.Where(g => g.Members.Any(m => m.UserId == userId)).ToListAsync(ct);

    public async Task<IReadOnlyList<Group>> ListRequestedByUserAsync(Guid userId, CancellationToken ct = default) =>
        await db.Groups.Where(g => g.JoinRequests.Any(r => r.UserId == userId)).ToListAsync(ct);

    // Gefiltert wird danach im Speicher (Gross-/Kleinschreibung und Umlaute zuverlässig, unabhängig von der Datenbank).
    public async Task<IReadOnlyList<Group>> ListSearchableAsync(CancellationToken ct = default) =>
        await db.Groups.Where(g => g.Visibility != GroupVisibility.Private).ToListAsync(ct);

    public Task<bool> CodeExistsAsync(string joinCode, CancellationToken ct = default) =>
        db.Groups.AnyAsync(g => g.JoinCode == joinCode, ct);

    public void Add(Group group) => db.Groups.Add(group);

    public void Remove(Group group) => db.Groups.Remove(group);
}
