using System.Text.RegularExpressions;
using Wortlet.Domain.Folders;

namespace Wortlet.Domain.Vocabulary;

public sealed record AnswerEvaluation(
    bool IsCorrect,
    IReadOnlyList<string> AcceptedAnswers, // alle Antworten, die als richtig gelten
    string Solution);                       // Anzeige der Lösung („Im Buch: …“)

/// <summary>
/// Antwortvergleich, 1:1 aus der HTML-Version übernommen:
/// Nur Gross/Klein, Leerzeichen und die Art des Apostrophs werden angeglichen. Alles andere muss exakt stimmen.
/// Richtung → Deutsch: ganzer Eintrag, ohne Klammern oder ein einzelner Teil („das Obst, die Frucht“ → „das Obst“),
/// und alle deutschen Bedeutungen mit derselben Fremdsprach-Antwort zählen (cream = die Sahne / der Rahm).
/// </summary>
public static partial class AnswerEvaluator
{
    /// <summary>Reine Tastatur-Varianten, die bei der Anzeige der Lösung ausgeblendet werden.</summary>
    private static readonly HashSet<string> HiddenVariants = new(StringComparer.Ordinal)
    {
        "rose", "cafe", "creme caramel", "coke®", "egg yolk", "yolk", "piece of candy", "candy",
        "potato chips", "to give sb. his bill", "to give sb. her bill",
    };

    [GeneratedRegex("[’`´‘]")]
    private static partial Regex ApostropheVariants();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@" \(\d+\)$")]
    private static partial Regex DuplicateSuffix();

    [GeneratedRegex(@"\s*\([^)]*\)\s*")]
    private static partial Regex Parentheses();

    public static string Normalize(string? s) =>
        Whitespace().Replace(ApostropheVariants().Replace((s ?? string.Empty).ToLowerInvariant(), "'").Replace("…", "..."), " ").Trim();

    public static bool IsMatch(string? input, IEnumerable<string> answers)
    {
        var n = Normalize(input);
        return answers.Any(a => Normalize(a) == n);
    }

    public static string BaseKey(string key) => DuplicateSuffix().Replace(key, string.Empty);

    /// <summary>Ganzer Eintrag, ohne Klammern, oder ein einzelner Teil („das Obst, die Frucht“ → „das Obst“).</summary>
    public static IReadOnlyList<string> GermanVariants(string key)
    {
        var full = BaseKey(key);
        var result = new List<string> { full };
        void Add(string v) { if (v.Length > 0 && !result.Contains(v)) result.Add(v); }

        var noParentheses = Whitespace().Replace(Parentheses().Replace(full, " "), " ").Trim();
        Add(noParentheses);
        foreach (var x in new[] { full, noParentheses })
            foreach (var part in x.Split(',').Select(t => t.Trim()))
                Add(part);
        return result;
    }

    /// <summary>Alle Wörter im Ordner mit derselben Fremdsprach-Antwort (z. B. cream = Sahne / Rahm).</summary>
    public static IReadOnlyList<Word> SameMeaning(Word word, IEnumerable<Word> folderWords)
    {
        var mine = word.Answers.Select(Normalize).ToHashSet(StringComparer.Ordinal);
        return folderWords.Where(x => ReferenceEquals(x, word) || x.Key == word.Key || x.Answers.Any(a => mine.Contains(Normalize(a)))).ToList();
    }

    public static IReadOnlyList<string> ReverseAccepted(Word word, IEnumerable<Word> folderWords) =>
        SameMeaning(word, folderWords).SelectMany(x => GermanVariants(x.Key)).Distinct(StringComparer.Ordinal).ToList();

    public static string ReverseSolution(Word word, IEnumerable<Word> folderWords) =>
        string.Join(" / ", SameMeaning(word, folderWords).Select(x => BaseKey(x.Key)).Distinct(StringComparer.Ordinal));

    /// <summary>Anzeige der Lösung: nur die Hauptformen (ohne reine Tastatur-Varianten).</summary>
    public static string Solution(IReadOnlyList<string> answers) =>
        string.Join(" / ", answers.Where((a, i) => !HiddenVariants.Contains(a) || answers.Count == 1 || answers[0] == a));

    /// <summary>Was in der Prüfung als Frage angezeigt wird.</summary>
    public static string Prompt(Word word, QuizDirection direction) =>
        direction == QuizDirection.ToGerman ? Solution(word.Answers) : BaseKey(word.Key);

    public static AnswerEvaluation Evaluate(Word word, IReadOnlyCollection<Word> folderWords, QuizDirection direction, string? input, bool skipped)
    {
        var accepted = direction == QuizDirection.ToGerman ? ReverseAccepted(word, folderWords) : word.Answers;
        var solution = direction == QuizDirection.ToGerman ? ReverseSolution(word, folderWords) : Solution(word.Answers);
        var correct = !skipped && IsMatch(input, accepted);
        return new AnswerEvaluation(correct, accepted, solution);
    }
}
