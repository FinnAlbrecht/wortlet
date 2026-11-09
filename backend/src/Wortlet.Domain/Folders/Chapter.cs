using Wortlet.Domain.Common;

namespace Wortlet.Domain.Folders;

public sealed class Chapter : Entity<Guid>
{
    public const int MaxNameLength = 120;
    public const int MaxPagesLength = 40;

    private readonly List<Word> _words = [];

    public Guid FolderId { get; private set; }
    public int Position { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Pages { get; private set; } = string.Empty;

    /// <summary>„Neue Wörter“ im Gegensatz zu „Alten Wörtern“ (für die Schnellauswahl).</summary>
    public bool IsNew { get; private set; }

    public IReadOnlyList<Word> Words => _words.OrderBy(w => w.Position).ToList();

    private Chapter() { }

    internal Chapter(Guid folderId, int position, string name, string pages, bool isNew)
    {
        Id = Guid.NewGuid();
        FolderId = folderId;
        Position = position;
        var n = Guard.Optional(name, MaxNameLength, "Der Kapitelname");
        Name = n.Length == 0 ? "Kapitel" : n;
        Pages = Guard.Optional(pages, MaxPagesLength, "Die Seitenangabe");
        IsNew = isNew;
    }

    internal void AddWord(string key, IEnumerable<string> answers, string? hint) =>
        _words.Add(new Word(Id, _words.Count, key, answers, hint));
}
