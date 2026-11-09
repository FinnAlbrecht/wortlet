using System.Security.Cryptography;
using Wortlet.Domain.Common;

namespace Wortlet.Domain.Groups;

public enum GroupRole
{
    Member,
    Leader,
}

/// <summary>Wer eine Gruppe finden und wie man ihr beitreten kann. Per Code geht es immer.</summary>
public enum GroupVisibility
{
    /// <summary>Nicht in der Suche – nur per Code.</summary>
    Private,

    /// <summary>In der Suche; Beitritt per Anfrage, die die Leitung annimmt oder ablehnt.</summary>
    OnRequest,

    /// <summary>In der Suche; jeder kann direkt beitreten.</summary>
    Public,
}

/// <summary>
/// Eine Lerngruppe (z. B. Klasse 3b). Wer sie erstellt, ist Leitung. Andere treten per Code bei –
/// oder, je nach <see cref="GroupVisibility"/>, über die Suche direkt bzw. per Anfrage.
/// Mitglieder sehen eine Rangliste; die Leitung kann Ordner und Sets teilen, die Mitglieder als Kopie übernehmen.
/// </summary>
public sealed class Group : AggregateRoot<Guid>
{
    public const int MaxNameLength = 60;
    public const int MaxMembers = 200;
    public const int MaxShares = 100;
    public const int CodeLength = 8;
    public const int MaxJoinRequests = 100;

    /// <summary>Ohne leicht verwechselbare Zeichen (0/O, 1/I/L).</summary>
    private const string CodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    private readonly List<GroupMember> _members = [];
    private readonly List<GroupShare> _shares = [];
    private readonly List<GroupJoinRequest> _joinRequests = [];

    public string Name { get; private set; } = string.Empty;
    public string JoinCode { get; private set; } = string.Empty;
    public GroupVisibility Visibility { get; private set; } = GroupVisibility.Private;
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyList<GroupJoinRequest> JoinRequests => _joinRequests.OrderBy(r => r.RequestedAt).ToList();
    public IReadOnlyList<GroupMember> Members => _members.OrderBy(m => m.JoinedAt).ToList();
    public IReadOnlyList<GroupShare> Shares => _shares.OrderByDescending(s => s.SharedAt).ToList();

    private Group() { }

