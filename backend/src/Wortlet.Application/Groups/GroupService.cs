using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Wortlet.Application.Common;
using ValidationException = Wortlet.Application.Common.ValidationException;
using Wortlet.Application.Profiles;
using Wortlet.Application.Statistics;
using Wortlet.Domain.Common;
using Wortlet.Domain.Folders;
using Wortlet.Domain.Groups;
using Wortlet.Domain.Languages;
using Wortlet.Domain.Profiles;
using Wortlet.Domain.Sets;
using Wortlet.Domain.Statistics;
using Wortlet.Domain.Vocabulary;

namespace Wortlet.Application.Groups;

public sealed record GroupSummaryDto(
    Guid Id, string Name, GroupVisibility Visibility, bool IsLeader, int MemberCount, int ShareCount, int PendingRequests);

/// <summary>Treffer der Gruppensuche – bewusst wenig: Name, Grösse, Leitung. Rangliste und Inhalte erst nach dem Beitritt.</summary>
public sealed record GroupSearchResultDto(Guid Id, string Name, GroupVisibility Visibility, int MemberCount, string LeaderName, string Status);

/// <summary>„joined“ = jetzt Mitglied, „requested“ = Anfrage wartet auf die Leitung.</summary>
public sealed record JoinResultDto(Guid GroupId, string Status);

/// <summary>Eigene offene Beitrittsanfrage.</summary>
public sealed record MyRequestDto(Guid GroupId, string Name, DateTime RequestedAt);

/// <summary>Offene Anfrage an eine Gruppe – nur für die Leitung sichtbar.</summary>
public sealed record JoinRequestDto(PublicProfileDto Profile, DateTime RequestedAt);

public sealed record RankingEntryDto(PublicProfileDto Profile, bool IsLeader, bool IsMe, int WeekAnswers, int CurrentStreak);

public sealed record GroupShareDto(
    Guid Id, string Kind, string Name, string LanguageName, int WordCount, DateTime SharedAt, bool IsMine, Guid? MyCopyFolderId);

public sealed record GroupDetailDto(
    Guid Id,
    string Name,
    string JoinCode,
    GroupVisibility Visibility,
    bool IsLeader,
    DateOnly WeekStart,
    IReadOnlyList<RankingEntryDto> Ranking,
    IReadOnlyList<GroupShareDto> Shares,
    IReadOnlyList<JoinRequestDto> JoinRequests);

public sealed record CopyResultDto(Guid FolderId, Guid LanguageId, bool Updated);

public sealed record GroupNameRequest(
    [Required(ErrorMessage = "Bitte gib einen Namen ein.")]
    [StringLength(Group.MaxNameLength, ErrorMessage = "Der Name ist zu lang.")] string Name);

public sealed record CreateGroupRequest(
    [Required(ErrorMessage = "Bitte gib einen Namen ein.")]
    [StringLength(Group.MaxNameLength, ErrorMessage = "Der Name ist zu lang.")] string Name,
    GroupVisibility Visibility = GroupVisibility.Private);

public sealed record VisibilityRequest(GroupVisibility Visibility);

public sealed record JoinGroupRequest(
    [Required(ErrorMessage = "Bitte gib den Code ein.")]
    [StringLength(20, ErrorMessage = "Der Code ist zu lang.")] string Code);

public sealed record ShareRequest(Guid? FolderId, Guid? SetId);

