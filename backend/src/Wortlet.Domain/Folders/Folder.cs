using Wortlet.Domain.Common;

namespace Wortlet.Domain.Folders;

/// <summary>
/// Ein Vokabelordner (z. B. „Einkaufen &amp; Ernährung“) mit Kapiteln, Wörtern und der Liste
/// „Wörter, die ich nicht kann“. Aggregatwurzel: Alle Änderungen laufen über diese Klasse.
/// </summary>
public sealed class Folder : AggregateRoot<Guid>
{
    public const int MaxNameLength = 120;
    public const int MaxDescriptionLength = 120;
    public const int MaxLabelLength = 60;
    public const int MaxWords = 5000;

    public const string DefaultOldLabel = "Alte Kapitel";
    public const string DefaultNewLabel = "Neue Kapitel";
    public const string CustomDescription = "Eigener Ordner";

    private readonly List<Chapter> _chapters = [];
    private readonly List<WeakWord> _weakWords = [];

    public Guid OwnerId { get; private set; }
    public Guid LanguageId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string OldLabel { get; private set; } = DefaultOldLabel;
    public string NewLabel { get; private set; } = DefaultNewLabel;

    /// <summary>Startdaten aus dem Buch: „genau wie im Buch“, mit den Regeln für Englisch.</summary>
    public bool IsSeed { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<Chapter> Chapters => _chapters.OrderBy(c => c.Position).ToList();
    public IReadOnlyCollection<WeakWord> WeakWords => _weakWords.AsReadOnly();

    public IEnumerable<Word> AllWords => Chapters.SelectMany(c => c.Words);
    public int WordCount => _chapters.Sum(c => c.Words.Count);

    private Folder() { }

    public static Folder Create(
        Guid ownerId,
        Guid languageId,
        string name,
        IReadOnlyList<ChapterDraft> chapters,
        DateTime now,
        string? description = null,
        string? oldLabel = null,
        string? newLabel = null,
        bool isSeed = false)
    {
        var folder = new Folder
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            LanguageId = languageId,
            Name = Guard.Name(name, MaxNameLength, "Der Name des Ordners"),
            Description = Guard.Optional(description, MaxDescriptionLength, "Die Beschreibung") is { Length: > 0 } d ? d : CustomDescription,
            OldLabel = Guard.Optional(oldLabel, MaxLabelLength, "Die Beschriftung") is { Length: > 0 } o ? o : DefaultOldLabel,
            NewLabel = Guard.Optional(newLabel, MaxLabelLength, "Die Beschriftung") is { Length: > 0 } n ? n : DefaultNewLabel,
            IsSeed = isSeed,
            CreatedAt = now,
            UpdatedAt = now,
        };
        folder.SetContent(chapters);
        return folder;
    }

    public void Rename(string name, DateTime now)
    {
        Name = Guard.Name(name, MaxNameLength, "Der Name des Ordners");
        UpdatedAt = now;
    }

    /// <summary>Ersetzt alle Kapitel und Wörter. Fehlerwörter, deren Wort es noch gibt, bleiben erhalten.</summary>
    public void ReplaceContent(IReadOnlyList<ChapterDraft> chapters, DateTime now)
    {
        SetContent(chapters);
        var keys = AllWords.Select(w => w.Key).ToHashSet(StringComparer.Ordinal);
        _weakWords.RemoveAll(w => !keys.Contains(w.WordKey));
        UpdatedAt = now;
    }

    private void SetContent(IReadOnlyList<ChapterDraft> chapters)
    {
        var nonEmpty = chapters.Where(c => c.Words.Count > 0).ToList();
        if (nonEmpty.Count == 0)
            throw new DomainException("Ein Ordner braucht mindestens ein Wort.");
        if (nonEmpty.Sum(c => c.Words.Count) > MaxWords)
            throw new DomainException($"Ein Ordner darf höchstens {MaxWords} Wörter enthalten.");

        _chapters.Clear();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var draft in nonEmpty)
        {
            var chapter = new Chapter(Id, _chapters.Count, draft.Name, draft.Pages, draft.IsNew);
            foreach (var word in draft.Words)
                chapter.AddWord(UniqueKey(word.German, seen), word.Answers, word.Hint);
            _chapters.Add(chapter);
        }
    }

    /// <summary>Wie im Original: doppelte deutsche Einträge bekommen „ (2)“, „ (3)“ …</summary>
    internal static string UniqueKey(string german, HashSet<string> seen)
    {
        var baseKey = (german ?? string.Empty).Trim();
        var key = baseKey;
        var k = 2;
        while (!seen.Add(key))
            key = $"{baseKey} ({k++})";
        return key;
    }

    public Word? FindWord(string key) => AllWords.FirstOrDefault(w => w.Key == key);

    public Word GetWord(string key) =>
        FindWord(key) ?? throw new DomainException("Dieses Wort gibt es in diesem Ordner nicht (mehr).");

    public bool IsWeak(string key) => _weakWords.Any(w => w.WordKey == key);

    /// <summary>Fehlerwörter in der Reihenfolge, in der sie dazugekommen sind (nur solche, die es noch gibt).</summary>
    public IReadOnlyList<Word> WeakWordList()
    {
        var byKey = AllWords.ToDictionary(w => w.Key, StringComparer.Ordinal);
        return _weakWords
            .OrderBy(w => w.AddedAt)
            .Select(w => byKey.GetValueOrDefault(w.WordKey))
            .Where(w => w is not null)
            .Select(w => w!)
            .ToList();
    }

    public void MarkWeak(string key, DateTime now)
    {
        GetWord(key);
        if (!IsWeak(key))
            _weakWords.Add(new WeakWord(key, now));
    }

    public void UnmarkWeak(string key) => _weakWords.RemoveAll(w => w.WordKey == key);

    /// <summary>
    /// Regel der Liste „Wörter, die ich nicht kann“:
    /// Falsch → kommt hinein. Richtig im Übungsmodus und im ersten Durchgang → kommt heraus.
    /// </summary>
    /// <returns>Ob das Wort vor dieser Antwort schon in der Liste war (für „War doch richtig“).</returns>
    public bool RecordAnswer(string key, bool correct, int round, bool practiceMode, DateTime now)
    {
        GetWord(key);
        var wasWeak = IsWeak(key);
        if (!correct)
            MarkWeak(key, now);
        else if (practiceMode && round == 1)
            UnmarkWeak(key);
        return wasWeak;
    }

    /// <summary>
    /// „War doch richtig“: Das Wort kommt wieder raus, falls es erst durch diese Antwort hineinkam.
    /// Im Übungsmodus im ersten Durchgang gilt es zudem als gekonnt.
    /// </summary>
    public void OverrideAsCorrect(string key, int round, bool practiceMode, bool wasWeakBefore)
    {
        GetWord(key);
        if (!wasWeakBefore)
            UnmarkWeak(key);
        if (practiceMode && round == 1)
            UnmarkWeak(key);
    }

    /// <summary>Für den Import: Fehlerwörter übernehmen, unbekannte Schlüssel ignorieren.</summary>
    public void RestoreWeakWords(IEnumerable<string> keys, DateTime now)
    {
        var i = 0;
        foreach (var key in keys)
        {
            if (FindWord(key) is not null && !IsWeak(key))
                _weakWords.Add(new WeakWord(key, now.AddTicks(i++)));
        }
    }
}