    public static Group Create(Guid leaderId, string name, string joinCode, DateTime now, GroupVisibility visibility = GroupVisibility.Private)
    {
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = Guard.Name(name, MaxNameLength, "Der Name der Gruppe"),
            JoinCode = joinCode,
            Visibility = visibility,
            CreatedAt = now,
        };
        group._members.Add(new GroupMember(leaderId, GroupRole.Leader, now));
        return group;
    }

    public static string NewJoinCode() =>
        new(Enumerable.Range(0, CodeLength).Select(_ => CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)]).ToArray());

    /// <summary>Gross-/Kleinschreibung, Leer- und Bindestriche beim Eintippen sind egal.</summary>
    public static string NormalizeCode(string? code) =>
        new((code ?? string.Empty).ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());

    public bool IsMember(Guid userId) => _members.Any(m => m.UserId == userId);

    public bool IsLeader(Guid userId) => _members.Any(m => m.UserId == userId && m.Role == GroupRole.Leader);

    public void EnsureMember(Guid userId)
    {
        if (!IsMember(userId))
            throw new DomainException("Du bist nicht in dieser Gruppe.");
    }

    public void EnsureLeader(Guid userId)
    {
        if (!IsLeader(userId))
            throw new DomainException("Das darf nur die Leitung der Gruppe.");
    }

    /// <summary>Beitritt per Code – geht bei jeder Sichtbarkeit. Eine offene Anfrage erledigt sich damit.</summary>
    public void Join(Guid userId, DateTime now)
    {
        _joinRequests.RemoveAll(r => r.UserId == userId);
        if (IsMember(userId)) return;
        if (_members.Count >= MaxMembers)
            throw new DomainException($"Die Gruppe ist voll (höchstens {MaxMembers} Mitglieder).");
        _members.Add(new GroupMember(userId, GroupRole.Member, now));
    }

    public bool IsSearchable => Visibility != GroupVisibility.Private;

    public bool HasRequested(Guid userId) => _joinRequests.Any(r => r.UserId == userId);

    /// <summary>
    /// Beitritt über die Suche: bei öffentlichen Gruppen sofort, bei Gruppen „auf Anfrage“ als Anfrage.
    /// </summary>
    /// <returns>true, wenn man jetzt Mitglied ist; false, wenn eine Anfrage wartet.</returns>
    public bool JoinFromSearch(Guid userId, DateTime now)
    {
        if (IsMember(userId)) return true;
        switch (Visibility)
        {
            case GroupVisibility.Public:
                Join(userId, now);
                return true;
            case GroupVisibility.OnRequest:
                if (HasRequested(userId)) return false;
                if (_joinRequests.Count >= MaxJoinRequests)
                    throw new DomainException("Diese Gruppe hat zu viele offene Anfragen. Versuche es später nochmals.");
                _joinRequests.Add(new GroupJoinRequest(userId, now));
                return false;
            default:
                throw new DomainException("Dieser Gruppe kannst du nur mit einem Code beitreten.");
        }
    }

    public void CancelRequest(Guid userId) => _joinRequests.RemoveAll(r => r.UserId == userId);

    public void AcceptRequest(Guid actorId, Guid userId, DateTime now)
    {
        EnsureLeader(actorId);
        if (!HasRequested(userId))
            throw new DomainException("Diese Anfrage gibt es nicht (mehr).");
        Join(userId, now);
    }

    public void DeclineRequest(Guid actorId, Guid userId)
    {
        EnsureLeader(actorId);
        if (_joinRequests.RemoveAll(r => r.UserId == userId) == 0)
            throw new DomainException("Diese Anfrage gibt es nicht (mehr).");
    }

    /// <summary>
    /// Sichtbarkeit ändern. Wird die Gruppe öffentlich, werden offene Anfragen angenommen –
    /// diese Personen wollten ja beitreten, und jetzt dürfte es jeder.
    /// </summary>
    public void ChangeVisibility(Guid actorId, GroupVisibility visibility, DateTime now)
    {
        EnsureLeader(actorId);
        if (!Enum.IsDefined(visibility))
            throw new DomainException("Diese Sichtbarkeit gibt es nicht.");
        Visibility = visibility;
        if (visibility == GroupVisibility.Public)
        {
            foreach (var request in _joinRequests.OrderBy(r => r.RequestedAt).ToList())
            {
                if (_members.Count >= MaxMembers) break;
                Join(request.UserId, now);
            }
        }
    }

    public void Leave(Guid userId)
    {
        EnsureMember(userId);
        if (IsLeader(userId))
            throw new DomainException("Als Leitung kannst du die Gruppe nicht verlassen. Du kannst sie aber löschen.");
        _members.RemoveAll(m => m.UserId == userId);
    }

    public void RemoveMember(Guid actorId, Guid userId)
    {
        EnsureLeader(actorId);
        if (actorId == userId)
            throw new DomainException("Du kannst dich nicht selbst entfernen.");
        if (_members.RemoveAll(m => m.UserId == userId) == 0)
            throw new DomainException("Diese Person ist nicht in der Gruppe.");
    }

    public void Rename(Guid actorId, string name)
    {
        EnsureLeader(actorId);
        Name = Guard.Name(name, MaxNameLength, "Der Name der Gruppe");
    }

    public void ChangeJoinCode(Guid actorId, string joinCode)
    {
        EnsureLeader(actorId);
        JoinCode = joinCode;
    }

    /// <summary>Teilt einen Ordner oder ein Set der Leitung. Doppelt teilen geht nicht.</summary>
    public GroupShare Share(Guid actorId, Guid? folderId, Guid? setId, DateTime now)
    {
        EnsureLeader(actorId);
        if ((folderId is null) == (setId is null))
            throw new DomainException("Teile entweder einen Ordner oder ein Set.");
        if (_shares.Any(s => s.FolderId == folderId && s.SetId == setId))
            throw new DomainException("Das ist schon mit der Gruppe geteilt.");
        if (_shares.Count >= MaxShares)
            throw new DomainException($"Eine Gruppe kann höchstens {MaxShares} Ordner und Sets teilen.");
        var share = new GroupShare(actorId, folderId, setId, now);
        _shares.Add(share);
        return share;
    }

    public void Unshare(Guid actorId, Guid shareId)
    {
        EnsureLeader(actorId);
        if (_shares.RemoveAll(s => s.Id == shareId) == 0)
            throw new DomainException("Das ist nicht (mehr) geteilt.");
    }

    public GroupShare GetShare(Guid shareId) =>
        _shares.FirstOrDefault(s => s.Id == shareId) ?? throw new DomainException("Das ist nicht (mehr) geteilt.");
}

/// <summary>Jemand möchte einer Gruppe „auf Anfrage“ beitreten.</summary>
public sealed class GroupJoinRequest
{
    public Guid UserId { get; private set; }
    public DateTime RequestedAt { get; private set; }

    private GroupJoinRequest() { }

    internal GroupJoinRequest(Guid userId, DateTime requestedAt)
    {
        UserId = userId;
        RequestedAt = requestedAt;
    }
}

public sealed class GroupMember
{
    public Guid UserId { get; private set; }
    public GroupRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }

    private GroupMember() { }

    internal GroupMember(Guid userId, GroupRole role, DateTime joinedAt)
    {
        UserId = userId;
        Role = role;
        JoinedAt = joinedAt;
    }
}

/// <summary>Ein mit der Gruppe geteilter Ordner oder ein geteiltes Set (gehört der Person, die es geteilt hat).</summary>
public sealed class GroupShare : Entity<Guid>
{
    public Guid SharedBy { get; private set; }
    public Guid? FolderId { get; private set; }
    public Guid? SetId { get; private set; }
    public DateTime SharedAt { get; private set; }

    private GroupShare() { }

    internal GroupShare(Guid sharedBy, Guid? folderId, Guid? setId, DateTime sharedAt)
    {
        Id = Guid.NewGuid();
        SharedBy = sharedBy;
        FolderId = folderId;
        SetId = setId;
        SharedAt = sharedAt;
    }
}

public interface IGroupRepository
{
    Task<Group?> GetAsync(Guid id, CancellationToken ct = default);
    Task<Group?> GetByCodeAsync(string joinCode, CancellationToken ct = default);
    Task<IReadOnlyList<Group>> ListForUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Gruppen, bei denen die Person eine offene Beitrittsanfrage hat.</summary>
    Task<IReadOnlyList<Group>> ListRequestedByUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Öffentliche Gruppen und Gruppen „auf Anfrage“ – nie private.</summary>
    Task<IReadOnlyList<Group>> ListSearchableAsync(CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string joinCode, CancellationToken ct = default);
    void Add(Group group);
    void Remove(Group group);
}
