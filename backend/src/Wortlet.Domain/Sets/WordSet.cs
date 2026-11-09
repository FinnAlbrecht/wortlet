using Wortlet.Domain.Common;

namespace Wortlet.Domain.Sets;

/// <summary>
/// Eigene Wortauswahl innerhalb einer Sprache, auch über mehrere Ordner (z. B. „Prüfung Freitag“).
/// Verweist – wie die Fehlerliste – über Ordner und deutschen Schlüssel auf die Wörter:
/// Korrekturen im Ordner wirken sofort, gelöschte Wörter fallen weg.
/// </summary>
public sealed class WordSet : AggregateRoot<Guid>
{
    public const int MaxNameLength = 120;
    public const int MaxItems = 2000;

    private readonly List<SetItem> _items = [];

    public Guid OwnerId { get; private set; }
    public Guid LanguageId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<SetItem> Items => _items.OrderBy(i => i.Position).ToList();

    private WordSet() { }

    public static WordSet Create(Guid ownerId, Guid languageId, string name, IReadOnlyList<SetItemRef> items, DateTime now)
    {
        var set = new WordSet
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            LanguageId = languageId,
            CreatedAt = now,
        };
        set.Update(name, items, now);
        return set;
    }

    public void Update(string name, IReadOnlyList<SetItemRef> items, DateTime now)
    {
        Name = Guard.Name(name, MaxNameLength, "Der Name des Sets");
        var distinct = items.Distinct().ToList();
        if (distinct.Count == 0)
            throw new DomainException("Ein Set braucht mindestens ein Wort.");
        if (distinct.Count > MaxItems)
            throw new DomainException($"Ein Set darf höchstens {MaxItems} Wörter enthalten.");

        _items.Clear();
        for (var k = 0; k < distinct.Count; k++)
            _items.Add(new SetItem(distinct[k].FolderId, distinct[k].WordKey, k));
        UpdatedAt = now;
    }
}

/// <summary>Verweis auf ein Wort: Ordner + deutscher Schlüssel.</summary>
public sealed record SetItemRef(Guid FolderId, string WordKey);

public sealed class SetItem
{
    public Guid FolderId { get; private set; }
    public string WordKey { get; private set; } = string.Empty;
    public int Position { get; private set; }

    private SetItem() { }

    internal SetItem(Guid folderId, string wordKey, int position)
    {
        FolderId = folderId;
        WordKey = wordKey;
        Position = position;
    }
}

public sealed record WordSetSummary(Guid Id, Guid LanguageId, string Name, int ItemCount, DateTime CreatedAt);

public interface IWordSetRepository
{
    Task<WordSet?> GetAsync(Guid ownerId, Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<WordSetSummary>> ListAsync(Guid ownerId, Guid languageId, CancellationToken ct = default);
    void Add(WordSet set);
    void Remove(WordSet set);
}
