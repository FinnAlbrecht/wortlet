using Microsoft.EntityFrameworkCore;
using Wortlet.Domain.Folders;
using Wortlet.Domain.Languages;
using Wortlet.Domain.Recognition;

namespace Wortlet.Infrastructure.Persistence.Repositories;

internal sealed class LanguageRepository(AppDbContext db) : ILanguageRepository
{
    public Task<Language?> GetAsync(Guid ownerId, Guid id, CancellationToken ct = default) =>
        db.Languages.FirstOrDefaultAsync(l => l.OwnerId == ownerId && l.Id == id, ct);

    public async Task<IReadOnlyList<Language>> ListAsync(Guid ownerId, CancellationToken ct = default) =>
        await db.Languages.Where(l => l.OwnerId == ownerId).OrderBy(l => l.CreatedAt).ToListAsync(ct);

    public void Add(Language language) => db.Languages.Add(language);

    public void Remove(Language language) => db.Languages.Remove(language);
}

internal sealed class FolderRepository(AppDbContext db) : IFolderRepository
{
    private IQueryable<Folder> WithContent() =>
        db.Folders.Include(f => f.Chapters).ThenInclude(c => c.Words).AsSplitQuery();

    public Task<Folder?> GetAsync(Guid ownerId, Guid id, CancellationToken ct = default) =>
        WithContent().FirstOrDefaultAsync(f => f.OwnerId == ownerId && f.Id == id, ct);

    public async Task<IReadOnlyList<Folder>> ListWithContentAsync(Guid ownerId, CancellationToken ct = default) =>
        await WithContent().Where(f => f.OwnerId == ownerId).ToListAsync(ct);

    public async Task<IReadOnlyList<FolderSummary>> ListSummariesAsync(Guid ownerId, Guid? languageId = null, CancellationToken ct = default)
    {
        var query = db.Folders.Where(f => f.OwnerId == ownerId);
        if (languageId is Guid lid)
            query = query.Where(f => f.LanguageId == lid);

        return await query
            .OrderBy(f => f.CreatedAt)
            .Select(f => new FolderSummary(
                f.Id,
                f.LanguageId,
                f.Name,
                f.Description,
                f.IsSeed,
                f.Chapters.Count(),
                f.Chapters.SelectMany(c => c.Words).Count(),
                f.WeakWords.Count(),
                f.CreatedAt))
            .ToListAsync(ct);
    }

    public Task<Folder?> GetCopyOfShareAsync(Guid ownerId, Guid shareId, CancellationToken ct = default) =>
        WithContent().FirstOrDefaultAsync(f => f.OwnerId == ownerId && f.SourceShareId == shareId, ct);

    public async Task<IReadOnlyDictionary<Guid, Guid>> ListCopiedSharesAsync(Guid ownerId, IReadOnlyCollection<Guid> shareIds, CancellationToken ct = default)
    {
        if (shareIds.Count == 0) return new Dictionary<Guid, Guid>();
        var ids = shareIds.Select(id => (Guid?)id).ToList();
        var rows = await db.Folders
            .Where(f => f.OwnerId == ownerId && ids.Contains(f.SourceShareId))
            .Select(f => new { ShareId = f.SourceShareId!.Value, f.Id })
            .ToListAsync(ct);
        return rows.GroupBy(r => r.ShareId).ToDictionary(g => g.Key, g => g.First().Id);
    }

    public Task<int> CountInLanguageAsync(Guid ownerId, Guid languageId, CancellationToken ct = default) =>
        db.Folders.CountAsync(f => f.OwnerId == ownerId && f.LanguageId == languageId, ct);

    public void Add(Folder folder) => db.Folders.Add(folder);

    public void Remove(Folder folder) => db.Folders.Remove(folder);
}

internal sealed class RecognitionUsageRepository(AppDbContext db) : IRecognitionUsageRepository
{
    public Task<RecognitionUsage?> GetAsync(Guid ownerId, DateOnly day, CancellationToken ct = default) =>
        db.RecognitionUsages.FirstOrDefaultAsync(u => u.OwnerId == ownerId && u.Day == day, ct);

    public void Add(RecognitionUsage usage) => db.RecognitionUsages.Add(usage);
}
