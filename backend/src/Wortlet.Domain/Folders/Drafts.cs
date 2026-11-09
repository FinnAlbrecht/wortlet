namespace Wortlet.Domain.Folders;

/// <summary>Inhalt eines Kapitels, bevor er in einen Ordner übernommen wird.</summary>
public sealed record ChapterDraft(string Name, string Pages, bool IsNew, IReadOnlyList<WordDraft> Words);

/// <summary>Ein Wort: deutscher Eintrag (zugleich Schlüssel), alle richtigen Antworten, optionaler Hinweis.</summary>
public sealed record WordDraft(string German, IReadOnlyList<string> Answers, string? Hint = null);