public sealed class GroupService(
    IGroupRepository groups,
    IProfileRepository profiles,
    ProfileService profileService,
    IStatisticsRepository stats,
    IFolderRepository folders,
    IWordSetRepository sets,
    ILanguageRepository languages,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
{
    public const int MaxGroupsPerUser = 30;

    /// <summary>Für die Lernserie reicht ein Jahr Verlauf.</summary>
    private const int StreakLookbackDays = 400;

    public async Task<IReadOnlyList<GroupSummaryDto>> ListMineAsync(CancellationToken ct = default)
    {
        var me = currentUser.UserId;
        return (await groups.ListForUserAsync(me, ct))
            .OrderBy(g => g.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(g => new GroupSummaryDto(g.Id, g.Name, g.Visibility, g.IsLeader(me), g.Members.Count, g.Shares.Count,
                g.IsLeader(me) ? g.JoinRequests.Count : 0))
            .ToList();
    }

    public const int MaxSearchResults = 20;

    private static readonly CompareInfo SwissCompare = CultureInfo.GetCultureInfo("de-CH").CompareInfo;
    private const CompareOptions SearchOptions = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    /// <summary>Sucht öffentliche Gruppen und Gruppen „auf Anfrage“ nach Namen. Private Gruppen findet man nie.</summary>
    public async Task<IReadOnlyList<GroupSearchResultDto>> SearchAsync(string? query, CancellationToken ct = default)
    {
        var q = (query ?? string.Empty).Trim();
        if (q.Length < 2)
            throw new ValidationException("Gib mindestens 2 Zeichen ein.");
        var me = currentUser.UserId;

        var hits = (await groups.ListSearchableAsync(ct))
            .Where(g => SwissCompare.IndexOf(g.Name, q, SearchOptions) >= 0)
            .OrderByDescending(g => SwissCompare.IsPrefix(g.Name, q, SearchOptions))
            .ThenByDescending(g => g.Members.Count)
            .ThenBy(g => g.Name, StringComparer.CurrentCultureIgnoreCase)
            .Take(MaxSearchResults)
            .ToList();

        var leaders = hits.ToDictionary(g => g.Id, g => g.Members.First(m => m.Role == GroupRole.Leader).UserId);
        var names = (await profiles.ListAsync(leaders.Values, ct)).ToDictionary(p => p.Id, p => p.DisplayName);
        return hits.Select(g =>
        {
            var status = g.IsMember(me) ? "member" : g.HasRequested(me) ? "requested" : "none";
            return new GroupSearchResultDto(g.Id, g.Name, g.Visibility, g.Members.Count, names.GetValueOrDefault(leaders[g.Id], "Leitung"), status);
        }).ToList();
    }

    /// <summary>Beitreten aus der Suche: öffentlich sofort, „auf Anfrage“ als Anfrage. Private Gruppen „gibt es nicht“.</summary>
    public async Task<JoinResultDto> JoinFromSearchAsync(Guid id, CancellationToken ct = default)
    {
        var me = currentUser.UserId;
        var group = await groups.GetAsync(id, ct);
        if (group is null || (!group.IsSearchable && !group.IsMember(me)))
            throw new NotFoundException("Diese Gruppe gibt es nicht.");
        if (group.IsMember(me))
            return new JoinResultDto(group.Id, "joined");

        await EnsureGroupLimit(me, ct);
        await profileService.EnsureAsync(me, ct);
        var joined = Run(() => group.JoinFromSearch(me, clock.UtcNow));
        await unitOfWork.SaveChangesAsync(ct);
        return new JoinResultDto(group.Id, joined ? "joined" : "requested");
    }

    public async Task<IReadOnlyList<MyRequestDto>> MyRequestsAsync(CancellationToken ct = default)
    {
        var me = currentUser.UserId;
        return (await groups.ListRequestedByUserAsync(me, ct))
            .Select(g => new MyRequestDto(g.Id, g.Name, g.JoinRequests.First(r => r.UserId == me).RequestedAt))
            .OrderByDescending(r => r.RequestedAt)
            .ToList();
    }

    public async Task CancelRequestAsync(Guid id, CancellationToken ct = default)
    {
        var group = await groups.GetAsync(id, ct);
        if (group is null || !group.HasRequested(currentUser.UserId))
            throw new NotFoundException("Diese Anfrage gibt es nicht (mehr).");
        group.CancelRequest(currentUser.UserId);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<GroupDetailDto> AcceptRequestAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var group = await LoadAsMember(id, ct);
        group.EnsureLeaderOrThrow(currentUser.UserId);
        if ((await groups.ListForUserAsync(userId, ct)).Count >= MaxGroupsPerUser)
            throw new ValidationException($"Diese Person ist schon in {MaxGroupsPerUser} Gruppen und kann keiner weiteren beitreten.");
        Run(() => { group.AcceptRequest(currentUser.UserId, userId, clock.UtcNow); return 0; });
        await unitOfWork.SaveChangesAsync(ct);
        return await DetailAsync(group, ct);
    }

    public async Task<GroupDetailDto> DeclineRequestAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var group = await LoadAsMember(id, ct);
        Run(() => { group.DeclineRequest(currentUser.UserId, userId); return 0; });
        await unitOfWork.SaveChangesAsync(ct);
        return await DetailAsync(group, ct);
    }

    public async Task<GroupDetailDto> ChangeVisibilityAsync(Guid id, VisibilityRequest request, CancellationToken ct = default)
    {
        var group = await LoadAsMember(id, ct);
        Run(() => { group.ChangeVisibility(currentUser.UserId, request.Visibility, clock.UtcNow); return 0; });
        await unitOfWork.SaveChangesAsync(ct);
        return await DetailAsync(group, ct);
    }

    public async Task<GroupDetailDto> CreateAsync(CreateGroupRequest request, CancellationToken ct = default)
    {
        var me = currentUser.UserId;
        if (!Enum.IsDefined(request.Visibility))
            throw new ValidationException("Diese Sichtbarkeit gibt es nicht.");
        await EnsureGroupLimit(me, ct);
        await profileService.EnsureAsync(me, ct);
        var code = await NewUniqueCode(ct);
        var group = Run(() => Group.Create(me, request.Name, code, clock.UtcNow, request.Visibility));
        groups.Add(group);
        await unitOfWork.SaveChangesAsync(ct);
        return await DetailAsync(group, ct);
    }

    public async Task<GroupDetailDto> JoinAsync(JoinGroupRequest request, CancellationToken ct = default)
    {
        var me = currentUser.UserId;
        var code = Group.NormalizeCode(request.Code);
        var group = (code.Length == Group.CodeLength ? await groups.GetByCodeAsync(code, ct) : null)
            ?? throw new NotFoundException("Zu diesem Code gibt es keine Gruppe. Prüfe ihn nochmals.");
        if (!group.IsMember(me))
        {
            await EnsureGroupLimit(me, ct);
            await profileService.EnsureAsync(me, ct);
            Run(() => { group.Join(me, clock.UtcNow); return 0; });
            await unitOfWork.SaveChangesAsync(ct);
        }
        return await DetailAsync(group, ct);
    }

    public async Task<GroupDetailDto> GetAsync(Guid id, CancellationToken ct = default) =>
        await DetailAsync(await LoadAsMember(id, ct), ct);

    public async Task<GroupDetailDto> RenameAsync(Guid id, GroupNameRequest request, CancellationToken ct = default)
    {
        var group = await LoadAsMember(id, ct);
        Run(() => { group.Rename(currentUser.UserId, request.Name); return 0; });
        await unitOfWork.SaveChangesAsync(ct);
        return await DetailAsync(group, ct);
    }

    /// <summary>Neuer Code – der alte gilt danach nicht mehr (z. B. wenn er herumgereicht wurde).</summary>
    public async Task<GroupDetailDto> NewCodeAsync(Guid id, CancellationToken ct = default)
    {
        var group = await LoadAsMember(id, ct);
        group.EnsureLeaderOrThrow(currentUser.UserId);
        var code = await NewUniqueCode(ct);
        Run(() => { group.ChangeJoinCode(currentUser.UserId, code); return 0; });
        await unitOfWork.SaveChangesAsync(ct);
        return await DetailAsync(group, ct);
    }

    public async Task LeaveAsync(Guid id, CancellationToken ct = default)
    {
        var group = await LoadAsMember(id, ct);
        Run(() => { group.Leave(currentUser.UserId); return 0; });
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var group = await LoadAsMember(id, ct);
        group.EnsureLeaderOrThrow(currentUser.UserId);
        groups.Remove(group);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<GroupDetailDto> RemoveMemberAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var group = await LoadAsMember(id, ct);
        Run(() => { group.RemoveMember(currentUser.UserId, userId); return 0; });
        await unitOfWork.SaveChangesAsync(ct);
        return await DetailAsync(group, ct);
    }

    public async Task<GroupDetailDto> ShareAsync(Guid id, ShareRequest request, CancellationToken ct = default)
    {
        var me = currentUser.UserId;
        var group = await LoadAsMember(id, ct);
        group.EnsureLeaderOrThrow(me);
        if (request.FolderId is Guid folderId && await folders.GetAsync(me, folderId, ct) is null)
            throw new NotFoundException("Diesen Ordner gibt es nicht.");
        if (request.SetId is Guid setId && await sets.GetAsync(me, setId, ct) is null)
            throw new NotFoundException("Dieses Set gibt es nicht.");
        Run(() => group.Share(me, request.FolderId, request.SetId, clock.UtcNow));
        await unitOfWork.SaveChangesAsync(ct);
        return await DetailAsync(group, ct);
    }

    public async Task<GroupDetailDto> UnshareAsync(Guid id, Guid shareId, CancellationToken ct = default)
    {
        var group = await LoadAsMember(id, ct);
        Run(() => { group.Unshare(currentUser.UserId, shareId); return 0; });
        await unitOfWork.SaveChangesAsync(ct);
        return await DetailAsync(group, ct);
    }

    /// <summary>
    /// Übernimmt einen geteilten Ordner bzw. ein geteiltes Set als eigenen Ordner (in die Sprache mit gleichem Namen).
    /// Wurde er schon übernommen, wird die eigene Kopie aktualisiert; die eigene Fehlerliste bleibt, soweit möglich.
    /// </summary>
    public async Task<CopyResultDto> CopyShareAsync(Guid id, Guid shareId, CancellationToken ct = default)
    {
        var me = currentUser.UserId;
        var group = await LoadAsMember(id, ct);
        var share = Run(() => group.GetShare(shareId));
        var source = await LoadSource(share, ct)
            ?? throw new NotFoundException("Das Geteilte gibt es nicht mehr.");
        var now = clock.UtcNow;

        if (await folders.GetCopyOfShareAsync(me, share.Id, ct) is { } existing)
        {
            Run(() => { existing.ReplaceContent(source.Chapters, now); return 0; });
            await unitOfWork.SaveChangesAsync(ct);
            return new CopyResultDto(existing.Id, existing.LanguageId, Updated: true);
        }

        var language = await FindOrCreateLanguage(me, source.LanguageName, now, ct);
        var copy = Run(() => Folder.Create(me, language.Id, source.Name, source.Chapters, now,
            description: $"Aus der Gruppe „{group.Name}“", oldLabel: source.OldLabel, newLabel: source.NewLabel));
        copy.MarkCopiedFrom(share.Id);
        folders.Add(copy);
        await unitOfWork.SaveChangesAsync(ct);
        return new CopyResultDto(copy.Id, language.Id, Updated: false);
    }

    private sealed record ShareSource(string Kind, string Name, string LanguageName, IReadOnlyList<ChapterDraft> Chapters, string? OldLabel, string? NewLabel)
    {
        public int WordCount => Chapters.Sum(c => c.Words.Count);
    }

    /// <summary>Inhalt eines geteilten Ordners/Sets – gelesen beim Besitzer, nicht beim Mitglied.</summary>
    private async Task<ShareSource?> LoadSource(GroupShare share, CancellationToken ct)
    {
        if (share.FolderId is Guid folderId)
        {
            var folder = await folders.GetAsync(share.SharedBy, folderId, ct);
            if (folder is null) return null;
            var language = await languages.GetAsync(share.SharedBy, folder.LanguageId, ct);
            var chapters = folder.Chapters
                .Select(c => new ChapterDraft(c.Name, c.Pages, c.IsNew, c.Words.Select(ToDraft).ToList()))
                .ToList();
            return new ShareSource("folder", folder.Name, language?.Name ?? "Sprache", chapters, folder.OldLabel, folder.NewLabel);
        }

        if (share.SetId is Guid setId)
        {
            var set = await sets.GetAsync(share.SharedBy, setId, ct);
            if (set is null) return null;
            var language = await languages.GetAsync(share.SharedBy, set.LanguageId, ct);
            var ownerFolders = (await folders.ListWithContentAsync(share.SharedBy, ct)).Where(f => f.LanguageId == set.LanguageId).ToList();
            var byId = ownerFolders.ToDictionary(f => f.Id);

            // Pro Herkunftsordner ein Kapitel, in der Reihenfolge des Sets.
            var chapters = new List<ChapterDraft>();
            foreach (var run in set.Items.Where(i => byId.ContainsKey(i.FolderId)).GroupBy(i => i.FolderId))
            {
                var folder = byId[run.Key];
                var words = run.Select(i => folder.FindWord(i.WordKey)).Where(w => w is not null).Select(w => ToDraft(w!)).ToList();
                if (words.Count > 0)
                    chapters.Add(new ChapterDraft(folder.Name, string.Empty, false, words));
            }
            return new ShareSource("set", set.Name, language?.Name ?? "Sprache", chapters, null, null);
        }
        return null;
    }

    private static WordDraft ToDraft(Word w) => new(AnswerEvaluator.BaseKey(w.Key), w.Answers, w.Hint);

    private async Task<GroupDetailDto> DetailAsync(Group group, CancellationToken ct)
    {
        var me = currentUser.UserId;
        var today = SwissCalendar.Day(clock.UtcNow);
        var weekStart = Streaks.WeekStart(today);
        var memberIds = group.Members.Select(m => m.UserId).ToList();

        var profileList = await profiles.ListAsync(memberIds, ct);
        var byUser = profileList.ToDictionary(p => p.Id);
        var days = await stats.ListDaysAsync(memberIds, today.AddDays(-StreakLookbackDays), ct);
        var daysByUser = days.Where(d => d.Answers > 0).GroupBy(d => d.OwnerId).ToDictionary(g => g.Key, g => g.ToList());

        var ranking = group.Members
            .Select(m =>
            {
                var mine = daysByUser.GetValueOrDefault(m.UserId) ?? [];
                var profile = byUser.TryGetValue(m.UserId, out var p)
                    ? ProfileService.ToPublic(p)
                    : new PublicProfileDto(m.UserId, "Mitglied", UserProfile.Icons[0], UserProfile.Colors[^1], false, 0);
                return new RankingEntryDto(
                    profile,
                    m.Role == GroupRole.Leader,
                    m.UserId == me,
                    mine.Where(d => d.Day >= weekStart && d.Day <= today).Sum(d => d.Answers),
                    Streaks.Compute(mine.Select(d => d.Day), today).Current);
            })
            .OrderByDescending(r => r.WeekAnswers)
            .ThenByDescending(r => r.CurrentStreak)
            .ThenBy(r => r.Profile.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var copied = await folders.ListCopiedSharesAsync(me, group.Shares.Select(s => s.Id).ToList(), ct);
        var shares = new List<GroupShareDto>();
        foreach (var share in group.Shares)
        {
            var source = await LoadSource(share, ct);
            if (source is null) continue;
            shares.Add(new GroupShareDto(share.Id, source.Kind, source.Name, source.LanguageName, source.WordCount, share.SharedAt,
                share.SharedBy == me, copied.TryGetValue(share.Id, out var folderId) ? folderId : null));
        }

        var requests = new List<JoinRequestDto>();
        if (group.IsLeader(me) && group.JoinRequests.Count > 0)
        {
            var requesters = (await profiles.ListAsync(group.JoinRequests.Select(r => r.UserId), ct)).ToDictionary(p => p.Id);
            requests = group.JoinRequests
                .Select(r => new JoinRequestDto(
                    requesters.TryGetValue(r.UserId, out var p)
                        ? ProfileService.ToPublic(p)
                        : new PublicProfileDto(r.UserId, "Unbekannt", UserProfile.Icons[0], UserProfile.Colors[^1], false, 0),
                    r.RequestedAt))
                .ToList();
        }

        return new GroupDetailDto(group.Id, group.Name, group.JoinCode, group.Visibility, group.IsLeader(me), weekStart, ranking, shares, requests);
    }

    private async Task<Language> FindOrCreateLanguage(Guid userId, string name, DateTime now, CancellationToken ct)
    {
        var existing = (await languages.ListAsync(userId, ct))
            .FirstOrDefault(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null) return existing;
        var created = Run(() => Language.Create(userId, name, now));
        languages.Add(created);
        return created;
    }

    private async Task EnsureGroupLimit(Guid userId, CancellationToken ct)
    {
        if ((await groups.ListForUserAsync(userId, ct)).Count >= MaxGroupsPerUser)
            throw new ValidationException($"Du kannst in höchstens {MaxGroupsPerUser} Gruppen sein.");
    }

    private async Task<string> NewUniqueCode(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var code = Group.NewJoinCode();
            if (!await groups.CodeExistsAsync(code, ct)) return code;
        }
        throw new ConflictException("Es konnte kein freier Code erzeugt werden. Bitte versuche es nochmals.");
    }

    /// <summary>Nicht-Mitglieder bekommen „gibt es nicht“ – so verrät die API nicht, welche Gruppen existieren.</summary>
    private async Task<Group> LoadAsMember(Guid id, CancellationToken ct)
    {
        var group = await groups.GetAsync(id, ct);
        if (group is null || !group.IsMember(currentUser.UserId))
            throw new NotFoundException("Diese Gruppe gibt es nicht.");
        return group;
    }

    private static T Run<T>(Func<T> action)
    {
        try { return action(); }
        catch (DomainException ex) { throw new ValidationException(ex.Message); }
    }
}

internal static class GroupGuards
{
    /// <summary>Leitungsrechte prüfen und als verständliche 400-Meldung melden.</summary>
    public static void EnsureLeaderOrThrow(this Group group, Guid userId)
    {
        try { group.EnsureLeader(userId); }
        catch (DomainException ex) { throw new ValidationException(ex.Message); }
    }
}
