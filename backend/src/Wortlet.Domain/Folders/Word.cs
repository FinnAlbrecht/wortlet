using Wortlet.Domain.Common;

namespace Wortlet.Domain.Folders;

public sealed class Word : Entity<Guid>
{
    public const int MaxKeyLength = 300;
    public const int MaxAnswerLength = 300;
    public const int MaxHintLength = 300;

    public Guid ChapterId { get; private set; }
    public int Position { get; private set; }

    /// <summary>Deutscher Eintrag. Eindeutig im Ordner (Duplikate bekommen „ (2)“ usw.).</summary>
    public string Key { get; private set; } = string.Empty;

    public List<string> Answers { get; private set; } = [];
    public string? Hint { get; private set; }

    private Word() { }

    internal Word(Guid chapterId, int position, string key, IEnumerable<string> answers, string? hint)
    {
        Id = Guid.NewGuid();
        ChapterId = chapterId;
        Position = position;
        Key = Guard.Name(key, MaxKeyLength, "Der deutsche Eintrag");
        Answers = answers
            .Select(a => (a ?? string.Empty).Trim())
            .Where(a => a.Length > 0)
            .Select(a => a.Length > MaxAnswerLength ? throw new DomainException($"Eine Antwort darf höchstens {MaxAnswerLength} Zeichen lang sein.") : a)
            .ToList();
        if (Answers.Count == 0)
            throw new DomainException($"Für „{Key}“ fehlt die Übersetzung.");
        var h = Guard.Optional(hint, MaxHintLength, "Der Hinweis");
        Hint = h.Length == 0 ? null : h;
    }
}
