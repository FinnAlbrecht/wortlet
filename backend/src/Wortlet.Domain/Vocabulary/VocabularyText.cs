using Wortlet.Domain.Folders;

namespace Wortlet.Domain.Vocabulary;

public sealed record VocabularyParseResult(IReadOnlyList<ChapterDraft> Chapters, IReadOnlyList<int> InvalidLines)
{
    public int WordCount => Chapters.Sum(c => c.Words.Count);
}

/// <summary>
/// Das Textformat des Wörterfelds (übernommen aus der HTML-Version):
/// <code>
/// # Kapitel
/// die Birne = pear
/// die Kasse = checkout; till
/// </code>
/// Erweiterung, damit auch die Buchdaten verlustfrei bearbeitbar sind (optional):
/// <code>
/// # Obst und Gemüse | S. 216–217 | neu
/// die Kasse = checkout; cash desk; till | 3 Wörter im Buch, eines reicht
/// </code>
/// </summary>
public static class VocabularyText
{
    public const string DefaultChapterName = "Alle Wörter";
    private const string NewMarker = "neu";

    public static VocabularyParseResult Parse(string? text)
    {
        var chapters = new List<(string Name, string Pages, bool IsNew, List<WordDraft> Words)>();
        var errors = new List<int>();
        var lines = (text ?? string.Empty).Replace("\r\n", "\n").Split('\n');
        (string Name, string Pages, bool IsNew, List<WordDraft> Words)? current = null;

        for (var n = 0; n < lines.Length; n++)
        {
            var line = lines[n].Trim();
            if (line.Length == 0) continue;

            if (line.StartsWith('#'))
            {
                var parts = line.TrimStart('#').Split('|').Select(p => p.Trim()).ToArray();
                var name = parts[0].Length > 0 ? parts[0] : "Kapitel";
                var isNew = parts.Skip(1).Any(p => p.Equals(NewMarker, StringComparison.OrdinalIgnoreCase));
                var pages = parts.Skip(1).FirstOrDefault(p => p.Length > 0 && !p.Equals(NewMarker, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
                current = (name, pages, isNew, new List<WordDraft>());
                chapters.Add(current.Value);
                continue;
            }

            var eq = line.IndexOf('=');
            if (eq < 0) { errors.Add(n + 1); continue; }

            var german = line[..eq].Trim();
            var rest = line[(eq + 1)..];
            string? hint = null;
            var bar = rest.IndexOf('|');
            if (bar >= 0)
            {
                hint = rest[(bar + 1)..].Trim();
                if (hint.Length == 0) hint = null;
                rest = rest[..bar];
            }
            var answers = rest.Split(';').Select(a => a.Trim()).Where(a => a.Length > 0).ToList();
            if (german.Length == 0 || answers.Count == 0) { errors.Add(n + 1); continue; }

            if (current is null)
            {
                current = (DefaultChapterName, string.Empty, false, new List<WordDraft>());
                chapters.Add(current.Value);
            }
            current.Value.Words.Add(new WordDraft(german, answers, hint));
        }

        var result = chapters
            .Where(c => c.Words.Count > 0)
            .Select(c => new ChapterDraft(c.Name, c.Pages, c.IsNew, c.Words))
            .ToList();
        return new VocabularyParseResult(result, errors);
    }

    /// <summary>Wandelt einen Ordner zurück in Text, sodass <see cref="Parse"/> wieder denselben Inhalt ergibt.</summary>
    public static string Format(IEnumerable<Chapter> chapters)
    {
        var lines = new List<string>();
        foreach (var chapter in chapters)
        {
            var head = "# " + chapter.Name;
            if (chapter.Pages.Length > 0) head += " | " + chapter.Pages;
            if (chapter.IsNew) head += " | " + NewMarker;
            if (lines.Count > 0) lines.Add(string.Empty);
            lines.Add(head);
            foreach (var word in chapter.Words)
            {
                var line = AnswerEvaluator.BaseKey(word.Key) + " = " + string.Join("; ", word.Answers);
                if (!string.IsNullOrEmpty(word.Hint)) line += " | " + word.Hint;
                lines.Add(line);
            }
        }
        return string.Join("\n", lines);
    }
}
