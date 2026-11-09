using Wortlet.Domain.Common;

namespace Wortlet.Domain.Statistics;

/// <summary>Wie oft ein Wort eines Ordners geübt wurde und wie oft es falsch war.</summary>
public sealed class WordStat : Entity<Guid>
{
    public Guid OwnerId { get; private set; }
    public Guid FolderId { get; private set; }
    public string WordKey { get; private set; } = string.Empty;
    public int Attempts { get; private set; }
    public int Wrong { get; private set; }
    public bool LastCorrect { get; private set; }
    public DateTime LastPracticedAt { get; private set; }

    private WordStat() { }

    public static WordStat Start(Guid ownerId, Guid folderId, string wordKey) => new()
    {
        Id = Guid.NewGuid(),
        OwnerId = ownerId,
        FolderId = folderId,
        WordKey = wordKey,
    };

    public void Record(bool correct, DateTime now)
    {
        Attempts++;
        if (!correct) Wrong++;
        LastCorrect = correct;
        LastPracticedAt = now;
    }

    /// <summary>„War doch richtig“: Die letzte falsche Antwort zählt nicht mehr als falsch.</summary>
    public void Overturn()
    {
        if (Wrong > 0) Wrong--;
        LastCorrect = true;
    }
}

/// <summary>Anzahl Antworten eines Benutzers an einem Tag (Schweizer Zeit). Grundlage für Lernserie und Rangliste.</summary>
public sealed class PracticeDay : Entity<Guid>
{
    public Guid OwnerId { get; private set; }
    public DateOnly Day { get; private set; }
    public int Answers { get; private set; }
    public int Correct { get; private set; }

    private PracticeDay() { }

    public static PracticeDay Start(Guid ownerId, DateOnly day) => new()
    {
        Id = Guid.NewGuid(),
        OwnerId = ownerId,
        Day = day,
    };

    public void Record(bool correct)
    {
        Answers++;
        if (correct) Correct++;
    }

    public void Overturn()
    {
        if (Correct < Answers) Correct++;
    }
}

/// <summary>Ergebnis einer Prüfung nach dem ersten Durchgang (daraus wird die Note berechnet).</summary>
public sealed class QuizResult : AggregateRoot<Guid>
{
    public const int MaxTitleLength = 160;

    public Guid OwnerId { get; private set; }

    /// <summary>Geprüfter Ordner (null bei einem Set oder wenn der Ordner gelöscht wurde).</summary>
    public Guid? FolderId { get; private set; }

    /// <summary>Geprüftes Set (null bei einem Ordner oder wenn das Set gelöscht wurde).</summary>
    public Guid? SetId { get; private set; }

    /// <summary>Name zum Zeitpunkt der Prüfung – bleibt erhalten, auch wenn Ordner/Set später gelöscht wird.</summary>
    public string Title { get; private set; } = string.Empty;

    public string Direction { get; private set; } = string.Empty;
    public bool PracticeMode { get; private set; }
    public int WordCount { get; private set; }
    public int FirstRoundCorrect { get; private set; }
    public double Grade { get; private set; }
    public DateTime FinishedAt { get; private set; }

    private QuizResult() { }

    public static readonly IReadOnlyList<string> Directions = ["de", "fx", "mix"];

    public static QuizResult Create(
        Guid ownerId, Guid? folderId, Guid? setId, string title, string direction,
        bool practiceMode, int wordCount, int firstRoundCorrect, DateTime now)
    {
        if ((folderId is null) == (setId is null))
            throw new DomainException("Eine Prüfung gehört entweder zu einem Ordner oder zu einem Set.");
        if (wordCount < 1)
            throw new DomainException("Eine Prüfung braucht mindestens ein Wort.");
        if (firstRoundCorrect < 0 || firstRoundCorrect > wordCount)
            throw new DomainException("Die Anzahl richtiger Antworten ist ungültig.");
        if (!Directions.Contains(direction))
            throw new DomainException("Die Richtung ist ungültig.");

        return new QuizResult
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            FolderId = folderId,
            SetId = setId,
            Title = Guard.Name(title, MaxTitleLength, "Der Titel"),
            Direction = direction,
            PracticeMode = practiceMode,
            WordCount = wordCount,
            FirstRoundCorrect = firstRoundCorrect,
            Grade = Vocabulary.Grading.SwissGrade(firstRoundCorrect, wordCount),
            FinishedAt = now,
        };
    }
}

/// <summary>Lernserie: wie viele Tage hintereinander geübt wurde.</summary>
public static class Streaks
{
    /// <summary>
    /// Aktuelle Serie zählt bis heute – oder bis gestern, wenn heute noch nicht geübt wurde
    /// (die Serie ist dann noch nicht verloren). Längste Serie über alle Tage.
    /// </summary>
    public static (int Current, int Longest) Compute(IEnumerable<DateOnly> practicedDays, DateOnly today)
    {
        var days = practicedDays.Distinct().OrderBy(d => d).ToList();
        int longest = 0, run = 0;
        DateOnly? previous = null;
        foreach (var d in days)
        {
            run = previous is { } p && p.AddDays(1) == d ? run + 1 : 1;
            longest = Math.Max(longest, run);
            previous = d;
        }

        var set = days.ToHashSet();
        var cursor = set.Contains(today) ? today : today.AddDays(-1);
        var current = 0;
        while (set.Contains(cursor))
        {
            current++;
            cursor = cursor.AddDays(-1);
        }
        return (current, longest);
    }

    /// <summary>Montag der Woche, in der <paramref name="day"/> liegt.</summary>
    public static DateOnly WeekStart(DateOnly day) => day.AddDays(-(((int)day.DayOfWeek + 6) % 7));
}

/// <summary>Ein Wort mit vielen Fehlern, für „Die schwierigsten Wörter“.</summary>
public sealed record HardWord(Guid FolderId, string WordKey, int Attempts, int Wrong);

public sealed record StatTotals(int WordsPracticed, int Quizzes, double? AverageGrade);

public interface IStatisticsRepository
{
    Task<WordStat?> GetWordStatAsync(Guid ownerId, Guid folderId, string wordKey, CancellationToken ct = default);
    Task<PracticeDay?> GetDayAsync(Guid ownerId, DateOnly day, CancellationToken ct = default);
    void Add(WordStat stat);
    void Add(PracticeDay day);
    void Add(QuizResult result);

    Task<IReadOnlyList<PracticeDay>> ListDaysAsync(Guid ownerId, CancellationToken ct = default);

    /// <summary>Übungstage mehrerer Benutzer ab einem Datum (für Ranglisten).</summary>
    Task<IReadOnlyList<PracticeDay>> ListDaysAsync(IReadOnlyCollection<Guid> ownerIds, DateOnly from, CancellationToken ct = default);

    Task<IReadOnlyList<QuizResult>> ListResultsAsync(Guid ownerId, int take, CancellationToken ct = default);
    Task<StatTotals> TotalsAsync(Guid ownerId, CancellationToken ct = default);
    Task<IReadOnlyList<HardWord>> HardestAsync(Guid ownerId, Guid? languageId, int take, CancellationToken ct = default);
}
